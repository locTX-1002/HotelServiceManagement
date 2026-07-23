using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        /// <summary>
        /// Ảnh xem trước: ảnh vừa chọn (nếu có) → ảnh riêng của loại → ảnh mẫu theo tên.
        /// Chọn được ngay cả khi chưa nhập tên; loại tạo mới sẽ gán ảnh sau khi lưu (đã có Id).
        /// </summary>
        public string PreviewImage => _pendingImagePath
            ?? RoomImages.Thumbnail(_existing?.Id ?? 0, TypeName);

        private string? _pendingImagePath;

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
            set
            {
                if (SetProperty(ref _basePriceText, value))
                {
                    OnPropertyChanged(nameof(PriceHint));
                }
            }
        }

        /// <summary>
        /// Đọc lại số tiền vừa gõ cho dễ kiểm: "20000" → "= 20.000 đ / đêm".
        /// Tiền VND nhiều số 0 rất dễ gõ thiếu/thừa, dòng này bắt lỗi ngay bằng mắt.
        /// </summary>
        public string PriceHint
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_basePriceText))
                {
                    return "Nhập số tiền, ví dụ 500000";
                }
                var digits = new string(_basePriceText.Where(char.IsDigit).ToArray());
                if (!decimal.TryParse(digits, out var value))
                {
                    return "Chỉ nhập chữ số";
                }
                return $"= {value:N0} đ / đêm";
            }
        }

        /// <summary>Mức giá hay dùng - bấm là điền luôn, khỏi gõ 6 số 0.</summary>
        public List<decimal> QuickPrices { get; } = [300_000, 500_000, 800_000, 1_200_000, 1_500_000];

        public RelayCommand PickPriceCommand { get; }

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
            PickPriceCommand = new RelayCommand(p =>
            {
                if (p is decimal price)
                {
                    BasePriceText = price.ToString("0", CultureInfo.InvariantCulture);
                }
            });
        }

        /// <summary>
        /// Bấm vào ảnh → chọn file từ máy. Không cần nhập tên trước: đang sửa thì gán ngay theo Id,
        /// tạo mới thì giữ tạm và gán sau khi lưu (lúc đó mới có Id).
        /// </summary>
        private void ChooseImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn ảnh cho loại phòng",
                Filter = "Ảnh|*.jpg;*.jpeg;*.png",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (_existing != null)
                {
                    RoomImages.SetCustomImage(_existing.Id, dialog.FileName);
                    _pendingImagePath = null;
                    Notify.Success("Đã đổi ảnh loại phòng.");
                }
                else
                {
                    // Chưa có Id - xem trước ngay, gán thật khi lưu xong
                    _pendingImagePath = dialog.FileName;
                }
                OnPropertyChanged(nameof(PreviewImage));
            }
            catch (Exception)
            {
                Notify.Error("Không đọc được ảnh. Kiểm tra file rồi thử lại.");
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
                    // Loại mới vừa có Id -> giờ mới gán được ảnh đã chọn lúc chưa lưu
                    if (_pendingImagePath != null && result.Data != null)
                    {
                        try
                        {
                            RoomImages.SetCustomImage(result.Data.Id, _pendingImagePath);
                        }
                        catch (Exception)
                        {
                            Notify.Warning("Đã lưu loại phòng nhưng chưa gán được ảnh.");
                        }
                    }
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
