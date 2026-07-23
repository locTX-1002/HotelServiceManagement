using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;
using Repositories;

namespace Services
{
    /// <summary>
    /// Nghiep vu Phong cua ung dung desktop:
    /// - Reserved/Occupied do luong Dat phong/Check-in dieu khien, khong set tay
    /// - Phong dang co khach/dat phong hoat dong: khong doi trang thai tuy tien,
    ///   khong ngung dung, khong doi sang loai phong nho hon so khach da dat
    /// - Xoa: co lich su thi xoa mem, chua dung bao gio moi xoa that
    /// - Trung so phong: chan truoc + do unique index DB (chong race)
    /// </summary>
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository = new RoomRepository();
        private readonly IRoomTypeRepository _roomTypeRepository = new RoomTypeRepository();

        public Task<List<Room>> GetAllAsync() => _roomRepository.GetAllAsync();

        public async Task<ServiceResult<Room>> CreateAsync(
            string roomNumber, int floor, int roomTypeId, RoomStatus status, bool isActive)
        {
            if (!AuthorizationPolicy.CanManageRooms)
                return ServiceResult<Room>.Failure("Chi Admin hoac Manager duoc tao phong.");
            var error = Validate(roomNumber, floor, roomTypeId, status, isCreating: true);
            if (error != null)
            {
                return ServiceResult<Room>.Failure(error);
            }

            var normalized = roomNumber.Trim();
            if (await _roomRepository.RoomNumberExistsAsync(normalized))
            {
                return ServiceResult<Room>.Failure("Số phòng đã tồn tại.");
            }

            var roomType = await _roomTypeRepository.GetByIdAsync(roomTypeId);
            if (roomType == null || !roomType.IsActive)
            {
                return ServiceResult<Room>.Failure("Loại phòng không tồn tại hoặc đã ngừng dùng.");
            }

            var room = new Room
            {
                RoomNumber = normalized,
                Floor = floor,
                RoomTypeId = roomTypeId,
                Status = status,
                IsActive = isActive,
            };

            try
            {
                await _roomRepository.AddAsync(room);
            }
            catch (DbUpdateException)
            {
                // Unique index DB vua chan ban ghi trung tao dong thoi - dich thanh loi sach
                if (await _roomRepository.RoomNumberExistsAsync(normalized))
                {
                    return ServiceResult<Room>.Failure("Số phòng đã tồn tại.");
                }
                throw;
            }

            room.RoomType = roomType;
            return ServiceResult<Room>.Success(room, "Đã tạo phòng.");
        }

        public async Task<ServiceResult<Room>> UpdateAsync(
            int id, string roomNumber, int floor, int roomTypeId, RoomStatus status, bool isActive)
        {
            if (!AuthorizationPolicy.CanManageRooms)
                return ServiceResult<Room>.Failure("Chi Admin hoac Manager duoc sua phong.");
            var error = Validate(roomNumber, floor, roomTypeId, status, isCreating: false);
            if (error != null)
            {
                return ServiceResult<Room>.Failure(error);
            }

            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                return ServiceResult<Room>.Failure("Không tìm thấy phòng.");
            }

            var normalized = roomNumber.Trim();
            if (await _roomRepository.RoomNumberExistsAsync(normalized, excludeId: id))
            {
                return ServiceResult<Room>.Failure("Số phòng đã tồn tại.");
            }

            var roomType = await _roomTypeRepository.GetByIdAsync(roomTypeId);
            if (roomType == null || !roomType.IsActive)
            {
                return ServiceResult<Room>.Failure("Loại phòng không tồn tại hoặc đã ngừng dùng.");
            }

            var hasOccupied = await _roomRepository.HasActiveStayAsync(id)
                              || await _roomRepository.HasCheckedInReservationAsync(id);
            var hasPendingOrConfirmed = await _roomRepository.HasPendingOrConfirmedReservationAsync(id);
            var hasBlocking = hasOccupied || hasPendingOrConfirmed;

