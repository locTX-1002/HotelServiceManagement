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
}
