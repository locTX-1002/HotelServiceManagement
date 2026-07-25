using System;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Promotions
{
    /// <summary>Dialog them/sua khuyen mai - validate tung o bang AddError, loi service len banner.</summary>
    public class PromotionEditDialogViewModel : ValidatableViewModelBase
    {
        private readonly IPromotionService _service = new PromotionService();
        private readonly Promotion? _existing;

        public event Action<bool>? RequestClose;

        public bool IsEdit => _existing != null;
        public string Title => IsEdit ? $"Sửa khuyến mãi {_existing!.Code}" : "Thêm khuyến mãi mới";
        public string Subtitle => IsEdit
            ? "Đổi giá trị, thời gian áp dụng hoặc tạm ngừng khuyến mãi."
            : "Tạo mã giảm giá để lễ tân áp vào hoá đơn.";

        private string _code = string.Empty;
        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private bool _isPercentage = true;
        public bool IsPercentage
        {
            get => _isPercentage;
            set
            {
                if (SetProperty(ref _isPercentage, value))
                {
                    OnPropertyChanged(nameof(IsFixedAmount));
                    OnPropertyChanged(nameof(ValueSuffix));
                    OnPropertyChanged(nameof(ValueHint));
                }
            }
        }

        // Hai RadioButton doi nhau: chi can 1 bien that, cai con lai la nghich dao
        // -> khong bao gio lech trang thai.
        public bool IsFixedAmount
        {
            get => !_isPercentage;
            set => IsPercentage = !value;
        }

        // Hau to sau o Gia tri doi theo loai dang chon, khoi phai doan dang nhap gi.
        public string ValueSuffix => IsPercentage ? "%" : "đ";
        public string ValueHint => IsPercentage
            ? "Nhập số phần trăm, tối đa 100 (ví dụ 10)"
            : "Nhập số tiền giảm, ví dụ 200000";

        // Nhan Gia tri bang chuoi de tu bao loi "phai la so" thay vi binding im lang bo qua.
        private string _valueInput = string.Empty;
        public string ValueInput
        {
            get => _valueInput;
            set => SetProperty(ref _valueInput, value);
        }

        private DateTime? _startDate = DateTime.Today;
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime? _endDate = DateTime.Today.AddDays(30);
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
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

        public PromotionEditDialogViewModel(Promotion? existing)
        {
            _existing = existing;

            if (existing != null)
            {
                _code = existing.Code;
                _description = existing.Description ?? string.Empty;
                _isPercentage = existing.Type == PromotionType.Percentage;
                // 0.## de o nhap khong hien "10,00" xau khi mo len sua.
                _valueInput = existing.Value.ToString("0.##");
                _startDate = existing.StartDate;
                _endDate = existing.EndDate;
                _isActive = existing.IsActive;
            }

            SaveCommand = new AsyncRelayCommand(SaveAsync, _ => !IsBusy);
        }

        private async Task SaveAsync(object? _)
        {
            ClearAllErrors();
            ErrorMessage = null;

            // Chuan hoa in hoa ngay o day de trung khop voi cach service luu (ToUpperInvariant).
            var code = Code.Trim().ToUpperInvariant();
            if (code.Length == 0)
            {
                AddError(nameof(Code), "Chưa nhập mã khuyến mãi.");
            }
            else if (code.Length > 30)
            {
                AddError(nameof(Code), "Mã tối đa 30 ký tự.");
            }

            var hasValue = decimal.TryParse(ValueInput, out var value);
            if (!hasValue || value <= 0)
            {
                AddError(nameof(ValueInput), "Giá trị phải là số lớn hơn 0.");
            }
            else if (IsPercentage && value > 100)
            {
                AddError(nameof(ValueInput), "Giảm theo phần trăm thì không quá 100.");
            }

            if (StartDate == null)
            {
                AddError(nameof(StartDate), "Chưa chọn ngày bắt đầu.");
            }
            if (EndDate == null)
            {
                AddError(nameof(EndDate), "Chưa chọn ngày kết thúc.");
            }
            if (StartDate != null && EndDate != null && EndDate.Value.Date < StartDate.Value.Date)
            {
                AddError(nameof(EndDate), "Ngày kết thúc phải từ ngày bắt đầu trở đi.");
            }

            if (HasErrors)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var type = IsPercentage ? PromotionType.Percentage : PromotionType.FixedAmount;
                var result = await _service.SaveAsync(
                    _existing?.Id,
                    code,
                    Description,
                    type,
                    value,
                    StartDate!.Value,
                    EndDate!.Value,
                    IsActive);

                if (result.Ok)
                {
                    Notify.Success(result.Message);
                    RequestClose?.Invoke(true);
                }
                else
                {
                    // Loi nghiep vu (trung ma, khong co quyen...) - giu nguyen form cho sua lai.
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
