using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Reservations;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    // Khach tu dat phong online - luon tao o trang thai Pending (le tan phai xac nhan), khong thu
    // coc. Tat ca rule trong phong, trung lich va suc chua tiep tuc dung chung ReservationService.
    public class GuestReservationService : IGuestReservationService
    {
        private readonly HotelDbContext _context;
        private readonly IReservationService _reservationService;

        public GuestReservationService(
            HotelDbContext context,
            IReservationService reservationService)
        {
            _context = context;
            _reservationService = reservationService;
        }

        public async Task<AuthServiceResult<IReadOnlyList<AvailableRoomTypeResponse>>> GetAvailableRoomTypesAsync(
            DateTime checkInDate,
            DateTime checkOutDate,
            int? numberOfGuests)
        {
            if (checkOutDate.Date <= checkInDate.Date)
            {
                return AuthServiceResult<IReadOnlyList<AvailableRoomTypeResponse>>.Failure(
                    "CheckOutDate must be after CheckInDate.");
            }

            var roomsResult = await _reservationService.GetAvailableRoomsAsync(
                checkInDate,
                checkOutDate,
                roomTypeId: null,
                capacity: numberOfGuests);

            if (!roomsResult.IsSuccess)
            {
                return AuthServiceResult<IReadOnlyList<AvailableRoomTypeResponse>>.Failure(
                    roomsResult.Message,
                    roomsResult.StatusCode);
            }

            var grouped = (roomsResult.Data ?? Array.Empty<AvailableRoomResponse>())
                .GroupBy(r => r.RoomTypeId)
                .Select(g => new AvailableRoomTypeResponse
                {
                    RoomTypeId = g.Key,
                    RoomTypeName = g.First().RoomTypeName,
                    Description = g.First().Description,
                    Capacity = g.First().Capacity,
                    BasePrice = g.First().BasePrice,
                    AvailableCount = g.Count()
                })
                .OrderBy(r => r.BasePrice)
                .ToList();

            return AuthServiceResult<IReadOnlyList<AvailableRoomTypeResponse>>.Success(
                grouped);
        }

        public async Task<AuthServiceResult<ReservationResponse>> CreateAsync(
            int guestId,
            GuestCreateReservationRequest request)
        {
            if (guestId <= 0)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Authenticated guest id is invalid.", 401);
            }

            if (request == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "Request body is required.");
            }

            if (request.CheckOutDate.Date <= request.CheckInDate.Date)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "CheckOutDate must be after CheckInDate.");
            }

            if (request.CheckInDate.Date < DateTime.UtcNow.Date)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "CheckInDate cannot be in the past.");
            }

            var roomsResult = await _reservationService.GetAvailableRoomsAsync(
                request.CheckInDate,
                request.CheckOutDate,
                request.RoomTypeId,
                request.NumberOfGuests);

            if (!roomsResult.IsSuccess)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    roomsResult.Message,
                    roomsResult.StatusCode);
            }

            var room = roomsResult.Data?.FirstOrDefault();
            if (room == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "No rooms of this type are available for the selected dates.",
                    409);
            }

            var createRequest = new CreateReservationRequest
            {
                GuestId = guestId,
                RoomId = room.Id,
                NumberOfGuests = request.NumberOfGuests,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Status = ReservationStatus.Pending,
                SpecialRequests = request.SpecialRequests
            };

            return await _reservationService.CreateAsync(
                createRequest,
                createdByUserId: null);
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> CancelAsync(
            int guestId,
            int reservationId)
        {
            if (guestId <= 0)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure(
                    "Authenticated guest id is invalid.", 401);
            }

            if (reservationId <= 0)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure(
                    "Reservation id must be greater than 0.");
            }

            // Deliberately query by both reservation id and authenticated guest id.
            // Returning 404 also avoids revealing whether another guest owns that id.
            var reservation = await _context.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Id == reservationId
                    && r.GuestId == guestId);

            if (reservation == null)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure(
                    "Reservation not found.", 404);
            }

            if (reservation.Status != ReservationStatus.Pending
                && reservation.Status != ReservationStatus.Confirmed)
            {
                return AuthServiceResult<AuthMessageResponse>.Failure(
                    "Only Pending or Confirmed reservations can be cancelled.",
                    409);
            }

            return await _reservationService.CancelAsync(reservationId);
        }
    }
}
