using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Guests;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class GuestService : IGuestService
    {
        private readonly HotelDbContext _context;

        public GuestService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<IReadOnlyList<GuestResponse>>> GetAllAsync(string? keyword = null)
        {
            var query = _context.Guests.Include(g => g.Reservations).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim().ToLower();
                query = query.Where(g => g.FullName.ToLower().Contains(normalizedKeyword)
                    || g.PhoneNumber.Contains(normalizedKeyword)
                    || (g.IdentityNumber != null && g.IdentityNumber.Contains(normalizedKeyword))
                    || (g.Email != null && g.Email.ToLower().Contains(normalizedKeyword)));
            }

            var guests = await query.OrderBy(g => g.FullName).ToListAsync();
            return AuthServiceResult<IReadOnlyList<GuestResponse>>.Success(guests.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<GuestResponse>> GetByIdAsync(int id)
        {
            var guest = await _context.Guests
                .Include(g => g.Reservations)
                .FirstOrDefaultAsync(g => g.Id == id);

            return guest == null
                ? AuthServiceResult<GuestResponse>.Failure("Guest not found.", 404)
                : AuthServiceResult<GuestResponse>.Success(ToResponse(guest));
        }

        public async Task<AuthServiceResult<GuestResponse>> CreateAsync(CreateGuestRequest request)
        {
            var validationMessage = Validate(request.FullName, request.PhoneNumber, request.IdentityNumber);
            if (validationMessage != null)
            {
                return AuthServiceResult<GuestResponse>.Failure(validationMessage);
            }

            var identityNumber = request.IdentityNumber.Trim();
            var identityExists = await _context.Guests.AnyAsync(g => g.IdentityNumber == identityNumber);
            if (identityExists)
            {
                return AuthServiceResult<GuestResponse>.Failure("Identity number already exists.", 409);
            }

            var guest = new Guest
            {
                FullName = request.FullName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                IdentityNumber = identityNumber
            };

            _context.Guests.Add(guest);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Yêu cầu khác vừa tạo cùng CCCD/CMND trong lúc chờ AnyAsync ở trên - unique index DB
                // đã chặn đúng, chỉ cần dịch exception thành 409 sạch thay vì để lộ 500 hoặc tạo trùng.
                var stillDuplicate = await _context.Guests.AnyAsync(g => g.IdentityNumber == identityNumber);
                if (!stillDuplicate)
                {
                    throw;
                }

                return AuthServiceResult<GuestResponse>.Failure("Identity number already exists.", 409);
            }

            return AuthServiceResult<GuestResponse>.Success(ToResponse(guest), "Guest created successfully.");
        }

        public async Task<AuthServiceResult<GuestResponse>> UpdateAsync(int id, UpdateGuestRequest request)
        {
            var validationMessage = Validate(request.FullName, request.PhoneNumber, request.IdentityNumber);
            if (validationMessage != null)
            {
                return AuthServiceResult<GuestResponse>.Failure(validationMessage);
            }

            var guest = await _context.Guests
                .Include(g => g.Reservations)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (guest == null)
            {
                return AuthServiceResult<GuestResponse>.Failure("Guest not found.", 404);
            }

            var identityNumber = request.IdentityNumber.Trim();
            var identityExists = await _context.Guests.AnyAsync(g => g.Id != id && g.IdentityNumber == identityNumber);
            if (identityExists)
            {
                return AuthServiceResult<GuestResponse>.Failure("Identity number already exists.", 409);
            }

            guest.FullName = request.FullName.Trim();
            guest.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            guest.PhoneNumber = request.PhoneNumber.Trim();
            guest.IdentityNumber = identityNumber;
            guest.Tag = request.Tag;
            guest.TagNote = string.IsNullOrWhiteSpace(request.TagNote) ? null : request.TagNote.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var stillDuplicate = await _context.Guests.AnyAsync(g => g.Id != id && g.IdentityNumber == identityNumber);
                if (!stillDuplicate)
                {
                    throw;
                }

                return AuthServiceResult<GuestResponse>.Failure("Identity number already exists.", 409);
            }

            return AuthServiceResult<GuestResponse>.Success(ToResponse(guest), "Guest updated successfully.");
        }

        private static GuestResponse ToResponse(Guest guest)
        {
            return new GuestResponse
            {
                Id = guest.Id,
                FullName = guest.FullName,
                Email = guest.Email,
                PhoneNumber = guest.PhoneNumber,
                IdentityNumber = guest.IdentityNumber ?? string.Empty,
                Tag = guest.Tag,
                TagNote = guest.TagNote,
                ReservationCount = guest.Reservations?.Count ?? 0
            };
        }

        private static string? Validate(string fullName, string phoneNumber, string identityNumber)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "FullName is required.";
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return "PhoneNumber is required.";
            }

            return string.IsNullOrWhiteSpace(identityNumber) ? "IdentityNumber is required." : null;
        }
    }
}
