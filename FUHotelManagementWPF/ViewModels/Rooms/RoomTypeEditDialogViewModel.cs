using System;
using System.Globalization;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>Dialog them/sua loai phong - cung mau chuan voi RoomEditDialogViewModel.</summary>
    public class RoomTypeEditDialogViewModel : ValidatableViewModelBase
    {
        private readonly IRoomTypeService _roomTypeService = new RoomTypeService();
        private readonly RoomType? _existing;

        public event Action<bool>? RequestClose;

        public string Title => _existing == null ? "Thêm loại phòng" : $"Sửa loại phòng \"{_existing.TypeName}\"";

        private string _typeName = string.Empty;
        public string TypeName
        {
            get => _typeName;
            set => SetProperty(ref _typeName, value);
        }

        private string _capacityText = "2";
        public string CapacityText
        {
            get => _capacityText;
            set => SetProperty(ref _capacityText, value);
        }

        private string _basePriceText = string.Empty;
        public string BasePriceText
        {
            get => _basePriceText;
            set => SetProperty(ref _basePriceText, value);
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
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

        public RoomTypeEditDialogViewModel(RoomType? existing)
        {
            _existing = existing;
            if (existing != null)
            {
                _typeName = existing.TypeName;
                _capacityText = existing.Capacity.ToString();
                _basePriceText = existing.BasePrice.ToString("0", CultureInfo.InvariantCulture);
                _description = existing.Description;
                _isActive = existing.IsActive;
            }

            SaveCommand = new AsyncRelayCommand(SaveAsync, _ => !IsBusy);
        }

        private async Task SaveAsync(object? _)
        {
            ClearAllErrors();
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(TypeName))
            {
                AddError(nameof(TypeName), "Chưa nhập tên loại phòng.");
            }
            else if (TypeName.Trim().Length > 50)
            {
                AddError(nameof(TypeName), "Tên loại phòng tối đa 50 ký tự.");
            }

            if (!int.TryParse(CapacityText, out var capacity) || capacity < 1)
            {
                AddError(nameof(CapacityText), "Sức chứa phải là số nguyên từ 1 trở lên.");
            }

            if (!decimal.TryParse(BasePriceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var basePrice)
                || basePrice < 0)
            {
                AddError(nameof(BasePriceText), "Giá cơ bản phải là số không âm (VD: 500000).");
            }

            if (HasErrors)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var result = _existing == null
                    ? await _roomTypeService.CreateAsync(TypeName, capacity, basePrice, Description, IsActive)
                    : await _roomTypeService.UpdateAsync(_existing.Id, TypeName, capacity, basePrice, Description, IsActive);

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
