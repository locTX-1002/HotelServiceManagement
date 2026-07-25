using System;
using System.Threading.Tasks;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Services
{
    /// <summary>Dialog nho: them mot nhom dich vu (Nha hang, Giat la, ...).</summary>
    public class ServiceCategoryDialogViewModel : ValidatableViewModelBase
    {
        private readonly IServiceCatalogService _service = new ServiceCatalogService();

        public event Action<bool>? RequestClose;

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
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

        public ServiceCategoryDialogViewModel()
        {
            SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
        }

        private async Task SaveAsync()
        {
            ClearAllErrors();
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Name))
            {
                AddError(nameof(Name), "Chưa nhập tên nhóm.");
            }
            if (HasErrors)
            {
                ErrorMessage = FirstError();
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _service.SaveCategoryAsync(null, Name, active: true);
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
                ErrorMessage = "Không lưu được nhóm dịch vụ. Kiểm tra kết nối rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
