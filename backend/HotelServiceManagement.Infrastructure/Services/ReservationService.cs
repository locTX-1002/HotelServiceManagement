using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Reservations;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class ReservationService : IReservationService
    {
        private static readonly ReservationStatus[] BlockingStatuses =
        {
            ReservationStatus.Pending,
            ReservationStatus.Confirmed,
            ReservationStatus.CheckedIn
        };

        private readonly HotelDbContext _context;

        public ReservationService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<IReadOnlyList<ReservationResponse>>> GetAllAsync()
        {
            var reservations = await QueryReservations()
                .OrderByDescending(r => r.CheckInDate)
                .ThenBy(r => r.Id)
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<ReservationResponse>>.Success(
                reservations.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<ReservationResponse>> GetByIdAsync(int id)
        {
            if (id <= 0)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Reservation id must be greater than 0.");
            }

            var reservation = await QueryReservations()
                .FirstOrDefaultAsync(r => r.Id == id);

            return reservation == null
                ? AuthServiceResult<ReservationResponse>.Failure("Reservation not found.", 404)
                : AuthServiceResult<ReservationResponse>.Success(ToResponse(reservation));
        }

        /// <summary>
        /// Creates a reservation after validating the date range, guest, room state,
        /// NumberOfGuests, status and overlapping booking rule again on the backend.
        /// </summary>
        public async Task<AuthServiceResult<ReservationResponse>> CreateAsync(
            CreateReservationRequest request,
            int createdByUserId)
        {
            if (request == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Request body is required.");
            }

            if (createdByUserId <= 0)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Authenticated user id is invalid.", 401);
            }

            var basicValidationMessage = ValidateReservationInput(
                request.GuestId,
                request.RoomId,
                request.CheckInDate,
                request.CheckOutDate,
                request.Status);

            if (basicValidationMessage != null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    basicValidationMessage);
            }

            var guest = await _context.Guests
                .FirstOrDefaultAsync(g => g.Id == request.GuestId);

            if (guest == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Guest does not exist.", 404);
            }

            var room = await LoadReservableRoomAsync(request.RoomId);
            if (room == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Active room with an active room type does not exist.", 404);
            }

            if (room.Status == RoomStatus.Maintenance)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Room is under maintenance and cannot be reserved.", 409);
            }

            var guestCountValidationMessage = ValidateNumberOfGuests(
                request.NumberOfGuests,
                room.RoomType.Capacity);

            if (guestCountValidationMessage != null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    guestCountValidationMessage);
            }

            if (await HasOverlappingReservationAsync(
                    request.RoomId,
                    request.CheckInDate,
                    request.CheckOutDate,
                    excludeReservationId: null))
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Room already has an active reservation in this date range.", 409);
            }

            var reservation = new Reservation
            {
                BookingCode = await GenerateBookingCodeAsync(),
                GuestId = request.GuestId,
                RoomId = request.RoomId,
                NumberOfGuests = request.NumberOfGuests,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Status = request.Status,
                CreatedByUserId = createdByUserId
            };

            _context.Reservations.Add(reservation);
            UpdateRoomStatusFromReservation(room, reservation.Status);
            await _context.SaveChangesAsync();

            reservation.Guest = guest;
            reservation.Room = room;

            return AuthServiceResult<ReservationResponse>.Success(
                ToResponse(reservation),
                "Reservation created successfully.");
        }

        /// <summary>
        /// Updates only Pending/Confirmed reservations. The guest count is checked against
        /// the newly selected room so changing room cannot bypass the capacity rule.
        /// </summary>
        public async Task<AuthServiceResult<ReservationResponse>> UpdateAsync(
            int id,
            UpdateReservationRequest request)
        {
            if (id <= 0)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Reservation id must be greater than 0.");
            }

            if (request == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Request body is required.");
            }

            var basicValidationMessage = ValidateReservationInput(
                request.GuestId,
                request.RoomId,
                request.CheckInDate,
                request.CheckOutDate,
                request.Status);

            if (basicValidationMessage != null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    basicValidationMessage);
            }

            var reservation = await QueryReservations()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Reservation not found.", 404);
            }

            if (!IsReservationEditable(reservation.Status))
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Cancelled, checked-in or completed reservation cannot be updated.",
                    409);
            }

            // Defensive check in case reservation status and Stay data become inconsistent.
            var alreadyHasStay = await _context.Stays
                .AnyAsync(s => s.ReservationId == id);

            if (alreadyHasStay)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Reservation that already has a stay cannot be updated.", 409);
            }

            var guest = await _context.Guests
                .FirstOrDefaultAsync(g => g.Id == request.GuestId);

            if (guest == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Guest does not exist.", 404);
            }

            var room = await LoadReservableRoomAsync(request.RoomId);
            if (room == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Active room with an active room type does not exist.", 404);
            }

            if (room.Status == RoomStatus.Maintenance)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Room is under maintenance and cannot be reserved.", 409);
            }

            var guestCountValidationMessage = ValidateNumberOfGuests(
                request.NumberOfGuests,
                room.RoomType.Capacity);

            if (guestCountValidationMessage != null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    guestCountValidationMessage);
            }

            if (await HasOverlappingReservationAsync(
                    request.RoomId,
                    request.CheckInDate,
                    request.CheckOutDate,
                    excludeReservationId: id))
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Room already has an active reservation in this date range.", 409);
            }

            var oldRoomId = reservation.RoomId;

            reservation.GuestId = request.GuestId;
            reservation.RoomId = request.RoomId;
            reservation.NumberOfGuests = request.NumberOfGuests;
            reservation.CheckInDate = request.CheckInDate;
            reservation.CheckOutDate = request.CheckOutDate;
            reservation.Status = request.Status;

            // Save reservation first so RefreshRoomStatusAsync reads the new persisted state.
            await _context.SaveChangesAsync();

            await RefreshRoomStatusAsync(oldRoomId);
            if (request.RoomId != oldRoomId)
            {
                await RefreshRoomStatusAsync(request.RoomId);
            }

            await _context.SaveChangesAsync();

            var updatedReservation = await QueryReservations()
                .FirstAsync(r => r.Id == id);

            return AuthServiceResult<ReservationResponse>.Success(
                ToResponse(updatedReservation),
                "Reservation updated successfully.");
        }

        /// <summary>
        /// Cancels only Pending/Confirmed reservations. Checked-in/completed reservations
        /// must continue through Stay/Check-out flow instead of being cancelled directly.
        /// </summary>
        public async Task<AuthServiceResult<AuthMessageResponse>> CancelAsync(int id)
        {
            if (id <= 0)
            {
                return MessageFailure("Reservation id must be greater than 0.");
            }

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return MessageFailure("Reservation not found.", 404);
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return MessageFailure("Reservation is already cancelled.", 409);
            }

            if (!IsReservationEditable(reservation.Status))
            {
                return MessageFailure(
                    "Checked-in or completed reservation cannot be cancelled.", 409);
            }

            var alreadyHasStay = await _context.Stays
                .AnyAsync(s => s.ReservationId == id);

            if (alreadyHasStay)
            {
                return MessageFailure(
                    "Reservation that already has a stay cannot be cancelled.", 409);
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            await RefreshRoomStatusAsync(reservation.RoomId);
            await _context.SaveChangesAsync();

            return MessageSuccess("Reservation cancelled successfully.");
        }

        /// <summary>
        /// Returns rooms that satisfy active-state, maintenance, date overlap and capacity rules.
        /// Invalid optional filters are rejected instead of being silently ignored.
        /// </summary>
        public async Task<AuthServiceResult<IReadOnlyList<AvailableRoomResponse>>> GetAvailableRoomsAsync(
            DateTime checkInDate,
            DateTime checkOutDate,
            int? roomTypeId,
            int? capacity)
        {
            var dateValidationMessage = ValidateDates(checkInDate, checkOutDate);
            if (dateValidationMessage != null)
            {
                return AuthServiceResult<IReadOnlyList<AvailableRoomResponse>>.Failure(
                    dateValidationMessage);
            }

            if (roomTypeId.HasValue && roomTypeId.Value < 1)
            {
                return AuthServiceResult<IReadOnlyList<AvailableRoomResponse>>.Failure(
                    "RoomTypeId must be greater than 0 when provided.");
            }

            if (capacity.HasValue && capacity.Value < 1)
            {
                return AuthServiceResult<IReadOnlyList<AvailableRoomResponse>>.Failure(
                    "Capacity must be at least 1 when provided.");
            }

            var query = _context.Rooms
                .AsNoTracking()
                .Where(r =>
                    r.IsActive
                    && r.RoomType.IsActive
                    && r.Status != RoomStatus.Maintenance);

            if (roomTypeId.HasValue)
            {
                query = query.Where(r => r.RoomTypeId == roomTypeId.Value);
            }

            if (capacity.HasValue)
            {
                query = query.Where(r => r.RoomType.Capacity >= capacity.Value);
            }

            var availableRooms = await query
                .Where(room => !_context.Reservations.Any(reservation =>
                    reservation.RoomId == room.Id
                    && BlockingStatuses.Contains(reservation.Status)
                    && reservation.CheckInDate < checkOutDate
                    && reservation.CheckOutDate > checkInDate))
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .Select(r => new AvailableRoomResponse
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    Floor = r.Floor,
                    RoomTypeId = r.RoomTypeId,
                    RoomTypeName = r.RoomType.TypeName,
                    Capacity = r.RoomType.Capacity,
                    BasePrice = r.RoomType.BasePrice
                })
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<AvailableRoomResponse>>.Success(
                availableRooms);
        }

        private IQueryable<Reservation> QueryReservations()
        {
            return _context.Reservations
                .Include(r => r.Guest)
                .Include(r => r.Room)
                    .ThenInclude(r => r.RoomType);
        }

        private Task<Room?> LoadReservableRoomAsync(int roomId)
        {
            return _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r =>
                    r.Id == roomId
                    && r.IsActive
                    && r.RoomType.IsActive);
        }

        private Task<bool> HasOverlappingReservationAsync(
            int roomId,
            DateTime checkInDate,
            DateTime checkOutDate,
            int? excludeReservationId)
        {
            return _context.Reservations.AnyAsync(reservation =>
                reservation.RoomId == roomId
                && (!excludeReservationId.HasValue
                    || reservation.Id != excludeReservationId.Value)
                && BlockingStatuses.Contains(reservation.Status)
                && reservation.CheckInDate < checkOutDate
                && reservation.CheckOutDate > checkInDate);
        }

        /// <summary>
        /// Recalculates business-driven room status without overwriting Maintenance.
        /// Occupied has priority over Reserved; otherwise room becomes Available.
        /// </summary>
        private async Task RefreshRoomStatusAsync(int roomId)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.Status == RoomStatus.Maintenance)
            {
                return;
            }

            var hasActiveStay = await _context.Stays.AnyAsync(s =>
                s.Reservation.RoomId == roomId
                && s.Status == StayStatus.Active);

            var hasCheckedInReservation = await _context.Reservations.AnyAsync(r =>
                r.RoomId == roomId
                && r.Status == ReservationStatus.CheckedIn);

            if (hasActiveStay || hasCheckedInReservation)
            {
                room.Status = RoomStatus.Occupied;
                return;
            }

            var hasPendingOrConfirmedReservation = await _context.Reservations.AnyAsync(r =>
                r.RoomId == roomId
                && (r.Status == ReservationStatus.Pending
                    || r.Status == ReservationStatus.Confirmed));

            room.Status = hasPendingOrConfirmedReservation
                ? RoomStatus.Reserved
                : RoomStatus.Available;
        }

        private static void UpdateRoomStatusFromReservation(
            Room room,
            ReservationStatus status)
        {
            if (status == ReservationStatus.Pending
                || status == ReservationStatus.Confirmed)
            {
                room.Status = RoomStatus.Reserved;
            }
        }

        private static ReservationResponse ToResponse(Reservation reservation)
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
                Status = reservation.Status
            };
        }

        private static string? ValidateReservationInput(
            int guestId,
            int roomId,
            DateTime checkInDate,
            DateTime checkOutDate,
            ReservationStatus status)
        {
            if (guestId <= 0)
            {
                return "GuestId must be greater than 0.";
            }

            if (roomId <= 0)
            {
                return "RoomId must be greater than 0.";
            }

            var dateValidationMessage = ValidateDates(checkInDate, checkOutDate);
            if (dateValidationMessage != null)
            {
                return dateValidationMessage;
            }

            if (!Enum.IsDefined(typeof(ReservationStatus), status))
            {
                return "Reservation status is invalid.";
            }

            return IsAllowedEditableStatus(status)
                ? null
                : "Reservation can only be created or updated with Pending or Confirmed status.";
        }

        private static bool IsAllowedEditableStatus(ReservationStatus status)
        {
            return status == ReservationStatus.Pending
                || status == ReservationStatus.Confirmed;
        }

        private static bool IsReservationEditable(ReservationStatus status)
        {
            return status == ReservationStatus.Pending
                || status == ReservationStatus.Confirmed;
        }

        private static string? ValidateDates(
            DateTime checkInDate,
            DateTime checkOutDate)
        {
            if (checkInDate == default || checkOutDate == default)
            {
                return "CheckInDate and CheckOutDate are required.";
            }

            return checkOutDate <= checkInDate
                ? "CheckOutDate must be later than CheckInDate."
                : null;
        }

        private static string? ValidateNumberOfGuests(
            int numberOfGuests,
            int roomCapacity)
        {
            if (numberOfGuests < 1)
            {
                return "NumberOfGuests must be at least 1.";
            }

            return numberOfGuests > roomCapacity
                ? $"NumberOfGuests cannot exceed room capacity ({roomCapacity})."
                : null;
        }

        private async Task<string> GenerateBookingCodeAsync()
        {
            string bookingCode;

            do
            {
                bookingCode = $"RES-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            }
            while (await _context.Reservations.AnyAsync(r =>
                r.BookingCode == bookingCode));

            return bookingCode;
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