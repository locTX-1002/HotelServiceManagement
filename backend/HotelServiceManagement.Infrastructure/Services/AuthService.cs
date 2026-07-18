using System.Security.Cryptography;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        // Thoi han refresh token: khong tick "ghi nho" thi song ngan, co tick thi song lau.
        // Access token (JWT) luon ngan han nhu nhau bat ke rememberMe - refresh token moi la thu
        // quyet dinh phien song bao lau.
        private static readonly TimeSpan DefaultRefreshLifetime = TimeSpan.FromDays(1);
        private static readonly TimeSpan RememberMeRefreshLifetime = TimeSpan.FromDays(30);

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

            var refreshLifetime = request.RememberMe ? RememberMeRefreshLifetime : DefaultRefreshLifetime;
            var refreshToken = IssueRefreshToken(user.Id, DateTime.UtcNow.Add(refreshLifetime));
            await _context.SaveChangesAsync();

            return AuthServiceResult<LoginResponse>.Success(BuildLoginResponse(user, refreshToken));
        }

        public async Task<AuthServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return AuthServiceResult<LoginResponse>.Failure("Refresh token is required.", 401);
            }

            var existing = await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            // Khong ton tai, da bi thu hoi (logout hoac da dung de xoay vong 1 lan roi), hoac het han
            // deu tra ve cung 1 loi 401 - khong phan biet ly do de tranh lo thong tin cho ke tan cong.
            if (existing == null || existing.RevokedAt != null || existing.ExpiresAt < DateTime.UtcNow)
            {
                return AuthServiceResult<LoginResponse>.Failure("Refresh token is invalid or expired.", 401);
            }

            if (!existing.User.IsActive)
            {
                return AuthServiceResult<LoginResponse>.Failure("User is inactive.", 401);
            }

            // Xoay vong: thu hoi token cu, phat token moi nhung giu NGUYEN moc ExpiresAt goc (khong
            // "tre hoa" moi lan refresh - tranh phien song vinh vien mien la con hoat dong lien tuc).
            var newRefreshToken = IssueRefreshToken(existing.UserId, existing.ExpiresAt);
            existing.RevokedAt = DateTime.UtcNow;
            existing.ReplacedByToken = newRefreshToken.Token;
            await _context.SaveChangesAsync();

            return AuthServiceResult<LoginResponse>.Success(BuildLoginResponse(existing.User, newRefreshToken));
        }

        public async Task LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var existing = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            // Khong tim thay / da thu hoi roi cung coi nhu logout thanh cong - client luon xoa phien local.
            if (existing == null || existing.RevokedAt != null)
            {
                return;
            }

            existing.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
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

        private RefreshToken IssueRefreshToken(int userId, DateTime expiresAt)
        {
            var token = new RefreshToken
            {
                Token = GenerateSecureToken(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };
            _context.RefreshTokens.Add(token);
            return token;
        }

        // Chuoi ngau nhien 32 byte (256 bit), khong phai JWT - chi la 1 dinh danh doi chieu voi DB,
        // du kho de doan de dung lam bearer token cho luong refresh.
        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private LoginResponse BuildLoginResponse(User user, RefreshToken refreshToken)
        {
            var accessToken = _jwtService.GenerateAccessToken(user);
            return new LoginResponse
            {
                AccessToken = accessToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refreshToken.Token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role?.RoleName ?? string.Empty
            };
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
