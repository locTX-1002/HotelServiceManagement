using System.Collections.ObjectModel;
using System.Windows;
using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Invoices;

public sealed class InvoicesViewModel : ViewModelBase
{
    private readonly IStayService _stayService = new StayService();
    private readonly IInvoiceService _invoiceService = new InvoiceService();
    private readonly IPaymentService _paymentService = new PaymentService();
    private readonly ISurchargeService _surchargeService = new SurchargeService();
    private readonly IPromotionService _promotionService = new PromotionService();
    private int _selectedStayLoadVersion;

    public ObservableCollection<Stay> ActiveStays { get; } = [];
    public ObservableCollection<Surcharge> Surcharges { get; } = [];
    public ObservableCollection<Payment> Payments { get; } = [];

    private List<Promotion> _promotions = [];
    public List<Promotion> Promotions
    {
        get => _promotions;
        private set => SetProperty(ref _promotions, value);
    }

    private Stay? _selectedStay;
    public Stay? SelectedStay
    {
        get => _selectedStay;
        set
        {
            if (SetProperty(ref _selectedStay, value))
            {
                _ = LoadSelectedStayAsync(++_selectedStayLoadVersion);
                OnPropertyChanged(nameof(HasSelectedStay));
            }
        }
    }

    public bool HasSelectedStay => SelectedStay != null;
    public string InvoiceNumber => Invoice == null ? "CHƯA LẬP HOÁ ĐƠN" : $"HD-{Invoice.Id:000000}";
    public string SelectedRoomText => SelectedStay?.Reservation?.Room == null
        ? "Chưa chọn phòng"
        : $"Phòng {SelectedStay.Reservation.Room.RoomNumber}";
    public string SelectedGuestText => SelectedStay?.Reservation?.Guest?.FullName ?? "Chưa chọn khách";
    public string StayPeriodText
    {
        get
        {
            if (SelectedStay == null) return "—";
            var checkOut = SelectedStay.ActualCheckOut?.ToString("dd/MM/yyyy HH:mm")
                           ?? $"Dự kiến {SelectedStay.Reservation.CheckOutDate:dd/MM/yyyy}";
            return $"{SelectedStay.ActualCheckIn:dd/MM/yyyy HH:mm} → {checkOut}";
        }
    }
    public string InvoiceDateText => Invoice == null
        ? "Chưa lập"
        : Invoice.InvoiceDate.ToString("dd/MM/yyyy HH:mm");
    public string InvoiceCreatorText => Invoice?.CreatedByUser?.FullName
                                        ?? (Invoice == null ? "—" : "Không xác định");

    /// <summary>0 = Hoa don, 1 = Phu thu, 2 = Thanh toan.</summary>
    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    private Promotion? _selectedPromotion;
    public Promotion? SelectedPromotion
    {
        get => _selectedPromotion;
        set
        {
            if (SetProperty(ref _selectedPromotion, value) && value != null)
            {
                PromotionCode = value.Code;
            }
        }
    }

    private string _promotionCode = string.Empty;
    public string PromotionCode
    {
        get => _promotionCode;
        set => SetProperty(ref _promotionCode, value);
    }

    private decimal _manualDiscount;
    /// <summary>
    /// So tien giam quan ly go THANG vao, khong qua ma khuyen mai. Co o day vi khong
    /// phai luc nao cung kip tao ma: khach phan nan, quan ly bot cho ho mot khoan roi
    /// chot bill ngay tai quay. Service van la lop chan cuoi (kiem tra lai quyen).
    /// </summary>
    public decimal ManualDiscount
    {
        get => _manualDiscount;
        set => SetProperty(ref _manualDiscount, value);
    }

    /// <summary>Chi quan ly moi thay o "Giam tay" - dung chung luat voi service.</summary>
    public bool CanGiveManualDiscount => AuthorizationPolicy.CanGiveManualDiscount;

