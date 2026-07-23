using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    /// <summary>
    /// DAO Singleton cho phu thu (do khach lam hong/mat, thu them luc tra phong).
    /// Moi thao tac ghi nam gon trong 1 DbContext de khong vo RowVersion.
    /// </summary>
    public class SurchargeDAO
    {
        private static SurchargeDAO? _instance;
        private static readonly object _lock = new();

        private SurchargeDAO() { }

        public static SurchargeDAO Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new SurchargeDAO();
                }
            }
        }

        /// <summary>Bang gia phu thu dang dung, de le tan chon.</summary>
        public async Task<List<SurchargeItem>> GetActiveItemsAsync()
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.SurchargeItems.AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        /// <summary>Cac dong phu thu da ghi cho mot luot luu tru.</summary>
        public async Task<List<Surcharge>> GetForStayAsync(int stayId)
        {
            await using var context = HotelDbContextFactory.Create();
            return await context.Surcharges.AsNoTracking()
                .Include(s => s.SurchargeItem)
                .Where(s => s.StayId == stayId)
                .OrderBy(s => s.Id)
                .ToListAsync();
        }

        /// <summary>Tong phu thu theo tung luot luu tru - dung de hien nhanh tren danh sach.</summary>
        public async Task<Dictionary<int, decimal>> GetTotalsAsync(IEnumerable<int> stayIds)
        {
            var ids = stayIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return [];
            }
            await using var context = HotelDbContextFactory.Create();
            return await context.Surcharges.AsNoTracking()
                .Where(s => ids.Contains(s.StayId))
                .GroupBy(s => s.StayId)
                .Select(g => new { StayId = g.Key, Total = g.Sum(x => x.Subtotal) })
                .ToDictionaryAsync(x => x.StayId, x => x.Total);
        }

        /// <summary>
        /// Ghi mot dong phu thu. Chup lai don gia tai thoi diem ghi nen sau nay
        /// bang gia doi thi dong da ghi van giu nguyen so tien cu.
        /// </summary>
        public async Task<(bool Ok, string Message)> AddAsync(
            int stayId, int surchargeItemId, int quantity, int createdByUserId)
        {
            if (quantity < 1)
            {
                return (false, "Số lượng phải từ 1 trở lên.");
            }

            await using var context = HotelDbContextFactory.Create();

            var stay = await context.Stays.FirstOrDefaultAsync(s => s.Id == stayId);
            if (stay == null)
            {
                return (false, "Không tìm thấy lượt lưu trú.");
            }
            if (stay.Status != StayStatus.Active)
            {
                return (false, "Khách đã trả phòng — không ghi thêm phụ thu được.");
            }

            var item = await context.SurchargeItems.FirstOrDefaultAsync(i => i.Id == surchargeItemId);
            if (item == null || !item.IsActive)
            {
                return (false, "Mục phụ thu không tồn tại hoặc đã ngừng dùng.");
            }

            context.Surcharges.Add(new Surcharge
            {
                StayId = stayId,
                SurchargeItemId = item.Id,
                Quantity = quantity,
                UnitPriceSnapshot = item.UnitPrice,
                Subtotal = item.UnitPrice * quantity,
                CreatedByUserId = createdByUserId > 0 ? createdByUserId : null,
                CreatedAt = DateTime.Now,
            });
            await context.SaveChangesAsync();

            return (true, $"Đã ghi phụ thu {item.Name} × {quantity}.");
        }

        /// <summary>Xoa mot dong phu thu ghi nham - chi con xoa duoc khi khach chua tra phong.</summary>
        public async Task<(bool Ok, string Message)> RemoveAsync(int surchargeId)
        {
            await using var context = HotelDbContextFactory.Create();

            var line = await context.Surcharges
                .Include(s => s.Stay)
                .FirstOrDefaultAsync(s => s.Id == surchargeId);
            if (line == null)
            {
                return (false, "Không tìm thấy dòng phụ thu.");
            }
            if (line.Stay.Status != StayStatus.Active)
            {
                return (false, "Khách đã trả phòng — không xoá phụ thu được nữa.");
            }

            context.Surcharges.Remove(line);
            await context.SaveChangesAsync();
            return (true, "Đã xoá dòng phụ thu.");
        }
    }
}
