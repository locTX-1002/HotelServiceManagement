using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    /// <summary>DAO Singleton cho Guest - khach hang.</summary>
    public class GuestDAO
    {
        private static GuestDAO? _instance;
        private static readonly object _lock = new();
        private GuestDAO() { }

        public static GuestDAO Instance
        {
            get { lock (_lock) { return _instance ??= new GuestDAO(); } }
        }

        public async Task<List<Guest>> SearchAsync(string? keyword)
        {
            await using var context = HotelDbContextFactory.Create();
            var query = context.Guests.AsNoTracking().Include(g => g.Reservations).AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLower();
                query = query.Where(g => g.FullName.ToLower().Contains(k)
                    || g.PhoneNumber.Contains(k)
                    || (g.IdentityNumber != null && g.IdentityNumber.Contains(k))
                    || (g.Email != null && g.Email.ToLower().Contains(k)));
            }
            return await query.OrderBy(g => g.FullName).ToListAsync();
        }

        public async Task<Guest?> GetByIdAsync(int id)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Guests.AsNoTracking()
                .Include(g => g.Reservations).ThenInclude(r => r.Room)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<bool> IdentityExistsAsync(string identityNumber, int? excludeId = null)
        {
            var n = identityNumber.Trim();
            await using var context = HotelDbContextFactory.Create();
            return await context.Guests.AnyAsync(g =>
                g.IdentityNumber == n && (excludeId == null || g.Id != excludeId));
        }

        public async Task<Guest> AddAsync(Guest guest)
        {
            await using var context = HotelDbContextFactory.Create();
            context.Guests.Add(guest);
            await context.SaveChangesAsync();
            return guest;
        }

        public async Task UpdateAsync(Guest guest)
        {
            await using var context = HotelDbContextFactory.Create();
            context.Guests.Update(guest);
            await context.SaveChangesAsync();
        }
    }
}
