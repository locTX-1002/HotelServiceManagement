using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Reservations;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Infrastructure.Services
{
    // Khach tu dat phong online - luon tao o trang thai Pending (le tan phai xac nhan), khong thu
    // coc (khong co cong thanh toan that trong he thong nay - coc van la nghiep vu quay le tan).
    // Tan dung nguyen ven logic kiem tra phong trong/trung lich/suc chua da hardened o ReservationService,
    // khong viet lai.
    public class GuestReservationService : IGuestReservationService
    {
        private readonly IReservationService _reservationService;

        public GuestReservationService(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        public async Task<AuthServiceResult<IReadOnlyList<AvailableRoomTypeResponse>>> GetAvailableRoomTypesAsync(
            DateTime checkInDate, DateTime checkOutDate, int? numberOfGuests)
        {
            if (checkOutDate.Date <= checkInDate.Date)
            {
                return AuthServiceResult<IReadOnlyList<AvailableRoomTypeResponse>>.Failure(
                    "CheckOutDate must be after CheckInDate.");
            }

            var roomsResult = await _reservationService.GetAvailableRoomsAsync(
                checkInDate, checkOutDate, roomTypeId: null, capacity: numberOfGuests);

            if (!roomsResult.IsSuccess)
            {
                return AuthServiceResult<IReadOnlyList<AvailableRoomTypeResponse>>.Failure(
                    roomsResult.Message, roomsResult.StatusCode);
            }

            var grouped = (roomsResult.Data ?? Array.Empty<AvailableRoomResponse>())
                .GroupBy(r => r.RoomTypeId)
                .Select(g => new AvailableRoomTypeResponse
                {
                    RoomTypeId = g.Key,
                    RoomTypeName = g.First().RoomTypeName,
                    Capacity = g.First().Capacity,
                    BasePrice = g.First().BasePrice,
                    AvailableCount = g.Count()
                })
                .OrderBy(r => r.BasePrice)
                .ToList();

            return AuthServiceResult<IReadOnlyList<AvailableRoomTypeResponse>>.Success(grouped);
        }

        public async Task<AuthServiceResult<ReservationResponse>> CreateAsync(int guestId, GuestCreateReservationRequest request)
        {
            if (request == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure("Request body is required.");
            }

            if (request.CheckOutDate.Date <= request.CheckInDate.Date)
            {
                return AuthServiceResult<ReservationResponse>.Failure("CheckOutDate must be after CheckInDate.");
            }

            if (request.CheckInDate.Date < DateTime.UtcNow.Date)
            {
                return AuthServiceResult<ReservationResponse>.Failure("CheckInDate cannot be in the past.");
            }

            var roomsResult = await _reservationService.GetAvailableRoomsAsync(
                request.CheckInDate, request.CheckOutDate, request.RoomTypeId, request.NumberOfGuests);

            if (!roomsResult.IsSuccess)
            {
                return AuthServiceResult<ReservationResponse>.Failure(roomsResult.Message, roomsResult.StatusCode);
            }

            var room = roomsResult.Data?.FirstOrDefault();
            if (room == null)
            {
                return AuthServiceResult<ReservationResponse>.Failure(
                    "No rooms of this type are available for the selected dates.", 409);
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

            // createdByUserId=null: khong co nhan vien nao tao dat phong nay, khach tu tao. Neu 2
            // khach cung dat trung 1 phong o 2 request gan nhu dong thoi, CreateAsync tu kiem tra
            // trung lich lan cuoi ngay truoc khi luu (da hardened san) nen van an toan.
            return await _reservationService.CreateAsync(createRequest, createdByUserId: null);
        }
    }
}
