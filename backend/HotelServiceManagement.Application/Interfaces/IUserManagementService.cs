using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Users;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IUserManagementService
    {
        Task<AuthServiceResult<IReadOnlyList<UserResponse>>> GetAllAsync();
        Task<AuthServiceResult<UserResponse>> GetByIdAsync(int id);
        Task<AuthServiceResult<UserResponse>> CreateAsync(CreateUserRequest request);
        Task<AuthServiceResult<UserResponse>> UpdateAsync(int id, UpdateUserRequest request);
        Task<AuthServiceResult<UserResponse>> UpdateStatusAsync(int id, UpdateUserStatusRequest request);
        Task<AuthServiceResult<AuthMessageResponse>> ResetPasswordAsync(int id, ResetUserPasswordRequest request);
    }
}
