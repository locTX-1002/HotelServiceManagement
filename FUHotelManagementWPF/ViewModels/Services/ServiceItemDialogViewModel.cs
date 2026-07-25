using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Services
{
    /// <summary>Dialog them / sua mot mon dich vu.</summary>
    public class ServiceItemDialogViewModel : ValidatableViewModelBase
    {
        private readonly IServiceCatalogService _service = new ServiceCatalogService();
        private readonly ServiceItem? _existing;

        public event Action<bool>? RequestClose;

        public bool IsEdit => _existing != null;
        public string Title => IsEdit ? $"Sửa món {_existing!.ServiceName}" : "Thêm món dịch vụ";

        public ObservableCollection<ServiceCategory> Categories { get; } = [];

        private ServiceCategory? _selectedCategory;
        public ServiceCategory? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>Nhap gia bang chuoi de bao loi ro nghia khi go chu vao o tien.</summary>
        private string _priceText = string.Empty;
        public string PriceText
        {
            get => _priceText;
            set => SetProperty(ref _priceText, value);
        }

        private bool _isAvailable = true;
        public bool IsAvailable
        {
            get => _isAvailable;
            set => SetProperty(ref _isAvailable, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { if (SetProperty(ref _errorMessage, value)) { OnPropertyChanged(nameof(HasError)); } }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public AsyncRelayCommand SaveCommand { get; }

        public ServiceItemDialogViewModel(ServiceItem? existing, IEnumerable<ServiceCategory> categories)
        {
            _existing = existing;
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            if (existing != null)
            {
                _name = existing.ServiceName;
                _priceText = existing.UnitPrice.ToString("0", CultureInfo.InvariantCulture);
                _isAvailable = existing.IsAvailable;
                _selectedCategory = Categories.FirstOrDefault(c => c.Id == existing.ServiceCategoryId);
            }
            _selectedCategory ??= Categories.FirstOrDefault();

            SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
        }

        private async Task SaveAsync()
        {
            ClearAllErrors();
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Name))
            {
                AddError(nameof(Name), "Chưa nhập tên món.");
            }

            // Bo dau cham/phay nhom nghin roi moi doc so, de go "50.000" van hieu.
            var digits = new string((PriceText ?? string.Empty).Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                || price <= 0)
            {
                AddError(nameof(PriceText), "Giá phải là số lớn hơn 0.");
            }

            if (SelectedCategory == null)
            {
                ErrorMessage = "Chưa chọn nhóm dịch vụ.";
            }
            if (HasErrors || ErrorMessage != null)
            {
                ErrorMessage ??= FirstError();
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _service.SaveItemAsync(_existing?.Id, SelectedCategory!.Id,
                    Name, price, IsAvailable);
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
                ErrorMessage = "Không lưu được món dịch vụ. Kiểm tra kết nối rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