    private Invoice? _invoice;
    public Invoice? Invoice
    {
        get => _invoice;
        private set
        {
            if (SetProperty(ref _invoice, value))
            {
                OnPropertyChanged(nameof(HasInvoice));
                OnPropertyChanged(nameof(InvoiceStatusText));
                OnPropertyChanged(nameof(InvoiceNumber));
                OnPropertyChanged(nameof(InvoiceDateText));
                OnPropertyChanged(nameof(InvoiceCreatorText));
            }
        }
    }

    public bool HasInvoice => Invoice != null;

    // ---- Tam tinh theo du lieu HIEN TAI ------------------------------------------------
    // Hoa don luu trong DB la anh chup luc bam lap. Khach goi them dich vu hay bi ghi phu
    // thu sau do thi man hinh van hien so cu, le tan khong biet co chenh cho den khi bam
    // tinh lai. Tinh song song o day tu du lieu vua tai de bao ngay.

    private decimal LiveRoomCharge
    {
        get
        {
            if (SelectedStay?.Reservation?.Room?.RoomType == null) return 0;

            var nights = BillingRules.ChargeableNights(
                SelectedStay.ActualCheckIn, SelectedStay.Reservation.CheckOutDate,
                SelectedStay.ActualCheckOut ?? DateTime.Now);
            return nights * SelectedStay.Reservation.Room.RoomType.BasePrice;
        }
    }

    private decimal LiveServiceCharge => SelectedStay?.ServiceOrders
        .Where(o => o.Status == ServiceOrderStatus.Completed).Sum(o => o.TotalAmount) ?? 0;

    private decimal LiveSurcharge => Surcharges.Sum(x => x.Subtotal);

    /// <summary>Giam gia da chot tren hoa don; chua co hoa don thi chua biet, tinh 0.</summary>
    private decimal LiveDiscount => Math.Clamp(Invoice?.DiscountAmount ?? 0, 0,
        LiveRoomCharge + LiveServiceCharge + LiveSurcharge);

    public decimal LiveTotal => LiveRoomCharge + LiveServiceCharge + LiveSurcharge - LiveDiscount;
    public string LiveTotalText => $"{LiveTotal:N0} đ";

    /// <summary>Chenh giua tam tinh hien tai va so da luu tren hoa don.</summary>
    public decimal PendingDifference => Invoice == null ? 0 : LiveTotal - Invoice.TotalAmount;

    public bool HasPendingCharges => Invoice != null && PendingDifference != 0;

    public string PendingChargeText
    {
        get
        {
            var parts = new List<string>();
            var service = LiveServiceCharge - Invoice?.ServiceCharge ?? 0;
            var surcharge = LiveSurcharge - Invoice?.SurchargeAmount ?? 0;
            var room = LiveRoomCharge - Invoice?.RoomCharge ?? 0;
            if (service != 0) parts.Add($"dịch vụ {service:N0} đ");
            if (surcharge != 0) parts.Add($"phụ thu {surcharge:N0} đ");
            if (room != 0) parts.Add($"tiền phòng {room:N0} đ");
            var detail = parts.Count > 0 ? $" ({string.Join(", ", parts)})" : string.Empty;
            return $"Phát sinh {PendingDifference:N0} đ chưa vào hoá đơn{detail}. "
                   + $"Tổng mới sẽ là {LiveTotalText}.";
        }
    }

    private decimal _paidAmount;
    public decimal PaidAmount
    {
        get => _paidAmount;
        private set => SetProperty(ref _paidAmount, value);
    }

    private decimal _remainingAmount;
    public decimal RemainingAmount
    {
        get => _remainingAmount;
        private set
        {
            if (SetProperty(ref _remainingAmount, value))
            {
                OnPropertyChanged(nameof(CanRecordPayment));
            }
        }
    }

