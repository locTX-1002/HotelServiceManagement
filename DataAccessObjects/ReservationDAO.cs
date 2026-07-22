using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    /// <summary>
    /// DAO Singleton cho Reservation. Cac thao tac ghi (create/update/status) lam TRON trong
    /// 1 context de RowVersion khop + refresh trang thai phong ngay trong cung transaction.
    /// </summary>
    public class ReservationDAO
    {
        private static ReservationDAO? _instance;
        private static readonly object _lock = new();
        private ReservationDAO() { }

        public static ReservationDAO Instance
        {
            get { lock (_lock) { return _instance ??= new ReservationDAO(); } }
        }

        private static readonly ReservationStatus[] BlockingStatuses =
            [ReservationStatus.Pending, ReservationStatus.Confirmed, ReservationStatus.CheckedIn];

        private static IQueryable<Reservation> Query(HotelDbContext c) => c.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room).ThenInclude(r => r.RoomType);

        public async Task<List<Reservation>> GetAllAsync()
        {
            await using var context = HotelDbContextFactory.Create();
            return await Query(context).AsNoTracking()
                .OrderByDescending(r => r.CheckInDate).ThenByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            await using var context = HotelDbContextFactory.Create();
            return await Query(context).AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> HasStayAsync(int reservationId)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Stays.AnyAsync(s => s.ReservationId == reservationId);
        }

        /// <summary>
        /// Phong con trong trong khoang ngay: khong dat phong hoat dong trung khoang (overlap)
        /// VA khong bi khach dang o thuc te chiem (active-stay occupancy). Port nguyen tu web.
        /// </summary>
        public async Task<List<Room>> GetAvailableRoomsAsync(
            DateTime checkIn, DateTime checkOut, int? roomTypeId, int? capacity)
        {
            await using var context = HotelDbContextFactory.Create();
            var query = context.Rooms.AsNoTracking().Include(r => r.RoomType)
                .Where(r => r.IsActive && r.RoomType.IsActive && r.Status != RoomStatus.Maintenance);

            if (roomTypeId.HasValue)
            {
                query = query.Where(r => r.RoomTypeId == roomTypeId.Value);
            }
            if (capacity.HasValue)
            {
                query = query.Where(r => r.RoomType.Capacity >= capacity.Value);
            }

            var occupancyFloor = DateTime.Today.AddDays(1);
            return await query
                .Where(room => !context.Reservations.Any(res =>
                    res.RoomId == room.Id
                    && BlockingStatuses.Contains(res.Status)
                    && res.CheckInDate < checkOut && res.CheckOutDate > checkIn))
                .Where(room => !context.Stays.Any(stay =>
                    stay.Reservation.RoomId == room.Id
                    && stay.Status == StayStatus.Active
                    && stay.ActualCheckIn < checkOut
                    && checkIn < (stay.Reservation.CheckOutDate > occupancyFloor
                        ? stay.Reservation.CheckOutDate : occupancyFloor)))
                .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
                .ToListAsync();
        }

        public async Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeId)
        {
            await using var context = HotelDbContextFactory.Create();
            var resOverlap = await context.Reservations.AnyAsync(res =>
                res.RoomId == roomId
                && (excludeId == null || res.Id != excludeId)
                && BlockingStatuses.Contains(res.Status)
                && res.CheckInDate < checkOut && res.CheckOutDate > checkIn);
            if (resOverlap)
            {
                return true;
            }

            var floor = DateTime.Today.AddDays(1);
            return await context.Stays.AnyAsync(stay =>
                stay.Reservation.RoomId == roomId
                && (excludeId == null || stay.ReservationId != excludeId)
                && stay.Status == StayStatus.Active
                && stay.ActualCheckIn < checkOut
                && checkIn < (stay.Reservation.CheckOutDate > floor ? stay.Reservation.CheckOutDate : floor));
        }

        public async Task<Room?> GetReservableRoomAsync(int roomId)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Rooms.AsNoTracking().Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive && r.RoomType.IsActive);
        }

        public async Task<Guest?> GetGuestAsync(int guestId)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Guests.AsNoTracking().FirstOrDefaultAsync(g => g.Id == guestId);
        }

        public async Task<bool> BookingCodeExistsAsync(string code)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Reservations.AnyAsync(r => r.BookingCode == code);
        }

        /// <summary>Tao dat phong + refresh trang thai phong trong cung context.</summary>
        public async Task<int> CreateAsync(Reservation reservation)
        {
            await using var context = HotelDbContextFactory.Create();
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();
            await RefreshRoomStatusAsync(context, reservation.RoomId);
            await context.SaveChangesAsync();
            return reservation.Id;
        }

        /// <summary>Sua dat phong (Pending/Confirmed) + refresh trang thai phong cu va moi.</summary>
        public async Task UpdateAsync(int id, int guestId, int roomId, int numberOfGuests,
            DateTime checkIn, DateTime checkOut, ReservationStatus status, string? specialRequests)
        {
            await using var context = HotelDbContextFactory.Create();
            var res = await context.Reservations.FirstAsync(r => r.Id == id);
            var oldRoomId = res.RoomId;
            res.GuestId = guestId;
            res.RoomId = roomId;
            res.NumberOfGuests = numberOfGuests;
            res.CheckInDate = checkIn;
            res.CheckOutDate = checkOut;
            res.Status = status;
            res.SpecialRequests = specialRequests;
            await context.SaveChangesAsync();

            await RefreshRoomStatusAsync(context, oldRoomId);
            if (roomId != oldRoomId)
            {
                await RefreshRoomStatusAsync(context, roomId);
            }
            await context.SaveChangesAsync();
        }

        /// <summary>Doi trang thai (Cancel/NoShow/Confirm) + refresh phong.</summary>
        public async Task SetStatusAsync(int id, ReservationStatus status)
        {
            await using var context = HotelDbContextFactory.Create();
            var res = await context.Reservations.FirstAsync(r => r.Id == id);
            res.Status = status;
            await context.SaveChangesAsync();
            await RefreshRoomStatusAsync(context, res.RoomId);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Tinh lai trang thai phong theo nghiep vu (KHONG de len Cleaning/Maintenance dang van hanh):
        /// co khach o -> Occupied; co dat phong cho/da xac nhan -> Reserved; con lai -> Available.
        /// </summary>
        private static async Task RefreshRoomStatusAsync(HotelDbContext context, int roomId)
        {
            var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null || room.Status is RoomStatus.Maintenance or RoomStatus.Cleaning)
            {
                return;
            }

            var occupied = await context.Stays.AnyAsync(s =>
                s.Reservation.RoomId == roomId && s.Status == StayStatus.Active)
                || await context.Reservations.AnyAsync(r =>
                    r.RoomId == roomId && r.Status == ReservationStatus.CheckedIn);
            if (occupied)
            {
                room.Status = RoomStatus.Occupied;
                return;
            }

            var reserved = await context.Reservations.AnyAsync(r =>
                r.RoomId == roomId
                && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed));
            room.Status = reserved ? RoomStatus.Reserved : RoomStatus.Available;
        }
    }
}
