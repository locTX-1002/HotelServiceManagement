using BusinessObjects.Entities;
using Repositories;

namespace Services
{
    /// <summary>
    /// Phu thu: do khach lam hong hoac mat, le tan ghi lai luc kiem phong truoc khi tra.
    /// Rang buoc nghiep vu nam trong DAO vi phai doc-kiem-ghi trong cung 1 DbContext.
    /// </summary>
    public class SurchargeService : ISurchargeService
    {
        private readonly ISurchargeRepository _repo;

        public SurchargeService() : this(new SurchargeRepository()) { }

        public SurchargeService(ISurchargeRepository repo) => _repo = repo;

        public Task<List<SurchargeItem>> GetActiveItemsAsync() => _repo.GetActiveItemsAsync();

        public Task<List<Surcharge>> GetForStayAsync(int stayId) => _repo.GetForStayAsync(stayId);

        public Task<Dictionary<int, decimal>> GetTotalsAsync(IEnumerable<int> stayIds)
            => _repo.GetTotalsAsync(stayIds);

        public async Task<ServiceResult> AddAsync(int stayId, int surchargeItemId, int quantity, int createdByUserId)
        {
            if (stayId <= 0 || surchargeItemId <= 0)
            {
                return ServiceResult.Failure("Chưa chọn lượt lưu trú hoặc mục phụ thu.");
            }
            var (ok, message) = await _repo.AddAsync(stayId, surchargeItemId, quantity, createdByUserId);
            return ok ? ServiceResult.Success(message) : ServiceResult.Failure(message);
        }

        public async Task<ServiceResult> RemoveAsync(int surchargeId)
        {
            if (surchargeId <= 0)
            {
                return ServiceResult.Failure("Chưa chọn dòng phụ thu.");
            }
            var (ok, message) = await _repo.RemoveAsync(surchargeId);
            return ok ? ServiceResult.Success(message) : ServiceResult.Failure(message);
        }
    }
}
