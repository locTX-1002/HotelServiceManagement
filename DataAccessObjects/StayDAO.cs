using System.Data;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

/// <summary>DAO Singleton cho check-in/check-out, cap nhat trang thai trong transaction.</summary>
public sealed class StayDAO
{
    private static readonly Lazy<StayDAO> LazyInstance = new(() => new StayDAO());
    private StayDAO() { }
    public static StayDAO Instance => LazyInstance.Value;

    public async Task<List<Stay>> GetActiveAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        return await Query(context).Where(s => s.Status == StayStatus.Active)
            .OrderBy(s => s.Reservation.Room.RoomNumber).ToListAsync();
    }

    /// <summary>
    /// Luot can xu ly tien: dang o, HOAC da tra phong ma hoa don chua thanh toan xong.
    ///
    /// Ve nhom thu hai: man Hoa don truoc day chi nap luot dang o, nen luot nao da tra
    /// phong ma con no tien la bien mat khoi giao dien - khong con cho nao thu duoc nua.
    /// Tien khach no nam lai trong database ma khong ai nhin thay.
    /// </summary>
    public async Task<List<Stay>> GetBillableAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        return await Query(context)
            .Where(s => s.Status == StayStatus.Active
                        || s.Invoice == null
                        || (s.Invoice.Status != InvoiceStatus.Paid
                            && s.Invoice.Status != InvoiceStatus.Cancelled))
            .OrderBy(s => s.Status == StayStatus.Active ? 0 : 1)
            .ThenBy(s => s.Reservation.Room.RoomNumber)
            .ToListAsync();
    }

    public async Task<Stay?> GetByIdAsync(int id)
    {
        await using var context = HotelDbContextFactory.Create();
        return await Query(context).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Stay?> CheckInAsync(int reservationId, int? userId, DateTime actualCheckIn)
    {
        await using var context = HotelDbContextFactory.Create();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var reservation = await context.Reservations.Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId);
        if (reservation == null || reservation.Status != ReservationStatus.Confirmed
            || reservation.Room.Status is RoomStatus.Occupied or RoomStatus.Maintenance
            || await context.Stays.AnyAsync(s => s.ReservationId == reservationId))
            return null;

        var stay = new Stay
        {
            ReservationId = reservationId,
            ActualCheckIn = actualCheckIn,
            Status = StayStatus.Active,
            CheckedInByUserId = userId,
        };
        // Nhan phong SOM hon don thi khoang o that su dai ra ve phia truoc - phai kiem
        // nhung dem them do co con trong khong. Truoc day khong kiem: chi nhin Room.Status,
        // ma phong co nguoi DAT cho dem nay nhung chua den nhan thi trang thai van la Trong.
        // Cho nhan phong la thanh hai khach mot phong.
        var stayFrom = actualCheckIn.Date < reservation.CheckInDate.Date
            ? actualCheckIn.Date
            : reservation.CheckInDate.Date;
        var clash = await context.Reservations.AnyAsync(r =>
            r.Id != reservation.Id
            && r.RoomId == reservation.RoomId
            && (r.Status == ReservationStatus.Pending
                || r.Status == ReservationStatus.Confirmed
                || r.Status == ReservationStatus.CheckedIn)
            && r.CheckInDate < reservation.CheckOutDate
            && r.CheckOutDate > stayFrom);
        if (clash)
        {
            return null;
        }

        // Nhan phong SOM hon ngay tren don thi keo luon ngay nhan cua don ve ngay vao that.
        // Truoc day de nguyen, thanh ra don mang mot ngay ma luot o mang ngay khac: lich
        // phong, danh sach dat phong, bang chi tiet va hoa don moi cho doc mot moc, doc ra
        // bon ngay khac nhau cho cung mot khach. Ghi ve mot cho thi khong con gi de lech.
        //
        // Chi keo khi den SOM. Den MUON thi giu nguyen ngay dat, vi nhung dem da giu cho
        // van phai tra tien - do la quy tac "di som khong hoan tien" ap cho ca hai chieu.
        if (actualCheckIn.Date < reservation.CheckInDate.Date)
        {
            reservation.CheckInDate = actualCheckIn.Date;
        }

        reservation.Status = ReservationStatus.CheckedIn;
        reservation.Room.Status = RoomStatus.Occupied;
        context.Stays.Add(stay);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return stay;
    }

    public async Task<Stay?> CheckOutAsync(int stayId, int? userId, DateTime actualCheckOut)
    {
        await using var context = HotelDbContextFactory.Create();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var stay = await context.Stays.Include(s => s.Reservation).ThenInclude(r => r.Room)
            .Include(s => s.Invoice)
            .Include(s => s.ServiceOrders)
            .FirstOrDefaultAsync(s => s.Id == stayId);
        if (stay == null || stay.Status != StayStatus.Active || actualCheckOut < stay.ActualCheckIn)
            return null;
        if (stay.Invoice?.Status != InvoiceStatus.Paid
            || stay.ServiceOrders.Any(o => o.Status is ServiceOrderStatus.Pending or ServiceOrderStatus.Processing))
            return null;

        stay.ActualCheckOut = actualCheckOut;

        // Ve doi xung voi luc nhan phong: o QUA HAN thi keo ngay tra cua don ve ngay roi di
        // that. Khong keo thi don van ghi ngay tra cu, trong khi khach o them may dem nua -
        // danh sach dat phong, lich phong va bao cao deu doc ra ngay sai.
        //
        // Chi keo DAI ra, khong bao gio rut ngan: tra som thi giu nguyen ngay tren don vi
        // nhung dem da giu cho van phai tra tien. Cua so cua don chi no ra cho vua thuc te.
        if (actualCheckOut.Date > stay.Reservation.CheckOutDate.Date)
        {
            stay.Reservation.CheckOutDate = actualCheckOut.Date;
        }

        stay.CheckedOutByUserId = userId;
        stay.Status = StayStatus.Completed;
        stay.Reservation.Status = ReservationStatus.Completed;
        stay.Reservation.Room.Status = RoomStatus.Cleaning;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return stay;
    }

    private static IQueryable<Stay> Query(HotelDbContext context) => context.Stays.AsNoTracking()
        .Include(s => s.Reservation).ThenInclude(r => r.Guest)
        .Include(s => s.Reservation).ThenInclude(r => r.Room).ThenInclude(r => r.RoomType)
        .Include(s => s.Invoice)
        .Include(s => s.ServiceOrders).ThenInclude(o => o.OrderDetails)
        .Include(s => s.Surcharges);

    /// <summary>Dat phong da xac nhan ma khach chua den quay - danh sach cho check-in.</summary>
    public async Task<List<Reservation>> GetArrivalsAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Reservations.AsNoTracking()
            .Include(r => r.Guest)
            .Include(r => r.Room).ThenInclude(room => room.RoomType)
            .Where(r => r.Status == ReservationStatus.Confirmed && r.Stay == null)
            .OrderBy(r => r.CheckInDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gia han luu tru: doi ngay tra tren don khi khach dang o muon o them.
    /// Khong co ham nay thi le tan phai check-out roi tao don moi, hong lich su va sai doanh thu.
    /// </summary>
    public async Task<(bool Ok, string Message)> ExtendAsync(int stayId, DateTime newCheckOut)
    {
        await using var context = HotelDbContextFactory.Create();
        var stay = await context.Stays
            .Include(s => s.Reservation).ThenInclude(r => r.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);

        if (stay == null) return (false, "Không tìm thấy lượt lưu trú.");
        if (stay.Status != StayStatus.Active) return (false, "Chỉ khách đang lưu trú mới gia hạn được.");

        var reservation = stay.Reservation;
        var target = newCheckOut.Date;

        if (target <= stay.ActualCheckIn.Date) return (false, "Ngày trả mới phải sau ngày khách nhận phòng.");
        if (target == reservation.CheckOutDate.Date) return (false, "Ngày trả mới trùng ngày trả hiện tại.");
        if (target < DateTime.Today) return (false, "Ngày trả mới không được ở quá khứ.");

        if (target > reservation.CheckOutDate.Date)
        {
            var busy = await context.Reservations.AsNoTracking().AnyAsync(other =>
                other.Id != reservation.Id
                && other.RoomId == reservation.RoomId
                && (other.Status == ReservationStatus.Pending
                    || other.Status == ReservationStatus.Confirmed
                    || other.Status == ReservationStatus.CheckedIn)
                && other.CheckInDate < target
                && other.CheckOutDate > reservation.CheckOutDate);
            if (busy) return (false, "Phòng đã có khách khác đặt trong khoảng muốn ở thêm.");
        }

        var oldDate = reservation.CheckOutDate;
        reservation.CheckOutDate = target;
        await context.SaveChangesAsync();
        var word = target > oldDate.Date ? "gia han" : "rut ngan";
        return (true, $"Da {word} phong {reservation.Room.RoomNumber} toi {target:dd/MM/yyyy}.");
    }
}
