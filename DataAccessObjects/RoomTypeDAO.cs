using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    /// <summary>DAO Singleton cho RoomType - moi thao tac 1 DbContext ngan han.</summary>
    public class RoomTypeDAO
    {
        private static RoomTypeDAO? _instance;
        private static readonly object _lock = new();

        private RoomTypeDAO() { }

        public static RoomTypeDAO Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new RoomTypeDAO();
                }
            }
        }

        /// <summary>Tat ca loai phong kem so phong dang dung (cho tab quan ly).</summary>
        public async Task<List<RoomType>> GetAllAsync()
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.RoomTypes
                .AsNoTracking()
                .Include(rt => rt.Rooms)
                .OrderBy(rt => rt.Id)
                .ToListAsync();
        }

        /// <summary>Loai phong dang bat - cho combobox chon khi tao/sua phong.</summary>
        public async Task<List<RoomType>> GetActiveAsync()
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.RoomTypes
                .AsNoTracking()
                .Where(rt => rt.IsActive)
                .OrderBy(rt => rt.TypeName)
                .ToListAsync();
        }

        public async Task<RoomType?> GetByIdAsync(int id)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.RoomTypes
                .AsNoTracking()
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);
        }

        public async Task<bool> NameExistsAsync(string typeName, int? excludeId = null)
        {
            var normalized = typeName.Trim().ToLower();
            await using var context = HotelDbContextFactory.Create();
            return await context.RoomTypes.AnyAsync(rt =>
                rt.TypeName.ToLower() == normalized
                && (excludeId == null || rt.Id != excludeId));
        }

        /// <summary>Co dat phong dang hoat dong (Pending/Confirmed/CheckedIn) vuot suc chua moi khong.</summary>
        public async Task<bool> HasReservationExceedingCapacityAsync(int roomTypeId, int newCapacity)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Reservations.AnyAsync(r =>
                r.Room.RoomTypeId == roomTypeId
                && (r.Status == ReservationStatus.Pending
                    || r.Status == ReservationStatus.Confirmed
                    || r.Status == ReservationStatus.CheckedIn)
                && r.NumberOfGuests > newCapacity);
        }

        public async Task AddAsync(RoomType roomType)
        {
            await using var context = HotelDbContextFactory.Create();
            context.RoomTypes.Add(roomType);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RoomType roomType)
        {
            await using var context = HotelDbContextFactory.Create();
            context.RoomTypes.Update(roomType);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(RoomType roomType)
        {
            await using var context = HotelDbContextFactory.Create();
            context.RoomTypes.Remove(roomType);
            await context.SaveChangesAsync();
        }
    }
}
