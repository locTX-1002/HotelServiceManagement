using System.Security.Cryptography;
using Google.Apis.Auth;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        // Thoi han refresh token: khong tick "ghi nho" thi song ngan, co tick thi song lau.
        // Access token (JWT) luon ngan han nhu nhau bat ke rememberMe - refresh token moi la thu
        // quyet dinh phien song bao lau.
        private static readonly TimeSpan DefaultRefreshLifetime = TimeSpan.FromDays(1);
        private static readonly TimeSpan RememberMeRefreshLifetime = TimeSpan.FromDays(30);

        // Token dat lai mat khau song rat ngan - chi du thoi gian mo email va bam link.
        private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

        private readonly HotelDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(
            HotelDbContext context,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _configuration = configuration;
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

        // Dang nhap nhanh cho nhan vien DA CO tai khoan - khong tu tao tai khoan moi tu Google, vi
        // tai khoan nhan vien luon do Admin cap phat (theo dung thiet ke Auth MVP hien co), khong tu
        // dang ky cong khai. GoogleJsonWebSignature.ValidateAsync tu xac minh chu ky + audience +
        // thoi han cua id_token voi khoa cong khai xoay vong cua Google - khong tu ky lai thu cong.
        public async Task<AuthServiceResult<LoginResponse>> GoogleLoginAsync(string idToken, bool rememberMe)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return AuthServiceResult<LoginResponse>.Failure("Google ID token is required.");
            }

            var clientId = _configuration["Google:ClientId"];
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = string.IsNullOrWhiteSpace(clientId) ? null : new[] { clientId }
                });
            }
            catch (Exception)
            {
                // Google.Apis.Auth chi nem InvalidJwtException khi chu ky/audience/han sai, nhung nem
                // thang JsonReaderException (khong bat duoc rieng, khong co kieu cong khai on dinh de
                // catch dung) khi chuoi dau vao khong dung dang JWT (VD chuoi rac tu client). Bat rong
                // o day de moi truong hop deu tra ve 401 sach thay vi lo stack trace qua 500 - day la
                // bien xac thuc tu client, khong phai loi noi bo can biet chi tiet.
                return AuthServiceResult<LoginResponse>.Failure("Google token is invalid or expired.", 401);
            }

            if (!payload.EmailVerified)
            {
                return AuthServiceResult<LoginResponse>.Failure("Google email is not verified.", 401);
            }

            var normalizedEmail = NormalizeEmail(payload.Email);
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || !user.IsActive)
            {
                return AuthServiceResult<LoginResponse>.Failure(
                    "No staff account found for this Google email. Ask an Admin to create one first.", 404);
            }

            var refreshLifetime = rememberMe ? RememberMeRefreshLifetime : DefaultRefreshLifetime;
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

        public async Task<AuthServiceResult<AuthMessageResponse>> ForgotPasswordAsync(string email)
        {
            const string genericMessage = "If that email exists, a reset link has been sent.";

            if (string.IsNullOrWhiteSpace(email))
            {
                return MessageFailure("Email is required.");
            }

            var normalizedEmail = NormalizeEmail(email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            // Luon tra ve cung 1 thong bao chung du email co ton tai hay khong - tranh lo cho ke tan
            // cong do email nao da dang ky trong he thong (user enumeration).
            if (user == null || !user.IsActive)
            {
                return MessageSuccess(genericMessage);
            }

            var token = new PasswordResetToken
            {
                Token = GenerateSecureToken(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(PasswordResetTokenLifetime)
            };
            _context.PasswordResetTokens.Add(token);
            await _context.SaveChangesAsync();

            var frontendOrigin = _configuration["Cors:FrontendOrigin"] ?? "http://localhost:5173";
            var resetLink = $"{frontendOrigin}/reset-password?token={Uri.EscapeDataString(token.Token)}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink);

            return MessageSuccess(genericMessage);
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> ResetPasswordWithTokenAsync(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return MessageFailure("New password must be at least 6 characters.");
            }

            var existing = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (existing == null || existing.UsedAt != null || existing.ExpiresAt < DateTime.UtcNow)
            {
                return MessageFailure("Reset link is invalid or expired.", 401);
            }

            existing.User.PasswordHash = _passwordHasher.HashPassword(newPassword);
            existing.UsedAt = DateTime.UtcNow;

            // Doi mat khau xong thu hoi toan bo refresh token cu - thiet bi/nguoi khac dang giu
            // refresh token cu (VD mat khau bi lo) se khong the tiep tuc lam moi phien duoc nua.
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == existing.UserId && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var rt in activeTokens)
            {
                rt.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return MessageSuccess("Password reset successfully.");
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
