using System.Threading.Tasks;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<CurrentUserResponse?> GetCurrentUserAsync(int userId);
    }
}
