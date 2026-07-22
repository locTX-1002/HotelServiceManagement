using BusinessObjects.Entities;
using Repositories;

namespace Services
{
    /// <summary>
    /// Nghiep vu Loai phong cua ung dung desktop:
    /// ten unique, khong giam suc chua duoi so khach cua dat phong dang hoat dong,
    /// xoa mem khi da co phong dung.
    /// </summary>
    public class RoomTypeService : IRoomTypeService
    {
        private readonly IRoomTypeRepository _roomTypeRepository = new RoomTypeRepository();

        public Task<List<RoomType>> GetAllAsync() => _roomTypeRepository.GetAllAsync();
        public Task<List<RoomType>> GetActiveAsync() => _roomTypeRepository.GetActiveAsync();

        public async Task<ServiceResult<RoomType>> CreateAsync(
            string typeName, int capacity, decimal basePrice, string? description, bool isActive)
        {
            var error = Validate(typeName, capacity, basePrice);
            if (error != null)
            {
                return ServiceResult<RoomType>.Failure(error);
            }

            if (await _roomTypeRepository.NameExistsAsync(typeName))
            {
                return ServiceResult<RoomType>.Failure("Tên loại phòng đã tồn tại.");
            }

            var roomType = new RoomType
            {
                TypeName = typeName.Trim(),
                Capacity = capacity,
                BasePrice = basePrice,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                IsActive = isActive,
            };

            await _roomTypeRepository.AddAsync(roomType);
            return ServiceResult<RoomType>.Success(roomType, "Đã tạo loại phòng.");
        }

        public async Task<ServiceResult<RoomType>> UpdateAsync(
            int id, string typeName, int capacity, decimal basePrice, string? description, bool isActive)
        {
            var error = Validate(typeName, capacity, basePrice);
            if (error != null)
            {
                return ServiceResult<RoomType>.Failure(error);
            }

            var roomType = await _roomTypeRepository.GetByIdAsync(id);
            if (roomType == null)
            {
                return ServiceResult<RoomType>.Failure("Không tìm thấy loại phòng.");
            }

            if (await _roomTypeRepository.NameExistsAsync(typeName, excludeId: id))
            {
                return ServiceResult<RoomType>.Failure("Tên loại phòng đã tồn tại.");
            }

            // Khong duoc giam suc chua xuong duoi so khach cua dat phong dang hoat dong
            if (capacity < roomType.Capacity
                && await _roomTypeRepository.HasReservationExceedingCapacityAsync(id, capacity))
            {
                return ServiceResult<RoomType>.Failure(
                    "Không thể giảm sức chứa vì đang có đặt phòng vượt sức chứa mới.");
            }

            roomType.TypeName = typeName.Trim();
            roomType.Capacity = capacity;
            roomType.BasePrice = basePrice;
            roomType.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            roomType.IsActive = isActive;
            roomType.Rooms = [];   // entity detached - khong de EF dinh vao navigation cu

            await _roomTypeRepository.UpdateAsync(roomType);
            return ServiceResult<RoomType>.Success(roomType, "Đã cập nhật loại phòng.");
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var roomType = await _roomTypeRepository.GetByIdAsync(id);
            if (roomType == null)
            {
                return ServiceResult.Failure("Không tìm thấy loại phòng.");
            }

            // Giu lich su + khoa ngoai: loai da co phong dung thi chi ngung dung (xoa mem)
            if (roomType.Rooms.Any())
            {
                roomType.IsActive = false;
                roomType.Rooms = [];
                await _roomTypeRepository.UpdateAsync(roomType);
                return ServiceResult.Success(
                    "Loại phòng đang được sử dụng nên đã chuyển sang ngừng dùng thay vì xoá.");
            }

            await _roomTypeRepository.DeleteAsync(roomType);
            return ServiceResult.Success("Đã xoá loại phòng.");
        }

        private static string? Validate(string typeName, int capacity, decimal basePrice)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return "Chưa nhập tên loại phòng.";
            }
            if (typeName.Trim().Length > 50)
            {
                return "Tên loại phòng tối đa 50 ký tự.";
            }
            if (capacity < 1)
            {
                return "Sức chứa phải từ 1 trở lên.";
            }
            return basePrice < 0 ? "Giá cơ bản không được âm." : null;
        }
    }
}
