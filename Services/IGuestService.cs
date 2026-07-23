using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Services;

public interface IGuestService
{
    Task<List<Guest>> GetAllAsync();
    Task<List<Guest>> SearchAsync(string keyword);
    Task<ServiceResult<Guest>> CreateAsync(string fullName, string? email, string phoneNumber,
        string? identityNumber, GuestTag tag, string? tagNote);
    Task<ServiceResult<Guest>> UpdateAsync(int id, string fullName, string? email, string phoneNumber,
        string? identityNumber, GuestTag tag, string? tagNote);
    Task<ServiceResult> DeleteAsync(int id);

    /// <summary>Tra dung mot khach theo CCCD hoac SDT chinh xac, khong ra thi tra null.</summary>
    Task<Guest?> FindExactAsync(string idOrPhone);
}
