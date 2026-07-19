using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Housekeeping;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class HousekeepingRequestService : IHousekeepingRequestService
    {
        private readonly HotelDbContext _context;

        public HousekeepingRequestService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<HousekeepingRequestResponse>> CreateForGuestAsync(int guestId, string? requestType, string? note)
        {
            var parsedType = Enum.TryParse<HousekeepingRequestType>(requestType, ignoreCase: true, out var parsed)
                ? parsed
                : HousekeepingRequestType.Other;

            var stay = await _context.Stays
                .Include(s => s.Reservation).ThenInclude(r => r.Guest)
                .Include(s => s.Reservation).ThenInclude(r => r.Room)
                .Where(s => s.Status == StayStatus.Active && s.Reservation.GuestId == guestId)
                .OrderByDescending(s => s.ActualCheckIn)
                .FirstOrDefaultAsync();

            if (stay == null)
            {
                return AuthServiceResult<HousekeepingRequestResponse>.Failure(
                    "No active stay found for this guest - housekeeping requests are only available while checked in.", 404);
            }

            var request = new HousekeepingRequest
            {
                StayId = stay.Id,
                RequestType = parsedType,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                Status = HousekeepingRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            _context.HousekeepingRequests.Add(request);
            await _context.SaveChangesAsync();

            return AuthServiceResult<HousekeepingRequestResponse>.Success(
                ToResponse(request, stay), "Housekeeping request sent.");
        }

        public async Task<AuthServiceResult<IReadOnlyList<HousekeepingRequestResponse>>> GetActiveAsync()
        {
            var requests = await _context.HousekeepingRequests
                .AsNoTracking()
                .Include(r => r.Stay).ThenInclude(s => s.Reservation).ThenInclude(res => res.Guest)
                .Include(r => r.Stay).ThenInclude(s => s.Reservation).ThenInclude(res => res.Room)
                .Include(r => r.HandledByUser)
                .Where(r => r.Status != HousekeepingRequestStatus.Completed)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<HousekeepingRequestResponse>>.Success(
                requests.Select(r => ToResponse(r, r.Stay)).ToList());
        }

        public async Task<AuthServiceResult<HousekeepingRequestResponse>> AcknowledgeAsync(int id, int staffUserId)
        {
            var request = await LoadForUpdateAsync(id);
            if (request == null)
            {
                return AuthServiceResult<HousekeepingRequestResponse>.Failure("Housekeeping request not found.", 404);
            }

            if (request.Status != HousekeepingRequestStatus.Pending)
            {
                return AuthServiceResult<HousekeepingRequestResponse>.Failure(
                    "Only a pending request can be acknowledged.", 409);
            }

            request.Status = HousekeepingRequestStatus.Acknowledged;
            request.HandledByUserId = staffUserId;
            await _context.SaveChangesAsync();

            return AuthServiceResult<HousekeepingRequestResponse>.Success(ToResponse(request, request.Stay));
        }

        public async Task<AuthServiceResult<HousekeepingRequestResponse>> CompleteAsync(int id, int staffUserId)
        {
            var request = await LoadForUpdateAsync(id);
            if (request == null)
            {
                return AuthServiceResult<HousekeepingRequestResponse>.Failure("Housekeeping request not found.", 404);
            }

            if (request.Status == HousekeepingRequestStatus.Completed)
            {
                return AuthServiceResult<HousekeepingRequestResponse>.Failure("Request is already completed.", 409);
            }

            request.Status = HousekeepingRequestStatus.Completed;
            request.HandledAt = DateTime.UtcNow;
            request.HandledByUserId = staffUserId;
            await _context.SaveChangesAsync();

            return AuthServiceResult<HousekeepingRequestResponse>.Success(ToResponse(request, request.Stay));
        }

        private async Task<HousekeepingRequest?> LoadForUpdateAsync(int id)
        {
            return await _context.HousekeepingRequests
                .Include(r => r.Stay).ThenInclude(s => s.Reservation).ThenInclude(res => res.Guest)
                .Include(r => r.Stay).ThenInclude(s => s.Reservation).ThenInclude(res => res.Room)
                .Include(r => r.HandledByUser)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        private static HousekeepingRequestResponse ToResponse(HousekeepingRequest request, Stay stay)
        {
            return new HousekeepingRequestResponse
            {
                Id = request.Id,
                StayId = request.StayId,
                BookingCode = stay.Reservation?.BookingCode ?? string.Empty,
                RoomNumber = stay.Reservation?.Room?.RoomNumber ?? string.Empty,
                GuestName = stay.Reservation?.Guest?.FullName ?? string.Empty,
                RequestType = request.RequestType,
                Note = request.Note,
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                HandledAt = request.HandledAt,
                HandledByUserName = request.HandledByUser?.FullName
            };
        }
    }
}
