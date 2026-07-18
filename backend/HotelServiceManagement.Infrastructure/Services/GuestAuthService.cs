using System.Security.Cryptography;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.GuestAuth;
using HotelServiceManagement.Application.DTOs.Reservations;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class GuestAuthService : IGuestAuthService
    {
        // Khong co "remember me" nhu nhan vien - khach hiem khi dang nhap moi ngay, co dinh 1 thoi
        // han du dai la vua, khong can them lua chon lam phuc tap UI dang ky/dang nhap.
        private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);

        private readonly HotelDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;

        public GuestAuthService(HotelDbContext context, IJwtService jwtService, IPasswordHasher passwordHasher)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthServiceResult<GuestAuthResponse>> RegisterAsync(GuestRegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Password must be at least 6 characters.");
            }

            var guest = await FindVerifiedGuestAsync(request.BookingCode, request.FullName, request.PhoneNumber);
            if (guest == null)
            {
                return AuthServiceResult<GuestAuthResponse>.Failure(
                    "Booking code, full name or phone number does not match any reservation.", 404);
            }

            var existingAccount = await _context.GuestAccounts.AnyAsync(a => a.GuestId == guest.Id);
            if (existingAccount)
            {
                return AuthServiceResult<GuestAuthResponse>.Failure(
                    "This guest already has an account. Please login instead.", 409);
            }

            var account = new GuestAccount
            {
                GuestId = guest.Id,
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
                // Request khac vua dang ky xong cho dung Guest nay trong luc cho SaveChangesAsync -
                // unique index GuestId da chan dung, chi can dich thanh 409 sach.
                var stillExists = await _context.GuestAccounts.AnyAsync(a => a.GuestId == guest.Id);
                if (!stillExists)
                {
                    throw;
                }

                return AuthServiceResult<GuestAuthResponse>.Failure(
                    "This guest already has an account. Please login instead.", 409);
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

            if (account == null || !_passwordHasher.VerifyPassword(request.Password, account.PasswordHash))
            {
                return AuthServiceResult<GuestAuthResponse>.Failure("Phone number or password is incorrect.", 401);
            }

            account.LastLoginAt = DateTime.UtcNow;
            var refreshToken = IssueRefreshToken(account.Id, DateTime.UtcNow.Add(RefreshLifetime));
            await _context.SaveChangesAsync();

            return AuthServiceResult<GuestAuthResponse>.Success(BuildAuthResponse(account.Guest, refreshToken));
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

        public async Task<AuthServiceResult<AuthMessageResponse>> ResetPasswordAsync(GuestResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure("New password must be at least 6 characters.");
            }

            var guest = await FindVerifiedGuestAsync(request.BookingCode, request.FullName, request.PhoneNumber);
            if (guest == null)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure(
                    "Booking code, full name or phone number does not match any reservation.", 404);
            }

            var account = await _context.GuestAccounts.FirstOrDefaultAsync(a => a.GuestId == guest.Id);
            if (account == null)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure(
                    "This guest does not have an account yet. Please register instead.", 404);
            }

            account.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);

            // Doi mat khau xong thu hoi toan bo refresh token cu - phong truong hop mat khau bi lo,
            // thiet bi/nguoi khac dang giu refresh token cu se khong the tiep tuc lam moi phien duoc nua.
            var activeTokens = await _context.GuestRefreshTokens
                .Where(rt => rt.GuestAccountId == account.Id && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
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

        // Xac minh khach la chu that cua 1 dat phong: BookingCode phai ton tai, VA ho ten + SDT tren
        // dat phong do phai khop dung Guest tuong ung - dung chung cho ca dang ky lan quen mat khau,
        // khong can ha tang gui email.
        private async Task<Guest?> FindVerifiedGuestAsync(string? bookingCode, string? fullName, string? phoneNumber)
        {
            var normalizedCode = bookingCode?.Trim() ?? string.Empty;
            if (normalizedCode.Length == 0)
            {
                return null;
            }

            var reservation = await _context.Reservations
                .Include(r => r.Guest)
                .FirstOrDefaultAsync(r => r.BookingCode == normalizedCode);

            if (reservation == null)
            {
                return null;
            }

            var nameMatches = string.Equals(
                reservation.Guest.FullName.Trim(), fullName?.Trim() ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
            var phoneMatches = string.Equals(
                reservation.Guest.PhoneNumber.Trim(), phoneNumber?.Trim() ?? string.Empty,
                StringComparison.Ordinal);

            return nameMatches && phoneMatches ? reservation.Guest : null;
        }

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
