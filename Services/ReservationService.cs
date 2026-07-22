using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;

namespace Services
{
    /// <summary>
    /// Nghiệp vụ Đặt phòng - port rule từ web (đã test kỹ): chỉ tạo/sửa ở Pending/Confirmed,
    /// chặn quá sức chứa, chặn trùng khoảng ngày (overlap + phòng đang có khách ở),
    /// No-show chỉ từ Confirmed, đặt/huỷ tự tính lại trạng thái phòng.
    /// </summary>
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _repo = new ReservationRepository();

        public Task<List<Reservation>> GetAllAsync() => _repo.GetAllAsync();

        public async Task<ServiceResult<List<Room>>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
        {
            var dateError = ValidateDates(checkIn, checkOut);
            if (dateError != null)
            {
                return ServiceResult<List<Room>>.Failure(dateError);
            }
            var rooms = await _repo.GetAvailableRoomsAsync(checkIn, checkOut, null, null);
            return ServiceResult<List<Room>>.Success(rooms);
        }

        public async Task<ServiceResult<Reservation>> CreateAsync(int guestId, int roomId, int numberOfGuests,
            DateTime checkIn, DateTime checkOut, ReservationStatus status, string? specialRequests, int? createdByUserId)
        {
            var error = ValidateInput(guestId, roomId, checkIn, checkOut, status);
            if (error != null)
            {
                return ServiceResult<Reservation>.Failure(error);
            }

            if (await _repo.GetGuestAsync(guestId) == null)
            {
                return ServiceResult<Reservation>.Failure("Khách hàng không tồn tại.");
            }

            var room = await _repo.GetReservableRoomAsync(roomId);
            if (room == null)
            {
                return ServiceResult<Reservation>.Failure("Phòng không tồn tại hoặc đã ngừng dùng.");
            }
            if (room.Status == RoomStatus.Maintenance)
            {
                return ServiceResult<Reservation>.Failure("Phòng đang bảo trì, không đặt được.");
            }

            var capacityError = ValidateGuestCount(numberOfGuests, room.RoomType.Capacity);
            if (capacityError != null)
            {
                return ServiceResult<Reservation>.Failure(capacityError);
            }

            if (await _repo.HasOverlapAsync(roomId, checkIn, checkOut, null))
            {
                return ServiceResult<Reservation>.Failure("Phòng đã có đặt phòng trong khoảng ngày này.");
            }

            var reservation = new Reservation
            {
                BookingCode = await GenerateBookingCodeAsync(),
                GuestId = guestId,
                RoomId = roomId,
                NumberOfGuests = numberOfGuests,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Status = status,
                SpecialRequests = string.IsNullOrWhiteSpace(specialRequests) ? null : specialRequests.Trim(),
                CreatedByUserId = createdByUserId,
            };

            await _repo.CreateAsync(reservation);
            return ServiceResult<Reservation>.Success(reservation, "Đã tạo đặt phòng.");
        }

        public async Task<ServiceResult> UpdateAsync(int id, int guestId, int roomId, int numberOfGuests,
            DateTime checkIn, DateTime checkOut, ReservationStatus status, string? specialRequests)
        {
            var error = ValidateInput(guestId, roomId, checkIn, checkOut, status);
            if (error != null)
            {
                return ServiceResult.Failure(error);
            }

            var current = await _repo.GetByIdAsync(id);
            if (current == null)
            {
                return ServiceResult.Failure("Không tìm thấy đặt phòng.");
            }
            if (!IsEditable(current.Status))
            {
                return ServiceResult.Failure("Đặt phòng đã huỷ / check-in / hoàn tất không sửa được.");
            }
            if (await _repo.HasStayAsync(id))
            {
                return ServiceResult.Failure("Đặt phòng đã có lượt lưu trú, không sửa được.");
            }
            if (await _repo.GetGuestAsync(guestId) == null)
            {
                return ServiceResult.Failure("Khách hàng không tồn tại.");
            }

            var room = await _repo.GetReservableRoomAsync(roomId);
            if (room == null)
            {
                return ServiceResult.Failure("Phòng không tồn tại hoặc đã ngừng dùng.");
            }
            if (room.Status == RoomStatus.Maintenance)
            {
                return ServiceResult.Failure("Phòng đang bảo trì, không đặt được.");
            }

            var capacityError = ValidateGuestCount(numberOfGuests, room.RoomType.Capacity);
            if (capacityError != null)
            {
                return ServiceResult.Failure(capacityError);
            }
            if (await _repo.HasOverlapAsync(roomId, checkIn, checkOut, excludeId: id))
            {
                return ServiceResult.Failure("Phòng đã có đặt phòng trong khoảng ngày này.");
            }

            await _repo.UpdateAsync(id, guestId, roomId, numberOfGuests, checkIn, checkOut, status,
                string.IsNullOrWhiteSpace(specialRequests) ? null : specialRequests.Trim());
            return ServiceResult.Success("Đã cập nhật đặt phòng.");
        }

