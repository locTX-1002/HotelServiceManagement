using BusinessObjects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Services
{
    /// <summary>Mot mon trong danh muc, boc lai de View khoi tu ghep chuoi.</summary>
    public class ServiceItemRow
    {
        public ServiceItem Item { get; }
        public ServiceItemRow(ServiceItem item) => Item = item;

        public string Name => Item.ServiceName;
        public string CategoryName => Item.ServiceCategory?.CategoryName ?? "—";
        public string PriceText => $"{Item.UnitPrice:N0} đ";
        public bool IsAvailable => Item.IsAvailable;
        public string StatusText => Item.IsAvailable ? "Đang bán" : "Ngừng bán";
        public string ToggleText => Item.IsAvailable ? "Ngừng bán" : "Bán lại";
    }

    /// <summary>
    /// Tab Danh muc: quan ly nhom dich vu va cac mon trong nhom.
    /// Mon KHONG xoa duoc - chi ngung ban. Xoa se lam hong cac don da lap truoc do
    /// (dong don tro toi mon), nen chi tat di de khong goi moi duoc nua.
    /// </summary>
    public class ServiceCatalogViewModel : ViewModelBase
    {
        private readonly IServiceCatalogService _service = new ServiceCatalogService();
        private readonly Func<Task> _refreshAll;

        public ObservableCollection<ServiceCategory> Categories { get; } = [];
        public ObservableCollection<ServiceItemRow> Items { get; } = [];

        /// <summary>An nut them/sua/ngung ban voi vai tro khong duoc phep - service van la lop chan cuoi.</summary>
        public bool CanManage => AuthorizationPolicy.CanManageServiceCatalog;

        private ServiceItemRow? _selectedItem;
        public ServiceItemRow? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool IsEmpty => !IsLoading && !HasError && Items.Count == 0;

        public string TotalText
        {
            get
            {
                var selling = Items.Count(x => x.IsAvailable);
                return $"{Items.Count} món · {selling} đang bán · {Categories.Count} nhóm";
            }
        }

        public AsyncRelayCommand AddCategoryCommand { get; }
        public AsyncRelayCommand AddItemCommand { get; }
        public AsyncRelayCommand EditItemCommand { get; }
        public AsyncRelayCommand ToggleItemCommand { get; }
        public AsyncRelayCommand ReloadCommand { get; }

        public ServiceCatalogViewModel(Func<Task> refreshAll)
        {
            _refreshAll = refreshAll;
            AddCategoryCommand = new AsyncRelayCommand(_ => OpenCategoryAsync());
            AddItemCommand = new AsyncRelayCommand(_ => OpenItemAsync(null));
            EditItemCommand = new AsyncRelayCommand(EditItemAsync);
            ToggleItemCommand = new AsyncRelayCommand(ToggleAsync);
            ReloadCommand = new AsyncRelayCommand(_ => LoadAsync());
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var categories = await _service.GetCategoriesAsync();
                var items = await _service.GetItemsAsync();

                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                Items.Clear();
                foreach (var item in items.OrderBy(x => x.ServiceCategoryId).ThenBy(x => x.ServiceName))
                {
                    Items.Add(new ServiceItemRow(item));
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không tải được danh mục dịch vụ. Kiểm tra kết nối rồi thử lại.";
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        private async Task OpenCategoryAsync()
        {
            var viewModel = new ServiceCategoryDialogViewModel();
            var dialog = new ServiceCategoryDialog(viewModel) { Owner = Rooms.RoomMapViewModel.ActiveWindow() };
            if (dialog.ShowDialog() == true)
            {
                await _refreshAll();
            }
        }

        /// <summary>
        /// Sua mon: lay dong tu CommandParameter chu KHONG dung SelectedItem. Bam Button ben trong
        /// ListBoxItem thi ButtonBase dat e.Handled = true nen ListBoxItem khong duoc chon,
        /// SelectedItem con null va hop thoai se mo trong (bam Luu la tao moi thay vi sua).
        /// </summary>
        private Task EditItemAsync(object? parameter)
        {
            if (parameter is not ServiceItemRow row)
            {
                return Task.CompletedTask;
            }

            return OpenItemAsync(row.Item);
        }

        private async Task OpenItemAsync(ServiceItem? existing)
        {
            if (Categories.Count == 0)
            {
                Notify.Warning("Tạo nhóm dịch vụ trước rồi mới thêm món được.");
                return;
            }

            var viewModel = new ServiceItemDialogViewModel(existing, Categories);
            var dialog = new ServiceItemDialog(viewModel) { Owner = Rooms.RoomMapViewModel.ActiveWindow() };
            if (dialog.ShowDialog() == true)
            {
                await _refreshAll();
            }
        }

        /// <summary>Bat/tat mon. Tat thi hoi truoc vi le tan se khong goi mon do duoc nua.</summary>
        private async Task ToggleAsync(object? parameter)
        {
            if (parameter is not ServiceItemRow row)
            {
                return;
            }

            if (row.IsAvailable)
            {
                var ok = ConfirmDialog.Ask(
                    $"Ngừng bán {row.Name}?",
                    "Lễ tân sẽ không gọi được món này cho khách nữa.",
                    "Các đơn đã gọi trước đó vẫn giữ nguyên và vẫn tính tiền bình thường.",
                    "Ngừng bán");
                if (!ok)
                {
                    return;
                }
            }

            var item = row.Item;
            var result = await _service.SaveItemAsync(item.Id, item.ServiceCategoryId, item.ServiceName,
                item.UnitPrice, !item.IsAvailable);

            if (result.Ok)
            {
                Notify.Success(result.Message);
                await _refreshAll();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
    }
}