            if (!isActive && hasBlocking)
            {
                return ServiceResult<Room>.Failure(
                    "Phòng đang có đặt phòng hoặc khách ở — không thể ngừng dùng.");
            }
            if (hasOccupied && status != RoomStatus.Occupied)
            {
                return ServiceResult<Room>.Failure("Phòng đang có khách ở phải giữ trạng thái Đang ở.");
            }
            if (!hasOccupied && hasPendingOrConfirmed && status != RoomStatus.Reserved)
            {
                return ServiceResult<Room>.Failure(
                    "Phòng đang có đặt phòng chờ/đã xác nhận phải giữ trạng thái Đã đặt.");
            }
            if (!hasBlocking && (status == RoomStatus.Reserved || status == RoomStatus.Occupied))
            {
                return ServiceResult<Room>.Failure(
                    "Trạng thái Đã đặt / Đang ở do luồng đặt phòng và check-in tự điều khiển.");
            }

            if (roomTypeId != room.RoomTypeId
                && await _roomRepository.HasReservationExceedingCapacityAsync(id, roomType.Capacity))
            {
                return ServiceResult<Room>.Failure(
                    "Không thể đổi loại phòng vì có đặt phòng vượt sức chứa của loại mới.");
            }

            room.RoomNumber = normalized;
            room.Floor = floor;
            room.RoomTypeId = roomTypeId;
            room.Status = status;
            room.IsActive = isActive;
            room.RoomType = null!;   // entity detached - khong de EF dinh navigation cu

            try
            {
                await _roomRepository.UpdateAsync(room);
            }
            catch (DbUpdateException)
            {
                if (await _roomRepository.RoomNumberExistsAsync(normalized, excludeId: id))
                {
                    return ServiceResult<Room>.Failure("Số phòng đã tồn tại.");
                }
                throw;
            }

