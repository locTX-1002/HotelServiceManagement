using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.GuestPortal;

public sealed class GuestServiceMenuItem
{
    public ServiceItem Item { get; }
    public GuestServiceMenuItem(ServiceItem item) => Item = item;
    public string Name => Item.ServiceName;
    public string PriceText => $"{Item.UnitPrice:N0} đ";
}

public sealed class GuestServiceMenuGroup
{
    public string Name { get; }
    public ObservableCollection<GuestServiceMenuItem> Items { get; } = [];
    public GuestServiceMenuGroup(string name) => Name = name;
    public string Heading => Name.ToUpperInvariant();
}

public sealed class GuestServiceCartLine : ViewModelBase
{
    public ServiceItem Item { get; }
    public GuestServiceCartLine(ServiceItem item) => Item = item;

    public string Name => Item.ServiceName;
    public string UnitPriceText => $"{Item.UnitPrice:N0} đ";

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set
        {
            var safe = Math.Max(1, value);
            if (SetProperty(ref _quantity, safe))
            {
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(SubtotalText));
                Changed?.Invoke();
            }
        }
    }

    public decimal Subtotal => Item.UnitPrice * Quantity;
    public string SubtotalText => $"{Subtotal:N0} đ";
    public event Action? Changed;
}

public sealed class GuestServiceOrderRow
{
    public ServiceOrder Order { get; }
    public GuestServiceOrderRow(ServiceOrder order) => Order = order;

    public string TimeText => Order.OrderDate.ToString("dd/MM/yyyy HH:mm");
    public string TotalText => $"{Order.TotalAmount:N0} đ";
    public string ItemsText => string.Join(", ",
        Order.OrderDetails.Select(x => $"{x.ServiceItem?.ServiceName ?? "Dịch vụ"} x{x.Quantity}"));

    public string StatusText => Order.Status switch
    {
        ServiceOrderStatus.Pending => "Chờ xử lý",
        ServiceOrderStatus.Processing => "Đang xử lý",
        ServiceOrderStatus.Completed => "Hoàn tất",
        _ => "Đã huỷ"
    };

    public string StatusKey => Order.Status.ToString();
}

/// <summary>
/// Guest Portal - goi dich vu cho chinh luot o dang Active cua guest dang dang nhap.
/// UI khong giu StayId de tranh guest tao don cho phong cua nguoi khac.
/// </summary>
public sealed class MyServicesViewModel : ViewModelBase
{
    private readonly IGuestServiceOrderService _orders = new GuestServiceOrderService();
    private readonly IServiceCatalogService _catalog = new ServiceCatalogService();

    public ObservableCollection<GuestServiceMenuGroup> Menu { get; } = [];
    public ObservableCollection<GuestServiceCartLine> Cart { get; } = [];
    public ObservableCollection<GuestServiceOrderRow> Orders { get; } = [];

    private Stay? _activeStay;
    public Stay? ActiveStay
    {
        get => _activeStay;
        private set
        {
            if (SetProperty(ref _activeStay, value))
            {
                OnPropertyChanged(nameof(HasActiveStay));
                OnPropertyChanged(nameof(RoomText));
                OnPropertyChanged(nameof(StayText));
                OnPropertyChanged(nameof(CanSubmit));
            }
        }
    }