    private bool CanManageBilling => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager or RoleNames.Receptionist;
    public bool CanRecordPayment => CanManageBilling
                                    && Invoice != null
                                    && Invoice.Status != InvoiceStatus.Cancelled
                                    && RemainingAmount > 0;
    // Phu thu them duoc ca sau khi da thu tien - giong dich vu. Tinh lai hoa don se cong
    // vao va le tan thu not phan chenh; khong con ly do khoa o day.
    public bool CanEditSurcharges => CanManageBilling
                                     && SelectedStay != null
                                     && Invoice?.Status != InvoiceStatus.Cancelled;
    public bool CanCancelInvoice => Invoice != null
                                    && Invoice.Status != InvoiceStatus.Cancelled
                                    && PaidAmount <= 0
                                    && AppSession.RoleName is RoleNames.Admin or RoleNames.Manager;
    public bool CanVoidPayment => AppSession.RoleName is RoleNames.Admin or RoleNames.Manager;
    // Hoa don da thu tien van cho tinh lai - khach goi them dich vu sau khi lap hoa don
    // la chuyen binh thuong. Chi chan khi hoa don da huy.
    public bool CanPrepareInvoice => CanManageBilling && SelectedStay != null
                                     && Invoice?.Status != InvoiceStatus.Cancelled;

    /// <summary>Lap lan dau thi ghi "Lap hoa don", da co roi thi la "Tinh lai hoa don".</summary>
    public string PrepareInvoiceText => Invoice == null ? "Lập hoá đơn" : "Tính lại hoá đơn";

    public string InvoiceStatusText => Invoice?.Status switch
    {
        InvoiceStatus.Unpaid => "Chưa thanh toán",
        InvoiceStatus.PartiallyPaid => "Thanh toán một phần",
        InvoiceStatus.Paid => "Đã thanh toán",
        InvoiceStatus.Cancelled => "Đã huỷ",
        _ => "Chưa lập",
    };

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand PrepareInvoiceCommand { get; }
    public AsyncRelayCommand CancelInvoiceCommand { get; }
    public AsyncRelayCommand AddSurchargeCommand { get; }
    public AsyncRelayCommand EditSurchargeCommand { get; }
    public AsyncRelayCommand DeleteSurchargeCommand { get; }
    public AsyncRelayCommand RecordPaymentCommand { get; }
    public AsyncRelayCommand VoidPaymentCommand { get; }

    public InvoicesViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
        PrepareInvoiceCommand = new AsyncRelayCommand(PrepareInvoiceAsync);
        CancelInvoiceCommand = new AsyncRelayCommand(CancelInvoiceAsync);
        AddSurchargeCommand = new AsyncRelayCommand(_ => OpenSurchargeDialogAsync(null));
        EditSurchargeCommand = new AsyncRelayCommand(x => OpenSurchargeDialogAsync(x as Surcharge));
        DeleteSurchargeCommand = new AsyncRelayCommand(DeleteSurchargeAsync);
        RecordPaymentCommand = new AsyncRelayCommand(_ => OpenPaymentDialogAsync());
        VoidPaymentCommand = new AsyncRelayCommand(VoidPaymentAsync);
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            // Man khac co the ban giao san mot luot can xu ly (vi du check-out bi chan
            // vi chua thanh toan) - uu tien chon dung luot do.
            var selectedId = NavigationService.TakePendingStayId() ?? SelectedStay?.Id;
            // GetBillable thay cho GetActive: gom ca luot da tra phong ma con no tien,
            // truoc day nhung luot do bien mat khoi man nay nen khong con cho nao thu.
            var staysTask = _stayService.GetBillableAsync();
            var promotionsTask = _promotionService.GetAllAsync();
            await Task.WhenAll(staysTask, promotionsTask);
            var stays = await staysTask;
            var promotions = await promotionsTask;

            ActiveStays.Clear();
            foreach (var stay in stays)
            {
                ActiveStays.Add(stay);
            }

            var today = DateTime.Today;
            Promotions = promotions
                .Where(x => x.IsActive && today >= x.StartDate.Date && today <= x.EndDate.Date)
                .OrderBy(x => x.Code)
                .ToList();

