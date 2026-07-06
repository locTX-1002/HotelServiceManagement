using System.Threading.Tasks;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUserRepository userRepository, IJwtService jwtService, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailWithRoleAsync(email);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdWithRoleAsync(id);
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailWithRoleAsync(request.Email);
            if (user == null)
            {
                return null;
            }

            if (!user.IsActive)
            {
                return null;
            }

            bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return null;
            }

            var accessToken = _jwtService.GenerateAccessToken(user);

            return new LoginResponse
            {
                AccessToken = accessToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role?.RoleName ?? string.Empty
            };
        }

        public async Task<CurrentUserResponse?> GetCurrentUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdWithRoleAsync(userId);
            if (user == null)
            {
                return null;
            }

            return new CurrentUserResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role?.RoleName ?? string.Empty,
                IsActive = user.IsActive
            };
        }
    }
}
