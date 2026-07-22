using BusinessObjects.Entities;
namespace Services; public interface IGuestAccountService { Task<ServiceResult<GuestAccount>> ActivateAsync(int guestId, string password); Task<ServiceResult<GuestAccount>> LoginAsync(string phone, string password); Task<ServiceResult> ChangePasswordAsync(int guestId, string currentPassword, string newPassword); }