            SelectedStay = ActiveStays.FirstOrDefault(x => x.Id == selectedId)
                           ?? ActiveStays.FirstOrDefault();
            if (SelectedStay == null)
            {
                ClearSelectedData();
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Không tải được dữ liệu hoá đơn. Vui lòng kiểm tra kết nối rồi thử lại.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadSelectedStayAsync(int? requestedVersion = null)
    {
        var loadVersion = requestedVersion ?? ++_selectedStayLoadVersion;
        var selectedStay = SelectedStay;
        ErrorMessage = null;
        ClearSelectedData();
        if (selectedStay == null)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var surchargeTask = _surchargeService.GetByStayAsync(selectedStay.Id);
            var invoiceTask = _invoiceService.GetByStayAsync(selectedStay.Id);
            await Task.WhenAll(surchargeTask, invoiceTask);
            var surcharges = await surchargeTask;
            var invoice = await invoiceTask;

            if (loadVersion != _selectedStayLoadVersion || SelectedStay?.Id != selectedStay.Id)
            {
                return;
            }

            foreach (var surcharge in surcharges)
            {
                Surcharges.Add(surcharge);
            }

            Invoice = invoice;
            SelectedPromotion = Invoice?.PromotionCode == null
                ? null
                : Promotions.FirstOrDefault(x => x.Code == Invoice.PromotionCode);
            PromotionCode = Invoice?.PromotionCode ?? string.Empty;

            if (Invoice != null)
            {
                await LoadPaymentSummaryAsync(loadVersion, Invoice);
            }
            RaiseInvoiceState();
        }
        catch (Exception)
        {
            if (loadVersion == _selectedStayLoadVersion)
            {
                ErrorMessage = "Không tải được chi tiết stay và hoá đơn.";
            }
        }
        finally
        {
            if (loadVersion == _selectedStayLoadVersion)
            {
                IsLoading = false;
            }
        }
    }

    private async Task PrepareInvoiceAsync(object? _)
    {
        if (SelectedStay == null)
        {
            Notify.Warning("Hãy chọn một stay trước khi lập hoá đơn.");
            return;
        }

        ErrorMessage = null;
        try
        {
            var result = await _invoiceService.PrepareAsync(
                SelectedStay.Id,
                string.IsNullOrWhiteSpace(PromotionCode) ? null : PromotionCode,
                manualDiscount: ManualDiscount);
            if (!result.Ok || result.Data == null)
            {
                ErrorMessage = result.Message;
                Notify.Error(result.Message);
                return;
            }

            Invoice = result.Data;
            Notify.Success(result.Message);
            await LoadPaymentSummaryAsync();
            RaiseInvoiceState();
        }
        catch (Exception)
        {
            ErrorMessage = "Không lập được hoá đơn. Vui lòng kiểm tra kết nối rồi thử lại.";
        }
    }

    private async Task CancelInvoiceAsync(object? _)
    {
        if (Invoice == null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "Huỷ hoá đơn hiện tại? Hoá đơn đã có thanh toán sẽ không thể huỷ.",
            "Xác nhận huỷ hoá đơn",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var result = await _invoiceService.CancelAsync(Invoice.Id);
            if (!result.Ok)
            {
                Notify.Error(result.Message);
                return;
            }

            Notify.Success(result.Message);
            await LoadSelectedStayAsync();
        }
        catch (Exception)
        {
            Notify.Error("Không huỷ được hoá đơn. Vui lòng kiểm tra kết nối rồi thử lại.");
        }
    }

    private async Task OpenSurchargeDialogAsync(Surcharge? existing)
    {
        if (SelectedStay == null)
        {
            Notify.Warning("Hãy chọn một stay trước.");
            return;
        }

        var viewModel = new SurchargeEditDialogViewModel(SelectedStay.Id, existing);
        var dialog = new SurchargeEditDialog(viewModel) { Owner = ActiveWindow() };
        if (dialog.ShowDialog() == true)
        {
            await LoadSelectedStayAsync();
        }
    }

