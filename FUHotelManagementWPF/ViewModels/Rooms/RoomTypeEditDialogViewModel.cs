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

        public bool IsEdit => _existing != null;
        public string Title => IsEdit ? $"Sửa loại phòng \"{_existing!.TypeName}\"" : "Thêm loại phòng mới";
        public string Subtitle => IsEdit
            ? "Thay đổi áp dụng cho mọi phòng thuộc loại này."
            : "Định nghĩa hạng phòng mới: sức chứa, giá và mô tả bán hàng.";
        public string HeaderIcon => IsEdit ? "" : "";

        /// <summary>Anh dai dien doi theo ten loai dang go (map suite/deluxe/family/standard).</summary>
        public string PreviewImage => RoomImages.Thumbnail(TypeName);

        private string _typeName = string.Empty;
        public string TypeName
        {
            get => _typeName;
            set
            {
                if (SetProperty(ref _typeName, value))
                {
                    OnPropertyChanged(nameof(PreviewImage));
                }
            }
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
        public RelayCommand ChooseImageCommand { get; }

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
            ChooseImageCommand = new RelayCommand(_ => ChooseImage());
        }

        /// <summary>Bam vao anh -> chon file tu may cho loai phong nay.</summary>
        private void ChooseImage()
        {
            if (string.IsNullOrWhiteSpace(TypeName))
            {
                Notify.Warning("Nhập tên loại phòng trước rồi mới đổi ảnh.");
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = $"Chọn ảnh cho loại phòng {TypeName}",
                Filter = "Ảnh|*.jpg;*.jpeg;*.png",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                RoomImages.SetCustomImage(TypeName, dialog.FileName);
                OnPropertyChanged(nameof(PreviewImage));
                Notify.Success($"Đã đổi ảnh loại phòng {TypeName}.");
            }
            catch (Exception)
            {
                Notify.Error("Không sao chép được ảnh. Kiểm tra file rồi thử lại.");
            }
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