            room.RoomType = roomType;
            return ServiceResult<Room>.Success(room, "Đã cập nhật phòng.");
        }

        public async Task<ServiceResult<Room>> UpdateStatusAsync(
            int id, RoomStatus newStatus, bool canManageMaintenance)
        {
            // Giu tham so de frontend cu van build, nhung khong tin quyen do caller truyen vao.
            canManageMaintenance = AuthorizationPolicy.CanManageRooms;
            if (!canManageMaintenance)
                return ServiceResult<Room>.Failure("Chi Admin hoac Manager duoc doi trang thai phong.");
            if (newStatus == RoomStatus.Reserved || newStatus == RoomStatus.Occupied)
            {
                return ServiceResult<Room>.Failure(
                    "Trạng thái Đã đặt / Đang ở do luồng đặt phòng và check-in tự điều khiển.");
            }

            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                return ServiceResult<Room>.Failure("Không tìm thấy phòng.");
            }
            if (!room.IsActive)
            {
                return ServiceResult<Room>.Failure("Phòng đã ngừng dùng — không đổi trạng thái vận hành.");
            }
            if (room.Status == newStatus)
            {
                return ServiceResult<Room>.Success(room, "Trạng thái phòng không đổi.");
            }

            var hasOccupied = await _roomRepository.HasActiveStayAsync(id)
                              || await _roomRepository.HasCheckedInReservationAsync(id);
            if (hasOccupied)
            {
                return ServiceResult<Room>.Failure("Phòng đang có khách ở phải giữ trạng thái Đang ở.");
            }

            var hasPendingOrConfirmed = await _roomRepository.HasPendingOrConfirmedReservationAsync(id);

            switch (room.Status)
            {
                case RoomStatus.Available when newStatus == RoomStatus.Cleaning:
                    if (hasPendingOrConfirmed)
                    {
                        return ServiceResult<Room>.Failure(
                            "Phòng đang có đặt phòng chờ/đã xác nhận — không chuyển sang Đang dọn.");
                    }
                    room.Status = RoomStatus.Cleaning;
                    break;

                case RoomStatus.Cleaning when newStatus == RoomStatus.Available:
                    // Don xong: neu da co dat phong dang cho thi tra ve Da dat, khong pho bay Trong
                    room.Status = hasPendingOrConfirmed ? RoomStatus.Reserved : RoomStatus.Available;
                    break;

                case RoomStatus.Available when newStatus == RoomStatus.Maintenance:
                    if (!canManageMaintenance)
                    {
                        return ServiceResult<Room>.Failure(
                            "Chỉ Quản trị viên hoặc Quản lý được đưa phòng vào bảo trì.");
                    }
                    if (hasPendingOrConfirmed)
                    {
                        return ServiceResult<Room>.Failure(
                            "Phòng đang có đặt phòng chờ/đã xác nhận — không đưa vào bảo trì.");
                    }
                    room.Status = RoomStatus.Maintenance;
                    break;

                case RoomStatus.Maintenance when newStatus == RoomStatus.Available:
                    if (!canManageMaintenance)
                    {
                        return ServiceResult<Room>.Failure(
                            "Chỉ Quản trị viên hoặc Quản lý được đưa phòng ra khỏi bảo trì.");
                    }
                    room.Status = hasPendingOrConfirmed ? RoomStatus.Reserved : RoomStatus.Available;
                    break;

                default:
                    return ServiceResult<Room>.Failure(
                        $"Không thể chuyển trạng thái từ {RoomStatusText(room.Status)} sang {RoomStatusText(newStatus)}.");
            }

            var savedRoomType = room.RoomType;
            room.RoomType = null!;
            await _roomRepository.UpdateAsync(room);
            room.RoomType = savedRoomType;

            return ServiceResult<Room>.Success(room, "Đã đổi trạng thái phòng.");
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            if (!AuthorizationPolicy.CanManageRooms)
                return ServiceResult.Failure("Chi Admin hoac Manager duoc xoa hoac ngung dung phong.");
            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                return ServiceResult.Failure("Không tìm thấy phòng.");
            }

            var hasActiveBusiness = await _roomRepository.HasActiveStayAsync(id)
                || await _roomRepository.HasCheckedInReservationAsync(id)
                || await _roomRepository.HasPendingOrConfirmedReservationAsync(id);

            if (hasActiveBusiness)
            {
                return ServiceResult.Failure(
                    "Phòng đang có đặt phòng hoặc khách ở — không thể xoá hay ngừng dùng.");
            }

            if (await _roomRepository.HasAnyReservationAsync(id))
            {
                room.IsActive = false;
                room.RoomType = null!;
                await _roomRepository.UpdateAsync(room);
                return ServiceResult.Success(
                    "Phòng đã có lịch sử đặt nên được chuyển sang ngừng dùng thay vì xoá.");
            }

            room.RoomType = null!;
            await _roomRepository.DeleteAsync(room);
            return ServiceResult.Success("Đã xoá phòng.");
        }

        /// <summary>Ten tieng Viet cua trang thai phong - dung chung cho UI.</summary>
        public static string RoomStatusText(RoomStatus status) => status switch
        {
            RoomStatus.Available => "Trống",
            RoomStatus.Reserved => "Đã đặt",
            RoomStatus.Occupied => "Đang ở",
            RoomStatus.Cleaning => "Đang dọn",
            RoomStatus.Maintenance => "Bảo trì",
            _ => status.ToString(),
        };

        private static string? Validate(
            string roomNumber, int floor, int roomTypeId, RoomStatus status, bool isCreating)
        {
            if (string.IsNullOrWhiteSpace(roomNumber))
            {
                return "Chưa nhập số phòng.";
            }
            if (roomNumber.Trim().Length > 20)
            {
                return "Số phòng tối đa 20 ký tự.";
            }
            if (floor <= 0)
            {
                return "Tầng phải lớn hơn 0.";
            }
            if (roomTypeId <= 0)
            {
                return "Chưa chọn loại phòng.";
            }
            if (!Enum.IsDefined(status))
            {
                return "Trạng thái phòng không hợp lệ.";
            }
            if (isCreating && (status == RoomStatus.Reserved || status == RoomStatus.Occupied))
            {
                return "Phòng mới không thể bắt đầu ở trạng thái Đã đặt / Đang ở.";
            }
            return null;
        }
    }
}
