using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>
    /// Dialog them/sua phong - MAU CHUAN dialog CRUD cho ca nhom:
    /// ValidatableViewModelBase (loi theo o) + banner loi nghiep vu + AsyncRelayCommand luu.
    /// </summary>
    public class RoomEditDialogViewModel : ValidatableViewModelBase
    {
        private readonly IRoomService _roomService = new RoomService();
        private readonly IRoomTypeService _roomTypeService = new RoomTypeService();
        private readonly Room? _existing;

        public event Action<bool>? RequestClose;

        public bool IsEdit => _existing != null;
        public string Title => IsEdit ? $"Sửa phòng {_existing!.RoomNumber}" : "Thêm phòng mới";
        public string Subtitle => IsEdit
            ? $"Đang chỉnh sửa phòng {_existing!.RoomNumber} — thay đổi có hiệu lực ngay khi lưu."
            : "Tạo phòng mới vào danh mục vận hành của khách sạn.";
        public string HeaderIcon => IsEdit ? "" : "";

        /// <summary>Anh xem truoc theo loai phong dang chon - doi loai la anh doi theo.</summary>
        public string PreviewImage => RoomImages.Thumbnail(SelectedRoomType?.TypeName ?? string.Empty);

        private string _roomNumber = string.Empty;
        public string RoomNumber
        {
            get => _roomNumber;
            set => SetProperty(ref _roomNumber, value);
        }

        private string _floorText = "1";
        public string FloorText
        {
            get => _floorText;
            set => SetProperty(ref _floorText, value);
        }

        private List<RoomType> _roomTypes = [];
        public List<RoomType> RoomTypes
        {
            get => _roomTypes;
            set => SetProperty(ref _roomTypes, value);
        }

        private RoomType? _selectedRoomType;
        public RoomType? SelectedRoomType
        {
            get => _selectedRoomType;
            set
            {
                if (SetProperty(ref _selectedRoomType, value))
                {
                    OnPropertyChanged(nameof(PreviewImage));
                }
            }
        }

        public List<StatusOption> StatusOptions { get; }

        private StatusOption _selectedStatus;
        public StatusOption SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public AsyncRelayCommand SaveCommand { get; }

        public RoomEditDialogViewModel(Room? existing)
        {
            _existing = existing;

            // Trang thai chon tay: chi cac trang thai van hanh; Da dat/Dang o do he thong
            StatusOptions =
            [
                new(RoomStatus.Available, "Trống"),
                new(RoomStatus.Cleaning, "Đang dọn"),
                new(RoomStatus.Maintenance, "Bảo trì"),
            ];
            if (existing != null
                && (existing.Status == RoomStatus.Reserved || existing.Status == RoomStatus.Occupied))
            {
                // Phong dang duoc he thong giu trang thai - hien dung hien trang, service se guard
                StatusOptions.Insert(0, new(existing.Status, RoomService.RoomStatusText(existing.Status)));
            }

            if (existing != null)
            {
                _roomNumber = existing.RoomNumber;
                _floorText = existing.Floor.ToString();
                _isActive = existing.IsActive;
                _selectedStatus = StatusOptions.First(o => o.Status == existing.Status);
            }
            else
            {
                _selectedStatus = StatusOptions[0];
            }

            SaveCommand = new AsyncRelayCommand(SaveAsync, _ => !IsBusy);
            _ = LoadRoomTypesAsync();
        }

        private async Task LoadRoomTypesAsync()
        {
            try
            {
                RoomTypes = await _roomTypeService.GetActiveAsync();
                if (_existing != null)
                {
                    SelectedRoomType = RoomTypes.FirstOrDefault(rt => rt.Id == _existing.RoomTypeId);
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không tải được danh sách loại phòng.";
            }
        }

        private async Task SaveAsync(object? _)
        {
            ClearAllErrors();
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(RoomNumber))
            {
                AddError(nameof(RoomNumber), "Chưa nhập số phòng.");
            }
            else if (RoomNumber.Trim().Length > 20)
            {
                AddError(nameof(RoomNumber), "Số phòng tối đa 20 ký tự.");
            }

            if (!int.TryParse(FloorText, out var floor) || floor <= 0)
            {
                AddError(nameof(FloorText), "Tầng phải là số nguyên lớn hơn 0.");
            }

            if (SelectedRoomType == null)
            {
                ErrorMessage = "Chưa chọn loại phòng.";
            }

            if (HasErrors || ErrorMessage != null)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var result = _existing == null
                    ? await _roomService.CreateAsync(
                        RoomNumber, floor, SelectedRoomType!.Id, SelectedStatus.Status, IsActive)
                    : await _roomService.UpdateAsync(
                        _existing.Id, RoomNumber, floor, SelectedRoomType!.Id, SelectedStatus.Status, IsActive);

                if (result.Ok)
                {
                    Notify.Success(result.Message);
                    RequestClose?.Invoke(true);
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không lưu được. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
