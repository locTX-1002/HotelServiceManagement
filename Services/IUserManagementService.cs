using BusinessObjects.Entities;
using BusinessObjects.Enums;
namespace Services; public interface IUserManagementService { Task<ServiceResult<List<User>>> GetAllAsync();

    /// <summary>Vai tro duoc phep gan qua giao dien - doc tu DB, KHONG gom Admin.</summary>
    Task<ServiceResult<List<Role>>> GetAssignableRolesAsync();
 Task<ServiceResult<User>> CreateAsync(string fullName, string email, string password, int roleId, Gender gender = Gender.Male); Task<ServiceResult<User>> UpdateAsync(int id, string fullName, string email, int roleId, Gender gender = Gender.Male); Task<ServiceResult<User>> SetActiveAsync(int id, bool active); Task<ServiceResult> ResetPasswordAsync(int id, string password); }
