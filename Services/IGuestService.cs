using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Services
{
    public interface IGuestService
    {
        Task<List<Guest>> SearchAsync(string? keyword);
        Task<Guest?> GetByIdAsync(int id);
        /// <summary>Tìm nhanh 1 khách khớp CHÍNH XÁC CCCD hoặc SĐT (dùng khi đặt phòng).</summary>
        Task<Guest?> FindExactAsync(string idOrPhone);
        Task<ServiceResult<Guest>> CreateAsync(string fullName, string phone, string? identityNumber, string? email);
        Task<ServiceResult<Guest>> UpdateAsync(int id, string fullName, string phone, string? identityNumber,
            string? email, GuestTag tag, string? tagNote);
    }
}
