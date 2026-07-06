using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly HotelDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(HotelDbContext context, IJwtService jwtService, IPasswordHasher passwordHasher)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return AuthServiceResult<LoginResponse>.Failure("Email and password are required.");
            }

            var normalizedEmail = NormalizeEmail(request.Email);
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || !user.IsActive || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return AuthServiceResult<LoginResponse>.Failure("Invalid email or password, or user is inactive.", 401);
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            return AuthServiceResult<LoginResponse>.Success(new LoginResponse
            {
                AccessToken = accessToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role?.RoleName ?? string.Empty
            });
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return MessageFailure("Current password and new password are required.");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return MessageFailure("ConfirmPassword must match NewPassword.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return MessageFailure("User not found.", 404);
            }

            if (!user.IsActive)
            {
                return MessageFailure("User is inactive.", 401);
            }

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return MessageFailure("Current password is incorrect.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return MessageSuccess("Password changed successfully.");
        }

        public async Task<AuthServiceResult<CurrentUserResponse>> GetCurrentUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return AuthServiceResult<CurrentUserResponse>.Failure("User not found.", 404);
            }

            return AuthServiceResult<CurrentUserResponse>.Success(new CurrentUserResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role?.RoleName ?? string.Empty,
                IsActive = user.IsActive
            });
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLower();
        }

        private static AuthServiceResult<AuthMessageResponse> MessageSuccess(string message)
        {
            return AuthServiceResult<AuthMessageResponse>.Success(new AuthMessageResponse { Message = message }, message);
        }

        private static AuthServiceResult<AuthMessageResponse> MessageFailure(string message, int statusCode = 400)
        {
            return AuthServiceResult<AuthMessageResponse>.Failure(message, statusCode);
        }
    }
}