    private async Task DeleteSurchargeAsync(object? parameter)
    {
        if (parameter is not Surcharge surcharge)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"Xoá phụ thu \"{surcharge.SurchargeItem?.Name}\"?",
            "Xác nhận xoá phụ thu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var result = await _surchargeService.DeleteAsync(surcharge.Id);
            if (!result.Ok)
            {
                Notify.Error(result.Message);
                return;
            }

            Notify.Success(result.Message);
            await LoadSelectedStayAsync();
        }
        catch (Exception)
        {
            Notify.Error("Không xoá được phụ thu. Vui lòng kiểm tra kết nối rồi thử lại.");
        }
    }

    private async Task OpenPaymentDialogAsync()
    {
        if (!CanRecordPayment || Invoice == null)
        {
            return;
        }

        var viewModel = new PaymentDialogViewModel(Invoice.Id, RemainingAmount);
        var dialog = new PaymentDialog(viewModel) { Owner = ActiveWindow() };
        if (dialog.ShowDialog() == true)
        {
            await LoadSelectedStayAsync();
        }
    }

    private async Task VoidPaymentAsync(object? parameter)
    {
        if (parameter is not Payment payment)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"Huỷ giao dịch {payment.Amount:N0} đ ngày {payment.PaymentDate:dd/MM/yyyy HH:mm}?",
            "Xác nhận huỷ giao dịch",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        if (payment.Status != PaymentStatus.Completed)
        {
            Notify.Warning("Chỉ có thể huỷ giao dịch đã hoàn tất.");
            return;
        }

        try
        {
            var result = await _paymentService.VoidAsync(payment.Id);
            if (!result.Ok)
            {
                Notify.Error(result.Message);
                return;
            }

            Notify.Success(result.Message);
            await LoadSelectedStayAsync();
        }
        catch (Exception)
        {
            Notify.Error("Không huỷ được giao dịch. Vui lòng kiểm tra kết nối rồi thử lại.");
        }
    }

    private async Task LoadPaymentSummaryAsync(int? selectedStayLoadVersion = null, Invoice? expectedInvoice = null)
    {
        var invoice = expectedInvoice ?? Invoice;
        if (invoice == null)
        {
            Payments.Clear();
            PaidAmount = 0;
            RemainingAmount = 0;
            return;
        }

        var result = await _paymentService.GetSummaryAsync(invoice.Id);
        if (selectedStayLoadVersion.HasValue
            && (selectedStayLoadVersion.Value != _selectedStayLoadVersion
                || SelectedStay?.Id != invoice.StayId))
        {
            return;
        }

        if (!result.Ok || result.Data == null)
        {
            ErrorMessage = result.Message;
            return;
        }

        Payments.Clear();
        Invoice = result.Data.Invoice;
        PaidAmount = result.Data.PaidAmount;
        RemainingAmount = result.Data.RemainingAmount;
        foreach (var payment in result.Data.Payments)
        {
            Payments.Add(payment);
        }
    }

    private void ClearSelectedData()
    {
        Surcharges.Clear();
        Payments.Clear();
        Invoice = null;
        PaidAmount = 0;
        RemainingAmount = 0;
        SelectedPromotion = null;
        PromotionCode = string.Empty;
        // Xoa luon so giam tay: doi sang khach khac ma con giu so cu la giam nham nguoi
        ManualDiscount = 0;
        RaiseInvoiceState();
    }

    private void RaiseInvoiceState()
    {
        OnPropertyChanged(nameof(HasInvoice));
        OnPropertyChanged(nameof(InvoiceNumber));
        OnPropertyChanged(nameof(SelectedRoomText));
        OnPropertyChanged(nameof(SelectedGuestText));
        OnPropertyChanged(nameof(StayPeriodText));
        OnPropertyChanged(nameof(InvoiceDateText));
        OnPropertyChanged(nameof(InvoiceCreatorText));
        OnPropertyChanged(nameof(InvoiceStatusText));
        OnPropertyChanged(nameof(CanRecordPayment));
        OnPropertyChanged(nameof(CanEditSurcharges));
        OnPropertyChanged(nameof(CanCancelInvoice));
        OnPropertyChanged(nameof(CanVoidPayment));
        OnPropertyChanged(nameof(CanPrepareInvoice));
        OnPropertyChanged(nameof(PrepareInvoiceText));
        OnPropertyChanged(nameof(LiveTotal));
        OnPropertyChanged(nameof(LiveTotalText));
        OnPropertyChanged(nameof(PendingDifference));
        OnPropertyChanged(nameof(HasPendingCharges));
        OnPropertyChanged(nameof(PendingChargeText));
    }

    private static Window? ActiveWindow()
        => Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
}
