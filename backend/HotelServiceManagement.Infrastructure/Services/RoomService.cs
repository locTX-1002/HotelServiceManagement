using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Rooms;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class RoomService : IRoomService
    {
        private static readonly ReservationStatus[] CapacityBlockingStatuses =
        {
            ReservationStatus.Pending,
            ReservationStatus.Confirmed,
            ReservationStatus.CheckedIn
        };

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

            return AuthServiceResult<IReadOnlyList<RoomResponse>>.Success(
                rooms.Select(ToResponse).ToList());
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
            if (id <= 0)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Room id must be greater than 0.");
            }

            var room = await QueryRooms().FirstOrDefaultAsync(r => r.Id == id);

            return room == null
                ? AuthServiceResult<RoomResponse>.Failure("Room not found.", 404)
                : AuthServiceResult<RoomResponse>.Success(ToResponse(room));
        }

        /// <summary>
        /// New rooms cannot start as Reserved/Occupied because those statuses must be
        /// produced by Reservation/Stay flows.
        /// </summary>
        public async Task<AuthServiceResult<RoomResponse>> CreateAsync(
            CreateRoomRequest request)
        {
            if (request == null)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Request body is required.");
            }

            var validationMessage = Validate(
                request.RoomNumber,
                request.Floor,
                request.RoomTypeId,
                request.Status,
                isCreating: true);

            if (validationMessage != null)
            {
                return AuthServiceResult<RoomResponse>.Failure(validationMessage);
            }

            var roomNumber = request.RoomNumber.Trim();
            var roomNumberExists = await _context.Rooms.AnyAsync(r =>
                r.RoomNumber.ToLower() == roomNumber.ToLower());

            if (roomNumberExists)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Room number already exists.", 409);
            }

            var roomType = await _context.RoomTypes.FirstOrDefaultAsync(rt =>
                rt.Id == request.RoomTypeId
                && rt.IsActive);

            if (roomType == null)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Active room type does not exist.", 404);
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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Yêu cầu khác vừa tạo cùng số phòng trong lúc chờ AnyAsync ở trên - unique index DB
                // đã chặn đúng, chỉ cần dịch exception thành 409 sạch thay vì để lộ 500.
                var stillDuplicate = await _context.Rooms.AnyAsync(r =>
                    r.RoomNumber.ToLower() == roomNumber.ToLower());
                if (!stillDuplicate)
                {
                    throw;
                }

                return AuthServiceResult<RoomResponse>.Failure(
                    "Room number already exists.", 409);
            }

            room.RoomType = roomType;

            return AuthServiceResult<RoomResponse>.Success(
                ToResponse(room),
                "Room created successfully.");
        }

        /// <summary>
        /// Protects active reservations/stays when changing room type, status or IsActive.
        /// Reserved and Occupied remain system-driven statuses.
        /// </summary>
        public async Task<AuthServiceResult<RoomResponse>> UpdateAsync(
            int id,
            UpdateRoomRequest request)
        {
            if (id <= 0)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Room id must be greater than 0.");
            }

            if (request == null)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Request body is required.");
            }

            var validationMessage = Validate(
                request.RoomNumber,
                request.Floor,
                request.RoomTypeId,
                request.Status,
                isCreating: false);

            if (validationMessage != null)
            {
                return AuthServiceResult<RoomResponse>.Failure(validationMessage);
            }

            var room = await QueryRooms().FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Room not found.", 404);
            }

            var roomNumber = request.RoomNumber.Trim();
            var roomNumberExists = await _context.Rooms.AnyAsync(r =>
                r.Id != id
                && r.RoomNumber.ToLower() == roomNumber.ToLower());

            if (roomNumberExists)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Room number already exists.", 409);
            }

            var roomType = await _context.RoomTypes.FirstOrDefaultAsync(rt =>
                rt.Id == request.RoomTypeId
                && rt.IsActive);

            if (roomType == null)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Active room type does not exist.", 404);
            }

            var hasActiveStay = await _context.Stays.AnyAsync(s =>
                s.Reservation.RoomId == id
                && s.Status == StayStatus.Active);

            var hasCheckedInReservation = await _context.Reservations.AnyAsync(r =>
                r.RoomId == id
                && r.Status == ReservationStatus.CheckedIn);

            var hasPendingOrConfirmedReservation =
                await _context.Reservations.AnyAsync(r =>
                    r.RoomId == id
                    && (r.Status == ReservationStatus.Pending
                        || r.Status == ReservationStatus.Confirmed));

            var hasOccupiedBusiness = hasActiveStay || hasCheckedInReservation;
            var hasBlockingBusiness = hasOccupiedBusiness
                || hasPendingOrConfirmedReservation;

            if (!request.IsActive && hasBlockingBusiness)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Room with an active reservation or stay cannot be deactivated.",
                    409);
            }

            if (hasOccupiedBusiness && request.Status != RoomStatus.Occupied)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Room with an active stay must remain Occupied.", 409);
            }

            if (!hasOccupiedBusiness
                && hasPendingOrConfirmedReservation
                && request.Status != RoomStatus.Reserved)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Room with a Pending or Confirmed reservation must remain Reserved.",
                    409);
            }

            if (!hasBlockingBusiness
                && (request.Status == RoomStatus.Reserved
                    || request.Status == RoomStatus.Occupied))
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    "Reserved and Occupied statuses can only be set by Reservation/Stay flows.",
                    409);
            }

            if (request.RoomTypeId != room.RoomTypeId)
            {
                var hasReservationExceedingNewCapacity =
                    await _context.Reservations.AnyAsync(r =>
                        r.RoomId == id
                        && CapacityBlockingStatuses.Contains(r.Status)
                        && r.NumberOfGuests > roomType.Capacity);

                if (hasReservationExceedingNewCapacity)
                {
                    return AuthServiceResult<RoomResponse>.Failure(
                        "Cannot change the room type because an active reservation exceeds the new capacity.",
                        409);
                }
            }

            room.RoomNumber = roomNumber;
            room.Floor = request.Floor;
            room.RoomTypeId = request.RoomTypeId;
            room.RoomType = roomType;
            room.Status = request.Status;
            room.IsActive = request.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                var stillDuplicate = await _context.Rooms.AnyAsync(r =>
                    r.Id != id && r.RoomNumber.ToLower() == roomNumber.ToLower());
                if (!stillDuplicate)
                {
                    throw;
                }

                return AuthServiceResult<RoomResponse>.Failure(
                    "Room number already exists.", 409);
            }

            return AuthServiceResult<RoomResponse>.Success(
                ToResponse(room),
                "Room updated successfully.");
        }

        public async Task<AuthServiceResult<RoomResponse>> UpdateStatusAsync(
            int id, RoomStatus status, bool canManageMaintenance)
        {
            if (id <= 0)
            {
                return AuthServiceResult<RoomResponse>.Failure("Room id must be greater than 0.");
            }

            if (!Enum.IsDefined(typeof(RoomStatus), status))
            {
                return AuthServiceResult<RoomResponse>.Failure("Room status is invalid.");
            }

            var room = await QueryRooms().FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
            {
                return AuthServiceResult<RoomResponse>.Failure("Room not found.", 404);
            }

            if (room.Status == status)
            {
                return AuthServiceResult<RoomResponse>.Success(ToResponse(room), "Room status is unchanged.");
            }

            var allowed = (room.Status, status) switch
            {
                (RoomStatus.Cleaning, RoomStatus.Available) => true,
                (RoomStatus.Available, RoomStatus.Cleaning) => true,
                (RoomStatus.Available, RoomStatus.Maintenance) => canManageMaintenance,
                (RoomStatus.Maintenance, RoomStatus.Available) => canManageMaintenance,
                _ => false
            };

            if (!allowed)
            {
                return AuthServiceResult<RoomResponse>.Failure(
                    $"Invalid room status transition from {room.Status} to {status}.", 409);
            }

            room.Status = status;
            await _context.SaveChangesAsync();
            return AuthServiceResult<RoomResponse>.Success(ToResponse(room), "Room status updated successfully.");
        }

        /// <summary>
        /// Rooms with active business data cannot be removed. Rooms with historical
        /// reservations are deactivated; only never-used rooms are physically deleted.
        /// </summary>
        public async Task<AuthServiceResult<AuthMessageResponse>> DeleteAsync(int id)
        {
            if (id <= 0)
            {
                return MessageFailure("Room id must be greater than 0.");
            }

            var room = await _context.Rooms
                .Include(r => r.Reservations)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return MessageFailure("Room not found.", 404);
            }

            var hasActiveStay = await _context.Stays.AnyAsync(s =>
                s.Reservation.RoomId == id
                && s.Status == StayStatus.Active);

            var hasBlockingReservation = room.Reservations.Any(r =>
                CapacityBlockingStatuses.Contains(r.Status));

            if (hasActiveStay || hasBlockingReservation)
            {
                return MessageFailure(
                    "Room with an active reservation or stay cannot be deleted or deactivated.",
                    409);
            }

            if (room.Reservations.Any())
            {
                room.IsActive = false;
                await _context.SaveChangesAsync();

                return MessageSuccess(
                    "Room has historical reservations, so it was deactivated instead of deleted.");
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

        private static string? Validate(
            string roomNumber,
            int floor,
            int roomTypeId,
            RoomStatus status,
            bool isCreating)
        {
            if (string.IsNullOrWhiteSpace(roomNumber))
            {
                return "RoomNumber is required.";
            }

            if (roomNumber.Trim().Length > 20)
            {
                return "RoomNumber cannot exceed 20 characters.";
            }

            if (floor <= 0)
            {
                return "Floor must be greater than 0.";
            }

            if (roomTypeId <= 0)
            {
                return "RoomTypeId must be greater than 0.";
            }

            if (!Enum.IsDefined(typeof(RoomStatus), status))
            {
                return "Room status is invalid.";
            }

            if (isCreating
                && (status == RoomStatus.Reserved
                    || status == RoomStatus.Occupied))
            {
                return "A new room cannot start as Reserved or Occupied.";
            }

            return null;
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
