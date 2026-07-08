using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.RoomTypes;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class RoomTypeService : IRoomTypeService
    {
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

            return AuthServiceResult<IReadOnlyList<RoomTypeResponse>>.Success(roomTypes.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<RoomTypeResponse>> GetByIdAsync(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);

            return roomType == null
                ? AuthServiceResult<RoomTypeResponse>.Failure("Room type not found.", 404)
                : AuthServiceResult<RoomTypeResponse>.Success(ToResponse(roomType));
        }

        public async Task<AuthServiceResult<RoomTypeResponse>> CreateAsync(CreateRoomTypeRequest request)
        {
            var validationMessage = Validate(request.TypeName, request.Capacity, request.BasePrice);
            if (validationMessage != null)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(validationMessage);
            }

            var normalizedName = request.TypeName.Trim().ToLower();
            var nameExists = await _context.RoomTypes.AnyAsync(rt => rt.TypeName.ToLower() == normalizedName);
            if (nameExists)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure("Room type name already exists.", 409);
            }

            var roomType = new RoomType
            {
                TypeName = request.TypeName.Trim(),
                Capacity = request.Capacity,
                BasePrice = request.BasePrice,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IsActive = request.IsActive
            };

            _context.RoomTypes.Add(roomType);
            await _context.SaveChangesAsync();

            return AuthServiceResult<RoomTypeResponse>.Success(ToResponse(roomType), "Room type created successfully.");
        }

        public async Task<AuthServiceResult<RoomTypeResponse>> UpdateAsync(int id, UpdateRoomTypeRequest request)
        {
            var validationMessage = Validate(request.TypeName, request.Capacity, request.BasePrice);
            if (validationMessage != null)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure(validationMessage);
            }

            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);
            if (roomType == null)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure("Room type not found.", 404);
            }

            var normalizedName = request.TypeName.Trim().ToLower();
            var nameExists = await _context.RoomTypes.AnyAsync(rt => rt.Id != id && rt.TypeName.ToLower() == normalizedName);
            if (nameExists)
            {
                return AuthServiceResult<RoomTypeResponse>.Failure("Room type name already exists.", 409);
            }

            roomType.TypeName = request.TypeName.Trim();
            roomType.Capacity = request.Capacity;
            roomType.BasePrice = request.BasePrice;
            roomType.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            roomType.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return AuthServiceResult<RoomTypeResponse>.Success(ToResponse(roomType), "Room type updated successfully.");
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> DeleteAsync(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);
            if (roomType == null)
            {
                return MessageFailure("Room type not found.", 404);
            }

            if (roomType.Rooms.Any())
            {
                roomType.IsActive = false;
                await _context.SaveChangesAsync();
                return MessageSuccess("Room type is being used, so it was deactivated instead of deleted.");
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

        private static string? Validate(string typeName, int capacity, decimal basePrice)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return "TypeName is required.";
            }

            if (capacity <= 0)
            {
                return "Capacity must be greater than 0.";
            }

            return basePrice < 0 ? "BasePrice must be greater than or equal to 0." : null;
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