        public async Task<ServiceResult> ConfirmAsync(int id)
        {
            var res = await _repo.GetByIdAsync(id);
            if (res == null)
            {
                return ServiceResult.Failure("Không tìm thấy đặt phòng.");
            }
            if (res.Status != ReservationStatus.Pending)
            {
                return ServiceResult.Failure("Chỉ đặt phòng đang chờ mới xác nhận được.");
            }
            await _repo.SetStatusAsync(id, ReservationStatus.Confirmed);
            return ServiceResult.Success("Đã xác nhận đặt phòng.");
        }

        public async Task<ServiceResult> CancelAsync(int id)
        {
            var res = await _repo.GetByIdAsync(id);
            if (res == null)
            {
                return ServiceResult.Failure("Không tìm thấy đặt phòng.");
            }
            if (res.Status == ReservationStatus.Cancelled)
            {
                return ServiceResult.Failure("Đặt phòng đã huỷ trước đó.");
            }
            if (!IsEditable(res.Status))
            {
                return ServiceResult.Failure("Đặt phòng đã check-in / hoàn tất không huỷ trực tiếp được.");
            }
            if (await _repo.HasStayAsync(id))
            {
                return ServiceResult.Failure("Đặt phòng đã có lượt lưu trú, không huỷ được.");
            }
            await _repo.SetStatusAsync(id, ReservationStatus.Cancelled);
            return ServiceResult.Success("Đã huỷ đặt phòng.");
        }

        public async Task<ServiceResult> NoShowAsync(int id)
        {
            var res = await _repo.GetByIdAsync(id);
            if (res == null)
            {
                return ServiceResult.Failure("Không tìm thấy đặt phòng.");
            }
            if (res.Status != ReservationStatus.Confirmed)
            {
                return ServiceResult.Failure("Chỉ đặt phòng đã xác nhận mới đánh dấu Không đến được.");
            }
            if (await _repo.HasStayAsync(id))
            {
                return ServiceResult.Failure("Đặt phòng đã có lượt lưu trú, không đánh dấu Không đến được.");
            }
            await _repo.SetStatusAsync(id, ReservationStatus.NoShow);
            return ServiceResult.Success("Đã đánh dấu Không đến. Tiền cọc (nếu có) xử lý thủ công ngoài hệ thống.");
        }

        /// <summary>Tên tiếng Việt của trạng thái đặt phòng - dùng chung cho UI.</summary>
        public static string StatusText(ReservationStatus status) => status switch
        {
            ReservationStatus.Pending => "Chờ xác nhận",
            ReservationStatus.Confirmed => "Đã xác nhận",
            ReservationStatus.Cancelled => "Đã huỷ",
            ReservationStatus.CheckedIn => "Đã check-in",
            ReservationStatus.Completed => "Hoàn tất",
            ReservationStatus.NoShow => "Không đến",
            _ => status.ToString(),
        };

        private static bool IsEditable(ReservationStatus s)
            => s == ReservationStatus.Pending || s == ReservationStatus.Confirmed;

        private static string? ValidateInput(int guestId, int roomId, DateTime checkIn, DateTime checkOut, ReservationStatus status)
        {
            if (guestId <= 0)
            {
                return "Chưa chọn khách hàng.";
            }
            if (roomId <= 0)
            {
                return "Chưa chọn phòng.";
            }
            var dateError = ValidateDates(checkIn, checkOut);
            if (dateError != null)
            {
                return dateError;
            }
            if (status != ReservationStatus.Pending && status != ReservationStatus.Confirmed)
            {
                return "Đặt phòng chỉ tạo/sửa ở trạng thái Chờ hoặc Đã xác nhận.";
            }
            return null;
        }

        private static string? ValidateDates(DateTime checkIn, DateTime checkOut)
        {
            if (checkIn == default || checkOut == default)
            {
                return "Chưa chọn ngày nhận/trả phòng.";
            }
            return checkOut <= checkIn ? "Ngày trả phải sau ngày nhận." : null;
        }

        private static string? ValidateGuestCount(int numberOfGuests, int capacity)
        {
            if (numberOfGuests < 1)
            {
                return "Số khách phải từ 1 trở lên.";
            }
            return numberOfGuests > capacity ? $"Số khách vượt sức chứa phòng ({capacity})." : null;
        }

        private async Task<string> GenerateBookingCodeAsync()
        {
            string code;
            do
            {
                code = $"RES-{DateTime.Now:yyyyMMddHHmmssfff}";
            }
            while (await _repo.BookingCodeExistsAsync(code));
            return code;
        }
    }
}
