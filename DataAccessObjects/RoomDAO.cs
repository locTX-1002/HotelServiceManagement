using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    /// <summary>DAO Singleton cho Room - moi thao tac 1 DbContext ngan han.</summary>
    public class RoomDAO
    {
        private static RoomDAO? _instance;
        private static readonly object _lock = new();

        private RoomDAO() { }

        public static RoomDAO Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new RoomDAO();
                }
            }
        }

        /// <summary>Tat ca phong kem loai phong, sap theo tang roi so phong.</summary>
        public async Task<List<Room>> GetAllAsync()
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();
        }

        public async Task<Room?> GetByIdAsync(int id)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> RoomNumberExistsAsync(string roomNumber, int? excludeId = null)
        {
            var normalized = roomNumber.Trim().ToLower();
            await using var context = HotelDbContextFactory.Create();
            return await context.Rooms.AnyAsync(r =>
                r.RoomNumber.ToLower() == normalized
                && (excludeId == null || r.Id != excludeId));
        }

        /// <summary>Phong dang co khach o thuc te (stay Active).</summary>
        public async Task<bool> HasActiveStayAsync(int roomId)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Stays.AnyAsync(s =>
                s.Reservation.RoomId == roomId && s.Status == StayStatus.Active);
        }

        public async Task<bool> HasCheckedInReservationAsync(int roomId)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Reservations.AnyAsync(r =>
                r.RoomId == roomId && r.Status == ReservationStatus.CheckedIn);
        }

        public async Task<bool> HasPendingOrConfirmedReservationAsync(int roomId)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Reservations.AnyAsync(r =>
                r.RoomId == roomId
                && (r.Status == ReservationStatus.Pending
                    || r.Status == ReservationStatus.Confirmed));
        }

        /// <summary>Co dat phong hoat dong vuot suc chua cua loai phong moi khong (khi doi loai).</summary>
        public async Task<bool> HasReservationExceedingCapacityAsync(int roomId, int capacity)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Reservations.AnyAsync(r =>
                r.RoomId == roomId
                && (r.Status == ReservationStatus.Pending
                    || r.Status == ReservationStatus.Confirmed
                    || r.Status == ReservationStatus.CheckedIn)
                && r.NumberOfGuests > capacity);
        }

        public async Task<bool> HasAnyReservationAsync(int roomId)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Reservations.AnyAsync(r => r.RoomId == roomId);
        }

        public async Task AddAsync(Room room)
        {
            await using var context = HotelDbContextFactory.Create();
            context.Rooms.Add(room);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Room room)
        {
            await using var context = HotelDbContextFactory.Create();
            context.Rooms.Update(room);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Room room)
        {
            await using var context = HotelDbContextFactory.Create();
            context.Rooms.Remove(room);
            await context.SaveChangesAsync();
        }
    }
}
