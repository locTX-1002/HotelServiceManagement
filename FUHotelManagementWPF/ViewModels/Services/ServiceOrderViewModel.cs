using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Services
{
    /// <summary>Mot lua chon phong dang co khach o, hien tren danh sach ben trai.</summary>
    public class StayOption
    {
        public Stay Stay { get; }
        public StayOption(Stay stay) => Stay = stay;

        public int Id => Stay.Id;
        public string RoomNumber => Stay.Reservation?.Room?.RoomNumber ?? "—";
        public string GuestName => Stay.Reservation?.Guest?.FullName ?? "—";
        public string SubText => $"{Stay.Reservation?.Room?.RoomType?.TypeName} · vào {Stay.ActualCheckIn:dd/MM}";
    }

    /// <summary>Mot o mon trong luoi thuc don - bam mot phat la vao gio.</summary>
    public class MenuTile
    {
        public ServiceItem Item { get; }
        public MenuTile(ServiceItem item) => Item = item;

        public string Name => Item.ServiceName;
        public string PriceText => $"{Item.UnitPrice:N0} đ";
    }

    /// <summary>Mot nhom mon (Nha hang, Giat la...) tren luoi thuc don.</summary>
    public class MenuGroup
    {
        public string Name { get; }
        public ObservableCollection<MenuTile> Tiles { get; } = [];
        public MenuGroup(string name) => Name = name;
    }

    /// <summary>Mot dong trong gio hang truoc khi bam Tao don.</summary>
    public class CartLine : ViewModelBase
    {
        public ServiceItem Item { get; }
        public CartLine(ServiceItem item) => Item = item;

        public string Name => Item.ServiceName;
        public string UnitPriceText => $"{Item.UnitPrice:N0} đ";

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value < 1 ? 1 : value))
                {
                    OnPropertyChanged(nameof(SubtotalText));
                    Changed?.Invoke();
                }
            }
        }

        public decimal Subtotal => Item.UnitPrice * Quantity;
        public string SubtotalText => $"{Subtotal:N0} đ";

        public event Action? Changed;
    }

    /// <summary>Mot don da tao, kem cac nut chuyen trang thai hop le.</summary>
    public class OrderRow
    {
        public ServiceOrder Order { get; }
        public OrderRow(ServiceOrder order) => Order = order;

        public string TimeText => $"{Order.OrderDate:dd/MM HH:mm}";
        public string TotalText => $"{Order.TotalAmount:N0} đ";
        public string ItemsText => string.Join(", ",
            Order.OrderDetails.Select(d => $"{d.ServiceItem?.ServiceName} x{d.Quantity}"));

        public string StatusText => Order.Status switch
        {
            ServiceOrderStatus.Pending or ServiceOrderStatus.Processing => "Chờ làm",
            ServiceOrderStatus.Completed => "Hoàn tất",
            _ => "Đã huỷ",
        };

        public string StatusKey => Order.Status.ToString();

        // Chi don Hoan tat moi duoc tinh tien vao hoa don; don con mo se CHAN tra phong.
        // Don dang cho van bam Hoan tat thang duoc - khong bat qua buoc "Dang lam".
        public bool ShowComplete => Order.Status is ServiceOrderStatus.Pending or ServiceOrderStatus.Processing;
        public bool ShowCancel => ShowComplete;
        public bool IsOpen => ShowComplete;
    }

    /// <summary>
    /// Tab Goi dich vu: chon phong dang co khach o -> them mon vao gio -> tao don,
    /// roi theo doi don qua cac trang thai Cho lam / Dang lam / Hoan tat.
    /// </summary>
    public class ServiceOrderViewModel : ViewModelBase
    {
        private readonly IServiceOrderService _orders = new ServiceOrderService();
        private readonly IServiceCatalogService _catalog = new ServiceCatalogService();
        private readonly IStayService _stays = new StayService();

        public ObservableCollection<StayOption> Stays { get; } = [];
        public ObservableCollection<MenuGroup> Menu { get; } = [];
        public ObservableCollection<CartLine> Cart { get; } = [];
        public ObservableCollection<OrderRow> Orders { get; } = [];

        // AuthorizationPolicy la internal cua tang Services nen man hinh khong goi duoc.
        // Chep lai dung dieu kien o day de AN nut; service van kiem doc lap - day chi la
        // lop cho do bam vao roi bi tu choi.
        public bool CanCreate => AppSession.RoleName is "Admin" or "Manager" or "Receptionist";
        public bool CanProcess => AppSession.RoleName is "Admin" or "Manager" or "ServiceStaff";

        private StayOption? _selectedStay;
        public StayOption? SelectedStay
        {
            get => _selectedStay;
            set
            {
                if (SetProperty(ref _selectedStay, value))
                {
                    OnPropertyChanged(nameof(HasSelectedStay));
                    OnPropertyChanged(nameof(SelectedStayTitle));
                    Cart.Clear();
                    RaiseCartState();
                    _ = LoadOrdersAsync();
                }
            }
        }

        public bool HasSelectedStay => SelectedStay != null;
        public string SelectedStayTitle => SelectedStay == null
            ? string.Empty
            : $"Phòng {SelectedStay.RoomNumber} · {SelectedStay.GuestName}";

        public decimal CartTotal => Cart.Sum(x => x.Subtotal);
        public string CartTotalText => $"{CartTotal:N0} đ";
        public bool CartIsEmpty => Cart.Count == 0;
        public bool CanSubmit => CanCreate && HasSelectedStay && Cart.Count > 0 && !IsBusy;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (SetProperty(ref _isBusy, value)) { OnPropertyChanged(nameof(CanSubmit)); } }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(NoStay)); } }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { if (SetProperty(ref _errorMessage, value)) { OnPropertyChanged(nameof(HasError)); } }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool NoStay => !IsLoading && Stays.Count == 0;
        public bool NoOrder => HasSelectedStay && Orders.Count == 0;
        public bool MenuIsEmpty => Menu.Count == 0;

        public RelayCommand AddTileCommand { get; }
        public RelayCommand RemoveFromCartCommand { get; }
        public AsyncRelayCommand SubmitCommand { get; }
        public AsyncRelayCommand CompleteCommand { get; }
        public AsyncRelayCommand CancelOrderCommand { get; }
        public AsyncRelayCommand ReloadCommand { get; }

        public ServiceOrderViewModel()
        {
            AddTileCommand = new RelayCommand(AddTile);
            RemoveFromCartCommand = new RelayCommand(RemoveFromCart);
            SubmitCommand = new AsyncRelayCommand(_ => SubmitAsync());
            CompleteCommand = new AsyncRelayCommand(p => ChangeStatusAsync(p, ServiceOrderStatus.Completed));
            CancelOrderCommand = new AsyncRelayCommand(CancelAsync);
            ReloadCommand = new AsyncRelayCommand(_ => LoadAsync());
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var keepId = SelectedStay?.Id;
                var stays = await _stays.GetActiveAsync();
                var items = await _catalog.GetItemsAsync(onlyAvailable: true);

                Stays.Clear();
                foreach (var stay in stays)
                {
                    Stays.Add(new StayOption(stay));
                }

                Menu.Clear();
                foreach (var group in items
                             .GroupBy(x => x.ServiceCategory?.CategoryName ?? "Khác")
                             .OrderBy(g => g.Key))
                {
                    var menuGroup = new MenuGroup(group.Key);
                    foreach (var item in group.OrderBy(x => x.ServiceName))
                    {
                        menuGroup.Tiles.Add(new MenuTile(item));
                    }
                    Menu.Add(menuGroup);
                }
                OnPropertyChanged(nameof(MenuIsEmpty));

                SelectedStay = Stays.FirstOrDefault(x => x.Id == keepId) ?? Stays.FirstOrDefault();
            }
            catch (Exception)
            {
                ErrorMessage = "Không tải được danh sách phòng và dịch vụ.";
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(NoStay));
            }
        }

        private async Task LoadOrdersAsync()
        {
            Orders.Clear();
            if (SelectedStay == null)
            {
                OnPropertyChanged(nameof(NoOrder));
                return;
            }

            try
            {
                var list = await _orders.GetByStayAsync(SelectedStay.Id);
                foreach (var order in list.OrderByDescending(x => x.OrderDate))
                {
                    Orders.Add(new OrderRow(order));
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không tải được đơn dịch vụ của phòng này.";
            }
            finally
            {
                OnPropertyChanged(nameof(NoOrder));
            }
        }

        /// <summary>Them mon vao gio. Mon da co thi cong don so luong thay vi tao dong moi.</summary>
        private void AddTile(object? parameter)
        {
            if (parameter is not MenuTile tile || !CanCreate)
            {
                return;
            }

            var existing = Cart.FirstOrDefault(x => x.Item.Id == tile.Item.Id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                var line = new CartLine(tile.Item);
                line.Changed += RaiseCartState;
                Cart.Add(line);
            }
            RaiseCartState();
        }

        private void RemoveFromCart(object? parameter)
        {
            if (parameter is CartLine line)
            {
                line.Changed -= RaiseCartState;
                Cart.Remove(line);
                RaiseCartState();
            }
        }

        private void RaiseCartState()
        {
            OnPropertyChanged(nameof(CartTotal));
            OnPropertyChanged(nameof(CartTotalText));
            OnPropertyChanged(nameof(CartIsEmpty));
            OnPropertyChanged(nameof(CanSubmit));
        }

        private async Task SubmitAsync()
        {
            if (SelectedStay == null || Cart.Count == 0)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = null;
            try
            {
                var lines = Cart.Select(x => new ServiceOrderLine(x.Item.Id, x.Quantity)).ToList();
                var result = await _orders.CreateAsync(SelectedStay.Id, lines);

                if (!result.Ok)
                {
                    ErrorMessage = result.Message;
                    return;
                }

                Notify.Success($"Đã tạo đơn {CartTotalText} cho phòng {SelectedStay.RoomNumber}.");
                Cart.Clear();
                RaiseCartState();
                await LoadOrdersAsync();
            }
            catch (Exception)
            {
                ErrorMessage = "Không tạo được đơn dịch vụ. Kiểm tra kết nối rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ChangeStatusAsync(object? parameter, ServiceOrderStatus status)
        {
            if (parameter is not OrderRow row)
            {
                return;
            }

            var result = await _orders.ChangeStatusAsync(row.Order.Id, status);
            if (result.Ok)
            {
                Notify.Success(result.Message);
                await LoadOrdersAsync();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }

        /// <summary>Huy don - hoi truoc vi huy roi khong khoi phuc duoc.</summary>
        private async Task CancelAsync(object? parameter)
        {
            if (parameter is not OrderRow row)
            {
                return;
            }

            var ok = ConfirmDialog.Ask(
                $"Huỷ đơn {row.TotalText}?",
                row.ItemsText,
                "Huỷ rồi không khôi phục được. Đơn đã huỷ không tính tiền vào hoá đơn.",
                "Huỷ đơn", isDanger: true);
            if (!ok)
            {
                return;
            }

            await ChangeStatusAsync(parameter, ServiceOrderStatus.Cancelled);
        }
    }
}
