using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.RoomTypes;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class RoomTypeService : IRoomTypeService
    {
        private static readonly ReservationStatus[] CapacityBlockingStatuses =
        {
            ReservationStatus.Pending,
            ReservationStatus.Confirmed,
            ReservationStatus.CheckedIn
        };

        private readonly HotelDbContext _context;

        public RoomTypeService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<IReadOnlyList<RoomTypeResponse>>> GetAllAsync()
        {
            var roomTypes = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .OrderBy(rt => rt.Id)
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<RoomTypeResponse>>.Success(
                roomTypes.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<RoomTypeResponse>> GetByIdAsync(int id)
        {
            if (id <= 0)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(
                    "Room type id must be greater than 0.");
            }

            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);

            return roomType == null
                ? AuthServiceResult<RoomTypeResponse>.Failure("Room type not found.", 404)
                : AuthServiceResult<RoomTypeResponse>.Success(ToResponse(roomType));
        }

        public async Task<AuthServiceResult<RoomTypeResponse>> CreateAsync(
            CreateRoomTypeRequest request)
        {
            if (request == null)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(
                    "Request body is required.");
            }

            var validationMessage = Validate(
                request.TypeName,
                request.Capacity,
                request.BasePrice);

            if (validationMessage != null)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(validationMessage);
            }

            var normalizedName = request.TypeName.Trim().ToLower();
            var nameExists = await _context.RoomTypes.AnyAsync(rt =>
                rt.TypeName.ToLower() == normalizedName);

            if (nameExists)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(
                    "Room type name already exists.", 409);
            }

            var roomType = new RoomType
            {
                TypeName = request.TypeName.Trim(),
                Capacity = request.Capacity,
                BasePrice = request.BasePrice,
                Description = NormalizeDescription(request.Description),
                IsActive = request.IsActive
            };

            _context.RoomTypes.Add(roomType);
            await _context.SaveChangesAsync();

            return AuthServiceResult<RoomTypeResponse>.Success(
                ToResponse(roomType),
                "Room type created successfully.");
        }

        /// <summary>
        /// Prevents reducing Capacity below NumberOfGuests of any active reservation
        /// belonging to rooms of this type.
        /// </summary>
        public async Task<AuthServiceResult<RoomTypeResponse>> UpdateAsync(
            int id,
            UpdateRoomTypeRequest request)
        {
            if (id <= 0)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(
                    "Room type id must be greater than 0.");
            }

            if (request == null)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(
                    "Request body is required.");
            }

            var validationMessage = Validate(
                request.TypeName,
                request.Capacity,
                request.BasePrice);

            if (validationMessage != null)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(validationMessage);
            }

            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);

            if (roomType == null)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(
                    "Room type not found.", 404);
            }

            var normalizedName = request.TypeName.Trim().ToLower();
            var nameExists = await _context.RoomTypes.AnyAsync(rt =>
                rt.Id != id
                && rt.TypeName.ToLower() == normalizedName);

            if (nameExists)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(
                    "Room type name already exists.", 409);
            }

            if (request.Capacity < roomType.Capacity)
            {
                var hasReservationExceedingNewCapacity =
                    await _context.Reservations.AnyAsync(r =>
                        r.Room.RoomTypeId == id
                        && CapacityBlockingStatuses.Contains(r.Status)
                        && r.NumberOfGuests > request.Capacity);

                if (hasReservationExceedingNewCapacity)
                {
                    return AuthServiceResult<RoomTypeResponse>.Failure(
                        "Cannot reduce room type capacity because an active reservation exceeds the new capacity.",
                        409);
                }
            }

            roomType.TypeName = request.TypeName.Trim();
            roomType.Capacity = request.Capacity;
            roomType.BasePrice = request.BasePrice;
            roomType.Description = NormalizeDescription(request.Description);
            roomType.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return AuthServiceResult<RoomTypeResponse>.Success(
                ToResponse(roomType),
                "Room type updated successfully.");
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> DeleteAsync(int id)
        {
            if (id <= 0)
            {
                return MessageFailure("Room type id must be greater than 0.");
            }

            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);

            if (roomType == null)
            {
                return MessageFailure("Room type not found.", 404);
            }

            // Keep history and foreign keys intact: types already used by rooms are soft-deleted.
            if (roomType.Rooms.Any())
            {
                roomType.IsActive = false;
                await _context.SaveChangesAsync();

                return MessageSuccess(
                    "Room type is being used, so it was deactivated instead of deleted.");
            }

            _context.RoomTypes.Remove(roomType);
            await _context.SaveChangesAsync();

            return MessageSuccess("Room type deleted successfully.");
        }

        private static RoomTypeResponse ToResponse(RoomType roomType)
        {
            return new RoomTypeResponse
            {
                Id = roomType.Id,
                TypeName = roomType.TypeName,
                Capacity = roomType.Capacity,
                BasePrice = roomType.BasePrice,
                Description = roomType.Description,
                IsActive = roomType.IsActive,
                RoomCount = roomType.Rooms?.Count ?? 0
            };
        }

        private static string? Validate(
            string typeName,
            int capacity,
            decimal basePrice)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return "TypeName is required.";
            }

            if (typeName.Trim().Length > 50)
            {
                return "TypeName cannot exceed 50 characters.";
            }

            if (capacity < 1)
            {
                return "Capacity must be at least 1.";
            }

            return basePrice < 0
                ? "BasePrice must be greater than or equal to 0."
                : null;
        }

        private static string? NormalizeDescription(string? description)
        {
            return string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();
        }

        private static AuthServiceResult<AuthMessageResponse> MessageSuccess(
            string message)
        {
            return AuthServiceResult<AuthMessageResponse>.Success(
                new AuthMessageResponse { Message = message },
                message);
        }

        private static AuthServiceResult<AuthMessageResponse> MessageFailure(
            string message,
            int statusCode = 400)
        {
            return AuthServiceResult<AuthMessageResponse>.Failure(
                message,
                statusCode);
        }
    }
}