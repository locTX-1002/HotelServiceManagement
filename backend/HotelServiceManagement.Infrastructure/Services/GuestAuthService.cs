using System.Security.Cryptography;
using Google.Apis.Auth;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.GuestAuth;
using HotelServiceManagement.Application.DTOs.Reservations;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class GuestAuthService : IGuestAuthService
    {
        // Khong co "remember me" nhu nhan vien - khach hiem khi dang nhap moi ngay, co dinh 1 thoi
        // han du dai la vua, khong can them lua chon lam phuc tap UI dang ky/dang nhap.
        private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
        private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

        private readonly HotelDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public GuestAuthService(
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

        // Tu dang ky tu do bang SDT - khong con doi hoi chung minh so huu 1 dat phong cu the (xac
        // minh danh tinh that dien ra o quay le tan luc check-in, cong khach online chi la lop tien
        // ich). Khop theo SDT voi Guest le tan da tao san (neu co) de khach thay ngay dat phong cu;
        // khong khop thi tao Guest moi trong. Khop nhieu hon 1 Guest (SDT khong duoc rang buoc duy
        // nhat trong DB, hiem gap) thi CHU DONG khong gan vao ai ca - tranh gan nham nguoi la, tao
        // Guest moi rieng cho chac.
        public async Task<AuthServiceResult<GuestAuthResponse>> RegisterAsync(GuestRegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Full name is required.");
            }

            var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Phone number is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Password must be at least 6 characters.");
            }

            var matches = await _context.Guests.Where(g => g.PhoneNumber == phoneNumber).ToListAsync();
            Guest guest;
            if (matches.Count == 1)
            {
                guest = matches[0];
                var alreadyHasAccount = await _context.GuestAccounts.AnyAsync(a => a.GuestId == guest.Id);
                if (alreadyHasAccount)
                {
                    return AuthServiceResult<GuestAuthResponse>.Failure(
                        "This phone number already has an account. Please login instead.", 409);
                }
            }
            else
            {
                guest = new Guest
                {
                    FullName = request.FullName.Trim(),
                    Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                    PhoneNumber = phoneNumber
                };
                _context.Guests.Add(guest);
            }

            var account = new GuestAccount
            {
                Guest = guest,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };
            _context.GuestAccounts.Add(account);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Request khac vua dang ky xong cho dung SDT nay trong luc cho SaveChangesAsync.
                var stillExists = await _context.GuestAccounts.AnyAsync(a => a.Guest.PhoneNumber == phoneNumber);
                if (!stillExists)
                {
                    throw;
                }

                return AuthServiceResult<GuestAuthResponse>.Failure(
                    "This phone number already has an account. Please login instead.", 409);
            }

            var refreshToken = IssueRefreshToken(account.Id, DateTime.UtcNow.Add(RefreshLifetime));
            await _context.SaveChangesAsync();

            return AuthServiceResult<GuestAuthResponse>.Success(
                BuildAuthResponse(guest, refreshToken), "Guest account created successfully.");
        }

        public async Task<AuthServiceResult<GuestAuthResponse>> LoginAsync(GuestLoginRequest request)
        {
            var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
            var account = await _context.GuestAccounts
                .Include(a => a.Guest)
                .FirstOrDefaultAsync(a => a.Guest.PhoneNumber == phoneNumber);

            // PasswordHash null = tai khoan chi dang nhap bang Google, chua tung dat mat khau -
            // tu nhien khong dang nhap duoc bang duong nay, khong phai loi.
            if (account?.PasswordHash == null || !_passwordHasher.VerifyPassword(request.Password, account.PasswordHash))
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Phone number or password is incorrect.", 401);
            }

            account.LastLoginAt = DateTime.UtcNow;
            var refreshToken = IssueRefreshToken(account.Id, DateTime.UtcNow.Add(RefreshLifetime));
            await _context.SaveChangesAsync();

            return AuthServiceResult<GuestAuthResponse>.Success(BuildAuthResponse(account.Guest, refreshToken));
        }

        // Dang nhap/dang ky bang Google. Lan dau tien dang nhap bang 1 tai khoan Google cu the (chua
        // co GoogleSubjectId nao khop) BAT BUOC phai co SDT kem theo - Google khong tra ve so dien
        // thoai nen can SDT de khop dung vao Guest le tan da tao san (neu co) hoac tao Guest moi,
        // giong het logic RegisterAsync. Thieu SDT o lan dau thi tra 428 de FE hien o nhap SDT roi
        // goi lai VOI CUNG idToken do (id_token con hieu luc ~1 tieng, khong can dang nhap Google lai).
        public async Task<AuthServiceResult<GuestAuthResponse>> GoogleLoginAsync(
            string idToken, string? phoneNumber, string? fullName, string? password)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Google ID token is required.");
            }

            // Rong = bo qua (khong dat mat khau luc nay, van dang nhap lai duoc qua Google). Co nhap
            // thi phai dat rang buoc do dai giong het RegisterAsync/ResetPasswordWithTokenAsync.
            var trimmedPassword = string.IsNullOrWhiteSpace(password) ? null : password.Trim();
            if (trimmedPassword != null && trimmedPassword.Length < 6)
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Password must be at least 6 characters.");
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
                // Google.Apis.Auth nem InvalidJwtException khi chu ky/audience/han sai, nhung nem
                // thang JsonReaderException (khong co kieu on dinh de catch rieng) khi chuoi dau vao
                // khong dung dang JWT - bat rong o day, day la bien xac thuc tu client.
                return AuthServiceResult<GuestAuthResponse>.Failure("Google token is invalid or expired.", 401);
            }

            if (!payload.EmailVerified)
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Google email is not verified.", 401);
            }

            var existingLink = await _context.GuestAccounts
                .Include(a => a.Guest)
                .FirstOrDefaultAsync(a => a.GoogleSubjectId == payload.Subject);

            if (existingLink != null)
            {
                existingLink.LastLoginAt = DateTime.UtcNow;
                var rt = IssueRefreshToken(existingLink.Id, DateTime.UtcNow.Add(RefreshLifetime));
                await _context.SaveChangesAsync();
                return AuthServiceResult<GuestAuthResponse>.Success(BuildAuthResponse(existingLink.Guest, rt));
            }

            var normalizedPhone = phoneNumber?.Trim() ?? string.Empty;
            if (normalizedPhone.Length == 0)
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("PHONE_REQUIRED", 428);
            }

            var matches = await _context.Guests.Where(g => g.PhoneNumber == normalizedPhone).ToListAsync();
            Guest guest;
            GuestAccount? accountForGuest = null;
            if (matches.Count == 1)
            {
                guest = matches[0];
                accountForGuest = await _context.GuestAccounts.FirstOrDefaultAsync(a => a.GuestId == guest.Id);
            }
            else
            {
                // Guest hoan toan moi (khong khop SDT nao co san) - uu tien ten khach tu sua tren man
                // hinh hoan tat ho so, sau do moi den ten Google, cuoi cung fallback mac dinh.
                var resolvedName = !string.IsNullOrWhiteSpace(fullName)
                    ? fullName.Trim()
                    : (string.IsNullOrWhiteSpace(payload.Name) ? "Khach Google" : payload.Name);
                guest = new Guest
                {
                    FullName = resolvedName,
                    Email = payload.Email,
                    PhoneNumber = normalizedPhone
                };
                _context.Guests.Add(guest);
            }

            GuestAccount targetAccount;
            if (accountForGuest != null)
            {
                accountForGuest.GoogleSubjectId = payload.Subject;
                targetAccount = accountForGuest;
            }
            else
            {
                targetAccount = new GuestAccount { Guest = guest, GoogleSubjectId = payload.Subject, CreatedAt = DateTime.UtcNow };
                _context.GuestAccounts.Add(targetAccount);
            }

            if (trimmedPassword != null)
            {
                targetAccount.PasswordHash = _passwordHasher.HashPassword(trimmedPassword);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return AuthServiceResult<GuestAuthResponse>.Failure(
                    "This Google account or phone number is already linked to another account.", 409);
            }

            var refreshToken = IssueRefreshToken(targetAccount.Id, DateTime.UtcNow.Add(RefreshLifetime));
            await _context.SaveChangesAsync();

            return AuthServiceResult<GuestAuthResponse>.Success(BuildAuthResponse(guest, refreshToken));
        }

        public async Task<AuthServiceResult<GuestAuthResponse>> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Refresh token is required.", 401);
            }

            var existing = await _context.GuestRefreshTokens
                .Include(rt => rt.GuestAccount).ThenInclude(a => a.Guest)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (existing == null || existing.RevokedAt != null || existing.ExpiresAt < DateTime.UtcNow)
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Refresh token is invalid or expired.", 401);
            }

            // Xoay vong: giu nguyen moc ExpiresAt goc, giong het pattern da dung cho RefreshToken nhan vien.
            var newRefreshToken = IssueRefreshToken(existing.GuestAccountId, existing.ExpiresAt);
            existing.RevokedAt = DateTime.UtcNow;
            existing.ReplacedByToken = newRefreshToken.Token;
            await _context.SaveChangesAsync();

            return AuthServiceResult<GuestAuthResponse>.Success(
                BuildAuthResponse(existing.GuestAccount.Guest, newRefreshToken));
        }

        public async Task LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var existing = await _context.GuestRefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            if (existing == null || existing.RevokedAt != null)
            {
                return;
            }

            existing.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> ForgotPasswordAsync(string email)
        {
            const string genericMessage = "If that email has an account, a reset link has been sent.";

            var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
            if (normalizedEmail.Length == 0)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure("Email is required.");
            }

            // Email khong unique tren bang Guests (1 gia dinh co the dung chung 1 email) - uu tien tai
            // khoan da dat mat khau, sau do la tai khoan tao gan nhat.
            var account = await _context.GuestAccounts
                .Include(a => a.Guest)
                .Where(a => a.Guest.Email != null && a.Guest.Email.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.PasswordHash != null)
                .ThenByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            // Tra ve cung 1 thong bao chung du tai khoan co ton tai hay khong - tranh lo cho ke tan
            // cong biet email nao da dang ky trong he thong.
            if (account == null)
            {
                return AuthServiceResult<AuthMessageResponse>.Success(new AuthMessageResponse { Message = genericMessage });
            }

            var token = new GuestPasswordResetToken
            {
                Token = GenerateSecureToken(),
                GuestAccountId = account.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(PasswordResetTokenLifetime)
            };
            _context.GuestPasswordResetTokens.Add(token);
            await _context.SaveChangesAsync();

            var frontendOrigin = _configuration["Cors:FrontendOrigin"] ?? "http://localhost:5173";
            var resetLink = $"{frontendOrigin}/guest/dat-lai-mat-khau?token={Uri.EscapeDataString(token.Token)}";
            await _emailService.SendPasswordResetEmailAsync(account.Guest.Email, account.Guest.FullName, resetLink);

            return AuthServiceResult<AuthMessageResponse>.Success(new AuthMessageResponse { Message = genericMessage });
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> ResetPasswordWithTokenAsync(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure("New password must be at least 6 characters.");
            }

            var existing = await _context.GuestPasswordResetTokens
                .Include(t => t.GuestAccount)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (existing == null || existing.UsedAt != null || existing.ExpiresAt < DateTime.UtcNow)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure("Reset link is invalid or expired.", 401);
            }

            existing.GuestAccount.PasswordHash = _passwordHasher.HashPassword(newPassword);
            existing.UsedAt = DateTime.UtcNow;

            var activeTokens = await _context.GuestRefreshTokens
                .Where(rt => rt.GuestAccountId == existing.GuestAccountId && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var rt in activeTokens)
            {
                rt.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return AuthServiceResult<AuthMessageResponse>.Success(
                new AuthMessageResponse { Message = "Password reset successfully." });
        }

        public async Task<AuthServiceResult<IReadOnlyList<ReservationResponse>>> GetMyReservationsAsync(int guestId)
        {
            var reservations = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Guest)
                .Include(r => r.Room).ThenInclude(r => r.RoomType)
                .Where(r => r.GuestId == guestId)
                .OrderByDescending(r => r.CheckInDate)
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<ReservationResponse>>.Success(
                reservations.Select(ToReservationResponse).ToList());
        }

        public async Task<AuthServiceResult<GuestProfileResponse>> GetMyProfileAsync(int guestId)
        {
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Id == guestId);
            if (guest == null)
            {
                return AuthServiceResult<GuestProfileResponse>.Failure("Guest not found.", 404);
            }

            var hasPassword = await _context.GuestAccounts.AnyAsync(a => a.GuestId == guestId && a.PasswordHash != null);
            return AuthServiceResult<GuestProfileResponse>.Success(ToProfileResponse(guest, hasPassword));
        }

        public async Task<AuthServiceResult<GuestProfileResponse>> UpdateMyProfileAsync(int guestId, UpdateGuestProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.FullName))
            {
                return AuthServiceResult<GuestProfileResponse>.Failure("Full name is required.");
            }

            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Id == guestId);
            if (guest == null)
            {
                return AuthServiceResult<GuestProfileResponse>.Failure("Guest not found.", 404);
            }

            guest.FullName = request.FullName.Trim();
            guest.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            await _context.SaveChangesAsync();

            var hasPassword = await _context.GuestAccounts.AnyAsync(a => a.GuestId == guestId && a.PasswordHash != null);
            return AuthServiceResult<GuestProfileResponse>.Success(
                ToProfileResponse(guest, hasPassword), "Profile updated successfully.");
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> ChangeMyPasswordAsync(int guestId, GuestChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.NewPassword) || request.NewPassword.Length < 6)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure("New password must be at least 6 characters.");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure("ConfirmPassword must match NewPassword.");
            }

            var account = await _context.GuestAccounts.FirstOrDefaultAsync(a => a.GuestId == guestId);
            if (account == null)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure("Guest account not found.", 404);
            }

            // Da tung dat mat khau roi thi bat buoc xac minh mat khau hien tai - tai khoan chi dang
            // nhap Google (PasswordHash con null) thi cho dat lan dau khong can xac minh, giong het
            // luc "Hoan tat ho so" o GoogleLoginAsync.
            if (account.PasswordHash != null)
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                    !_passwordHasher.VerifyPassword(request.CurrentPassword, account.PasswordHash))
                {
                    return AuthServiceResult<AuthMessageResponse>.Failure("Current password is incorrect.");
                }
            }

            account.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return AuthServiceResult<AuthMessageResponse>.Success(
                new AuthMessageResponse { Message = "Password changed successfully." });
        }

        private static GuestProfileResponse ToProfileResponse(Guest guest, bool hasPassword) => new()
        {
            FullName = guest.FullName,
            Email = guest.Email,
            PhoneNumber = guest.PhoneNumber,
            HasPassword = hasPassword,
        };

        private GuestRefreshToken IssueRefreshToken(int guestAccountId, DateTime expiresAt)
        {
            var token = new GuestRefreshToken
            {
                Token = GenerateSecureToken(),
                GuestAccountId = guestAccountId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };
            _context.GuestRefreshTokens.Add(token);
            return token;
        }

        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private GuestAuthResponse BuildAuthResponse(Guest guest, GuestRefreshToken refreshToken)
        {
            var accessToken = _jwtService.GenerateGuestAccessToken(guest);
            return new GuestAuthResponse
            {
                AccessToken = accessToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refreshToken.Token,
                GuestId = guest.Id,
                FullName = guest.FullName,
                PhoneNumber = guest.PhoneNumber
            };
        }

        private static ReservationResponse ToReservationResponse(Reservation reservation)
        {
            return new ReservationResponse
            {
                Id = reservation.Id,
                BookingCode = reservation.BookingCode,
                GuestId = reservation.GuestId,
                GuestName = reservation.Guest?.FullName ?? string.Empty,
                GuestPhoneNumber = reservation.Guest?.PhoneNumber ?? string.Empty,
                RoomId = reservation.RoomId,
                RoomNumber = reservation.Room?.RoomNumber ?? string.Empty,
                RoomTypeName = reservation.Room?.RoomType?.TypeName ?? string.Empty,
                NumberOfGuests = reservation.NumberOfGuests,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                Status = reservation.Status,
                SpecialRequests = reservation.SpecialRequests,
                DepositAmount = reservation.DepositAmount,
                DepositPaymentMethod = reservation.DepositPaymentMethod?.ToString(),
                DepositPaidAt = reservation.DepositPaidAt
            };
        }
    }
}
