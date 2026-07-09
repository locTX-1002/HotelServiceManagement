using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Rooms;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class RoomService : IRoomService
    {
        private readonly HotelDbContext _context;

        public RoomService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<IReadOnlyList<RoomResponse>>> GetAllAsync()
        {
            var rooms = await QueryRooms()
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<RoomResponse>>.Success(rooms.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<IReadOnlyList<RoomMapFloorResponse>>> GetMapAsync()
        {
            var rooms = await QueryRooms()
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();

            var map = rooms
                .GroupBy(r => r.Floor)
                .OrderBy(g => g.Key)
                .Select(g => new RoomMapFloorResponse
                {
                    Floor = g.Key,
                    Rooms = g.Select(ToResponse).ToList()
                })
                .ToList();

            return AuthServiceResult<IReadOnlyList<RoomMapFloorResponse>>.Success(map);
        }

        public async Task<AuthServiceResult<RoomResponse>> GetByIdAsync(int id)
        {
            var room = await QueryRooms().FirstOrDefaultAsync(r => r.Id == id);
            return room == null
                ? AuthServiceResult<RoomResponse>.Failure("Room not found.", 404)
                : AuthServiceResult<RoomResponse>.Success(ToResponse(room));
        }

        public async Task<AuthServiceResult<RoomResponse>> CreateAsync(CreateRoomRequest request)
        {
            var validationMessage = Validate(request.RoomNumber, request.Floor, request.RoomTypeId);
            if (validationMessage != null)
            {
                return AuthServiceResult<RoomResponse>.Failure(validationMessage);
            }

            var roomNumber = request.RoomNumber.Trim();
            var roomNumberExists = await _context.Rooms.AnyAsync(r => r.RoomNumber.ToLower() == roomNumber.ToLower());
            if (roomNumberExists)
            {
                return AuthServiceResult<RoomResponse>.Failure("Room number already exists.", 409);
            }

            var roomType = await _context.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == request.RoomTypeId && rt.IsActive);
            if (roomType == null)
            {
                return AuthServiceResult<RoomResponse>.Failure("Active room type does not exist.");
            }

            var room = new Room
            {
                RoomNumber = roomNumber,
                Floor = request.Floor,
                RoomTypeId = request.RoomTypeId,
                Status = request.Status,
                IsActive = request.IsActive
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            room.RoomType = roomType;

            return AuthServiceResult<RoomResponse>.Success(ToResponse(room), "Room created successfully.");
        }

        public async Task<AuthServiceResult<RoomResponse>> UpdateAsync(int id, UpdateRoomRequest request)
        {
            var validationMessage = Validate(request.RoomNumber, request.Floor, request.RoomTypeId);
            if (validationMessage != null)
            {
                return AuthServiceResult<RoomResponse>.Failure(validationMessage);
            }

            var room = await QueryRooms().FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
            {
                return AuthServiceResult<RoomResponse>.Failure("Room not found.", 404);
            }

            var roomNumber = request.RoomNumber.Trim();
            var roomNumberExists = await _context.Rooms.AnyAsync(r => r.Id != id && r.RoomNumber.ToLower() == roomNumber.ToLower());
            if (roomNumberExists)
            {
                return AuthServiceResult<RoomResponse>.Failure("Room number already exists.", 409);
            }

            var roomType = await _context.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == request.RoomTypeId && rt.IsActive);
            if (roomType == null)
            {
                return AuthServiceResult<RoomResponse>.Failure("Active room type does not exist.");
            }

            room.RoomNumber = roomNumber;
            room.Floor = request.Floor;
            room.RoomTypeId = request.RoomTypeId;
            room.RoomType = roomType;
            room.Status = request.Status;
            room.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return AuthServiceResult<RoomResponse>.Success(ToResponse(room), "Room updated successfully.");
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> DeleteAsync(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Reservations)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
            {
                return MessageFailure("Room not found.", 404);
            }

            if (room.Reservations.Any())
            {
                room.IsActive = false;
                await _context.SaveChangesAsync();
                return MessageSuccess("Room is being used, so it was deactivated instead of deleted.");
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return MessageSuccess("Room deleted successfully.");
        }

        private IQueryable<Room> QueryRooms()
        {
            return _context.Rooms.Include(r => r.RoomType);
        }

        private static RoomResponse ToResponse(Room room)
        {
            return new RoomResponse
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                Floor = room.Floor,
                RoomTypeId = room.RoomTypeId,
                RoomTypeName = room.RoomType?.TypeName ?? string.Empty,
                Capacity = room.RoomType?.Capacity ?? 0,
                BasePrice = room.RoomType?.BasePrice ?? 0,
                Status = room.Status,
                IsActive = room.IsActive
            };
        }

        private static string? Validate(string roomNumber, int floor, int roomTypeId)
        {
            if (string.IsNullOrWhiteSpace(roomNumber))
            {
                return "RoomNumber is required.";
            }

            if (floor <= 0)
            {
                return "Floor must be greater than 0.";
            }

            return roomTypeId <= 0 ? "RoomTypeId is required." : null;
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