    public bool HasActiveStay => ActiveStay != null;
    public string RoomText => ActiveStay == null
        ? "Chưa có phòng đang ở"
        : $"Phòng {ActiveStay.Reservation?.Room?.RoomNumber ?? "—"}";
    public string StayText => ActiveStay == null
        ? "Bạn cần nhận phòng trước khi gọi dịch vụ."
        : $"Đang lưu trú từ {ActiveStay.ActualCheckIn:dd/MM/yyyy}";

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                OnPropertyChanged(nameof(CanSubmit));
        }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool MenuIsEmpty => Menu.Count == 0;
    public bool CartIsEmpty => Cart.Count == 0;
    public bool NoOrders => Orders.Count == 0;
    public decimal CartTotal => Cart.Sum(x => x.Subtotal);
    public string CartTotalText => $"{CartTotal:N0} đ";
    public bool CanSubmit => HasActiveStay && Cart.Count > 0 && !IsBusy;

    public RelayCommand AddItemCommand { get; }
    public RelayCommand IncreaseCommand { get; }
    public RelayCommand DecreaseCommand { get; }
    public RelayCommand RemoveCommand { get; }
    public AsyncRelayCommand SubmitCommand { get; }
    public AsyncRelayCommand ReloadCommand { get; }

    public MyServicesViewModel()
    {
        AddItemCommand = new RelayCommand(AddItem);
        IncreaseCommand = new RelayCommand(p => ChangeQuantity(p, +1));
        DecreaseCommand = new RelayCommand(p => ChangeQuantity(p, -1));
        RemoveCommand = new RelayCommand(RemoveItem);
        SubmitCommand = new AsyncRelayCommand(_ => SubmitAsync());
        ReloadCommand = new AsyncRelayCommand(_ => LoadAsync());
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var stay = await _orders.GetCurrentActiveStayAsync();
            ActiveStay = stay.Ok ? stay.Data : null;

            Menu.Clear();
            Orders.Clear();
            Cart.Clear();
            RaiseCartState();

            if (!stay.Ok || ActiveStay == null)
            {
                ErrorMessage = stay.Message;
                RaiseCollectionState();
                return;
            }

            var items = await _catalog.GetItemsAsync(onlyAvailable: true);
            foreach (var group in items
                         .GroupBy(x => x.ServiceCategory?.CategoryName ?? "Khác")
                         .OrderBy(x => x.Key))
            {
                var menuGroup = new GuestServiceMenuGroup(group.Key);
                foreach (var item in group.OrderBy(x => x.ServiceName))
                    menuGroup.Items.Add(new GuestServiceMenuItem(item));
                Menu.Add(menuGroup);
            }

            var orders = await _orders.GetCurrentStayOrdersAsync();
            if (orders.Ok && orders.Data != null)
            {
                foreach (var order in orders.Data.OrderByDescending(x => x.OrderDate))
                    Orders.Add(new GuestServiceOrderRow(order));
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Không tải được dịch vụ phòng. Kiểm tra kết nối rồi thử lại.";
        }
        finally
        {
            IsLoading = false;
            RaiseCollectionState();
        }
    }

    private void AddItem(object? parameter)
    {
        if (parameter is not GuestServiceMenuItem tile || !HasActiveStay)
            return;

        var existing = Cart.FirstOrDefault(x => x.Item.Id == tile.Item.Id);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            var line = new GuestServiceCartLine(tile.Item);
            line.Changed += RaiseCartState;
            Cart.Add(line);
        }
        RaiseCartState();
    }

    private void ChangeQuantity(object? parameter, int delta)
    {
        if (parameter is not GuestServiceCartLine line)
            return;
        if (delta < 0 && line.Quantity == 1)
        {
            RemoveItem(line);
            return;
        }
        line.Quantity += delta;
    }

    private void RemoveItem(object? parameter)
    {
        if (parameter is not GuestServiceCartLine line)
            return;
        line.Changed -= RaiseCartState;
        Cart.Remove(line);
        RaiseCartState();
    }

    private async Task SubmitAsync()
    {
        if (!CanSubmit)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var lines = Cart.Select(x => new ServiceOrderLine(x.Item.Id, x.Quantity)).ToList();
            var result = await _orders.CreateAsync(lines);
            if (!result.Ok)
            {
                ErrorMessage = result.Message;
                return;
            }

            Notify.Success(result.Message);
            Cart.Clear();
            RaiseCartState();

            var currentOrders = await _orders.GetCurrentStayOrdersAsync();
            Orders.Clear();
            if (currentOrders.Ok && currentOrders.Data != null)
            {
                foreach (var order in currentOrders.Data.OrderByDescending(x => x.OrderDate))
                    Orders.Add(new GuestServiceOrderRow(order));
            }
            OnPropertyChanged(nameof(NoOrders));
        }
        catch (Exception)
        {
            ErrorMessage = "Không gửi được đơn dịch vụ. Kiểm tra kết nối rồi thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseCartState()
    {
        OnPropertyChanged(nameof(CartIsEmpty));
        OnPropertyChanged(nameof(CartTotal));
        OnPropertyChanged(nameof(CartTotalText));
        OnPropertyChanged(nameof(CanSubmit));
    }

    private void RaiseCollectionState()
    {
        OnPropertyChanged(nameof(MenuIsEmpty));
        OnPropertyChanged(nameof(NoOrders));
        RaiseCartState();
    }
}
