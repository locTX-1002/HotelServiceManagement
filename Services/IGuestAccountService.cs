using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Entities;

namespace Services;

public interface IGuestAccountService
{
   
    Task<ServiceResult<List<GuestAccount>>> GetAllAsync();

   
    Task<ServiceResult<GuestAccount>> RegisterAsync(string fullName, string phoneNumber, string? email, string password, string confirmPassword);

  
    Task<ServiceResult<GuestAccount>> CreateByStaffAsync(string fullName, string phoneNumber, string? email, string password, string confirmPassword);

   
    Task<ServiceResult<GuestAccount>> ActivateAsync(int guestId, string password);

    Task<ServiceResult<GuestAccount>> LoginAsync(string phone, string password);

    Task<ServiceResult> ChangePasswordAsync(int guestId, string currentPassword, string newPassword);

    Task<ServiceResult> UpdateGuestInfoAsync(int guestId, string fullName, string phoneNumber, string? email);
}