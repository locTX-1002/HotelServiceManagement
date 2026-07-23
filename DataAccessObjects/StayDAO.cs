using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    /// <summary>
    /// DAO Singleton cho Stay (lưu trú). Check-in/out lam TRON trong 1 context: doi trang thai
    /// stay + reservation + room cung transaction. Ket qua tra ve (ok, message) da tieng Viet.
    /// </summary>
    public class StayDAO
    {
        private static StayDAO? _instance;
        private static readonly object _lock = new();
        private StayDAO() { }

        public static StayDAO Instance
        {
            get { lock (_lock) { return _instance ??= new StayDAO(); } }
        }

        /// <summary>Cac dat phong Confirmed chua check-in (danh sach "hom nay den").</summary>
        public async Task<List<Reservation>> GetPendingArrivalsAsync()
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Reservations.AsNoTracking()
                .Include(r => r.Guest)
                .Include(r => r.Room).ThenInclude(r => r.RoomType)
                .Where(r => r.Status == ReservationStatus.Confirmed && r.Stay == null)
                .OrderBy(r => r.CheckInDate)
                .ToListAsync();
        }

        /// <summary>Cac lưu trú dang hoat dong (danh sach "dang o").</summary>
        public async Task<List<Stay>> GetActiveAsync()
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Stays.AsNoTracking()
                .Include(s => s.Reservation).ThenInclude(r => r.Guest)
                .Include(s => s.Reservation).ThenInclude(r => r.Room).ThenInclude(r => r.RoomType)
                .Where(s => s.Status == StayStatus.Active)
                .OrderBy(s => s.ActualCheckIn)
                .ToListAsync();
        }

        /// <summary>
        /// Check-in: chi tu Confirmed, phong phai san sang (Available/Reserved), khong check-in
        /// truoc ngay dat, khong check-in sau ngay tra. Tra ve ly do cu the neu phong chua san sang.
        /// </summary>
        public async Task<(bool Ok, string Message)> CheckInAsync(
            int reservationId, DateTime actualCheckIn, int checkedInByUserId)
        {
            await using var context = HotelDbContextFactory.Create();
            var res = await context.Reservations
                .Include(r => r.Room)
                .Include(r => r.Stay)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (res == null)
            {
                return (false, "Không tìm thấy đặt phòng.");
            }
            if (res.Status != ReservationStatus.Confirmed)
            {
                return (false, "Chỉ đặt phòng đã xác nhận mới check-in được.");
            }
            if (res.Stay != null)
            {
                return (false, "Đặt phòng này đã check-in rồi.");
            }
            if (!res.Room.IsActive || res.Room.Status is not (RoomStatus.Available or RoomStatus.Reserved))
            {
                var reason = !res.Room.IsActive ? "phòng đang ngừng dùng"
                    : res.Room.Status switch
                    {
                        RoomStatus.Occupied => "khách trước chưa trả phòng",
                        RoomStatus.Cleaning => "phòng chưa dọn xong",
                        RoomStatus.Maintenance => "phòng đang bảo trì",
                        _ => "phòng chưa sẵn sàng",
                    };
                return (false, $"Chưa check-in được phòng {res.Room.RoomNumber}: {reason}.");
            }
            if (actualCheckIn >= res.CheckOutDate)
            {
                return (false, "Giờ check-in phải trước ngày trả phòng.");
            }
            if (actualCheckIn.Date < res.CheckInDate.Date)
            {
                return (false, $"Không thể check-in trước ngày đặt ({res.CheckInDate:dd/MM/yyyy}).");
            }

            context.Stays.Add(new Stay
            {
                ReservationId = res.Id,
                ActualCheckIn = actualCheckIn,
                CheckedInByUserId = checkedInByUserId,
                Status = StayStatus.Active,
            });
            res.Status = ReservationStatus.CheckedIn;
            res.Room.Status = RoomStatus.Occupied;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, "Đặt phòng vừa bị thao tác khác thay đổi. Tải lại rồi thử lại.");
            }
            return (true, $"Đã check-in phòng {res.Room.RoomNumber}.");
        }

        /// <summary>
        /// Check-out: stay Active -> Completed, reservation -> Completed, phong -> Đang dọn.
        /// KHONG tao hoa don o day (tach sang module Hoa don) - le tan tao hoa don tu stay da hoan tat.
        /// </summary>
        public async Task<(bool Ok, string Message)> CheckOutAsync(int stayId, int checkedOutByUserId)
        {
            await using var context = HotelDbContextFactory.Create();
            var stay = await context.Stays
                .Include(s => s.Reservation).ThenInclude(r => r.Room)
                .FirstOrDefaultAsync(s => s.Id == stayId);

            if (stay == null)
            {
                return (false, "Không tìm thấy lượt lưu trú.");
            }
            if (stay.Status != StayStatus.Active)
            {
                return (false, "Chỉ lượt đang lưu trú mới check-out được.");
            }

            stay.ActualCheckOut = DateTime.Now;
            stay.Status = StayStatus.Completed;
            stay.CheckedOutByUserId = checkedOutByUserId;
            stay.Reservation.Status = ReservationStatus.Completed;
            stay.Reservation.Room.Status = RoomStatus.Cleaning;

            await context.SaveChangesAsync();
            return (true, $"Đã check-out phòng {stay.Reservation.Room.RoomNumber}. Phòng chuyển sang Đang dọn.");
        }

        /// <summary>
        /// Gia han luu tru: doi ngay tra cua don khi khach dang o muon o them.
        /// Truoc day khach dang o khong sua don duoc nen le tan phai check-out roi tao don moi,
        /// lam hong lich su va sai doanh thu.
        /// </summary>
        public async Task<(bool Ok, string Message)> ExtendAsync(int stayId, DateTime newCheckOut)
        {
            await using var context = HotelDbContextFactory.Create();

            var stay = await context.Stays
                .Include(s => s.Reservation).ThenInclude(r => r.Room)
                .FirstOrDefaultAsync(s => s.Id == stayId);

            if (stay == null)
            {
                return (false, "Không tìm thấy lượt lưu trú.");
            }
            if (stay.Status != StayStatus.Active)
            {
                return (false, "Chỉ khách đang lưu trú mới gia hạn được.");
            }

            var reservation = stay.Reservation;
            var target = newCheckOut.Date;

            if (target <= stay.ActualCheckIn.Date)
            {
                return (false, "Ngày trả mới phải sau ngày khách nhận phòng.");
            }
            if (target == reservation.CheckOutDate.Date)
            {
                return (false, "Ngày trả mới trùng với ngày trả hiện tại.");
            }
            if (target < DateTime.Today)
            {
                return (false, "Ngày trả mới không được ở quá khứ.");
            }

            // Rut ngan thi khong can kiem tra dat chong, chi keo dai moi can
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

                if (busy)
                {
                    return (false,
                        "Phòng đã có khách khác đặt trong khoảng muốn ở thêm. "
                        + "Chọn ngày ngắn hơn hoặc đổi khách sang phòng khác.");
                }
            }

            var oldDate = reservation.CheckOutDate;
            reservation.CheckOutDate = target;
            await context.SaveChangesAsync();

            var word = target > oldDate.Date ? "gia hạn" : "rút ngắn";
            return (true, $"Đã {word} phòng {reservation.Room.RoomNumber} tới {target:dd/MM/yyyy}.");
        }
    }
}
