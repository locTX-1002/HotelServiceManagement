using System.Collections.ObjectModel;
using System.Windows;
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
                _ = LoadSelectedStayAsync();
                OnPropertyChanged(nameof(HasSelectedStay));
            }
        }
    }

    public bool HasSelectedStay => SelectedStay != null;

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
            }
        }
    }

    public bool HasInvoice => Invoice != null;

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

    public bool CanRecordPayment => Invoice != null
                                    && Invoice.Status != InvoiceStatus.Cancelled
                                    && RemainingAmount > 0;
    public bool CanEditSurcharges => SelectedStay != null && PaidAmount <= 0;
    public bool CanCancelInvoice => Invoice != null
                                    && Invoice.Status != InvoiceStatus.Cancelled
                                    && PaidAmount <= 0
                                    && AppSession.RoleName is "Admin" or "Manager";
    public bool CanVoidPayment => AppSession.RoleName is "Admin" or "Manager";

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
    public RelayCommand AddSurchargeCommand { get; }
    public RelayCommand EditSurchargeCommand { get; }
    public AsyncRelayCommand DeleteSurchargeCommand { get; }
    public RelayCommand RecordPaymentCommand { get; }
    public AsyncRelayCommand VoidPaymentCommand { get; }

    public InvoicesViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
        PrepareInvoiceCommand = new AsyncRelayCommand(PrepareInvoiceAsync);
        CancelInvoiceCommand = new AsyncRelayCommand(CancelInvoiceAsync);
        AddSurchargeCommand = new RelayCommand(_ => OpenSurchargeDialog(null));
        EditSurchargeCommand = new RelayCommand(x => OpenSurchargeDialog(x as Surcharge));
        DeleteSurchargeCommand = new AsyncRelayCommand(DeleteSurchargeAsync);
        RecordPaymentCommand = new RelayCommand(_ => OpenPaymentDialog());
        VoidPaymentCommand = new AsyncRelayCommand(VoidPaymentAsync);
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var selectedId = SelectedStay?.Id;
            var staysTask = _stayService.GetActiveAsync();
            var promotionsTask = _promotionService.GetAllAsync();
            await Task.WhenAll(staysTask, promotionsTask);

            ActiveStays.Clear();
            foreach (var stay in staysTask.Result)
            {
                ActiveStays.Add(stay);
            }

            var today = DateTime.Today;
            Promotions = promotionsTask.Result
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

    private async Task LoadSelectedStayAsync()
    {
        ErrorMessage = null;
        ClearSelectedData();
        if (SelectedStay == null)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var surchargeTask = _surchargeService.GetByStayAsync(SelectedStay.Id);
            var invoiceTask = _invoiceService.GetByStayAsync(SelectedStay.Id);
            await Task.WhenAll(surchargeTask, invoiceTask);

            foreach (var surcharge in surchargeTask.Result)
            {
                Surcharges.Add(surcharge);
            }

            Invoice = invoiceTask.Result;
            SelectedPromotion = Invoice?.PromotionCode == null
                ? null
                : Promotions.FirstOrDefault(x => x.Code == Invoice.PromotionCode);
            PromotionCode = Invoice?.PromotionCode ?? string.Empty;

            if (Invoice != null)
            {
                await LoadPaymentSummaryAsync();
            }
            RaiseInvoiceState();
        }
        catch (Exception)
        {
            ErrorMessage = "Không tải được chi tiết stay và hoá đơn.";
        }
        finally
        {
            IsLoading = false;
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
                string.IsNullOrWhiteSpace(PromotionCode) ? null : PromotionCode);
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

        var result = await _invoiceService.CancelAsync(Invoice.Id);
        if (!result.Ok)
        {
            Notify.Error(result.Message);
            return;
        }

        Notify.Success(result.Message);
        await LoadSelectedStayAsync();
    }

    private async void OpenSurchargeDialog(Surcharge? existing)
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

        var result = await _surchargeService.DeleteAsync(surcharge.Id);
        if (!result.Ok)
        {
            Notify.Error(result.Message);
            return;
        }

        Notify.Success(result.Message);
        await LoadSelectedStayAsync();
    }

    private async void OpenPaymentDialog()
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

        var result = await _paymentService.VoidAsync(payment.Id);
        if (!result.Ok)
        {
            Notify.Error(result.Message);
            return;
        }

        Notify.Success(result.Message);
        await LoadSelectedStayAsync();
    }

    private async Task LoadPaymentSummaryAsync()
    {
        Payments.Clear();
        PaidAmount = 0;
        RemainingAmount = Invoice?.TotalAmount ?? 0;
        if (Invoice == null)
        {
            return;
        }

        var result = await _paymentService.GetSummaryAsync(Invoice.Id);
        if (!result.Ok || result.Data == null)
        {
            ErrorMessage = result.Message;
            return;
        }

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
        RaiseInvoiceState();
    }

    private void RaiseInvoiceState()
    {
        OnPropertyChanged(nameof(HasInvoice));
        OnPropertyChanged(nameof(InvoiceStatusText));
        OnPropertyChanged(nameof(CanRecordPayment));
        OnPropertyChanged(nameof(CanEditSurcharges));
        OnPropertyChanged(nameof(CanCancelInvoice));
        OnPropertyChanged(nameof(CanVoidPayment));
    }

    private static Window? ActiveWindow()
        => Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
}
