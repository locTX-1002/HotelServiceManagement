using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Invoices;

public sealed class PaymentDialogViewModel : ValidatableViewModelBase
{
    private readonly IPaymentService _paymentService = new PaymentService();
    private readonly int _invoiceId;

    public event Action<bool>? RequestClose;

    public decimal RemainingAmount { get; }
    public string RemainingText => $"{RemainingAmount:N0} đ";

    private string _amountText;
    public string AmountText
    {
        get => _amountText;
        set => SetProperty(ref _amountText, value);
    }

    public IReadOnlyList<PaymentMethodOption> PaymentMethods { get; } =
    [
        new(PaymentMethod.Cash, "Tiền mặt"),
        new(PaymentMethod.BankTransfer, "Chuyển khoản"),
    ];

    private PaymentMethodOption _selectedMethod;
    public PaymentMethodOption SelectedMethod
    {
        get => _selectedMethod;
        set
        {
            if (SetProperty(ref _selectedMethod, value))
            {
                OnPropertyChanged(nameof(RequiresTransactionId));
                if (!RequiresTransactionId)
                {
                    TransactionId = string.Empty;
                }
            }
        }
    }

    public bool RequiresTransactionId => SelectedMethod.Method == PaymentMethod.BankTransfer;

    private string _transactionId = string.Empty;
    public string TransactionId
    {
        get => _transactionId;
        set => SetProperty(ref _transactionId, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand SaveCommand { get; }

    public PaymentDialogViewModel(int invoiceId, decimal remainingAmount)
    {
        _invoiceId = invoiceId;
        RemainingAmount = remainingAmount;
        _amountText = remainingAmount.ToString("0.##");
        _selectedMethod = PaymentMethods[0];
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    private async Task SaveAsync(object? _)
    {
        ClearAllErrors();
        ErrorMessage = null;

        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
        {
            AddError(nameof(AmountText), "Số tiền phải lớn hơn 0.");
        }
        else if (amount > RemainingAmount)
        {
            AddError(nameof(AmountText), "Số tiền không được vượt quá dư nợ.");
        }

        if (RequiresTransactionId && string.IsNullOrWhiteSpace(TransactionId))
        {
            AddError(nameof(TransactionId), "Chuyển khoản bắt buộc có mã giao dịch.");
        }
        else if (TransactionId.Trim().Length > 100)
        {
            AddError(nameof(TransactionId), "Mã giao dịch tối đa 100 ký tự.");
        }

        if (HasErrors)
        {
            return;
        }

        try
        {
            var result = await _paymentService.RecordAsync(
                _invoiceId,
                amount,
                SelectedMethod.Method,
                RequiresTransactionId ? TransactionId : null);

            if (!result.Ok)
            {
                ErrorMessage = result.Message;
                return;
            }

            Notify.Success(result.Message);
            RequestClose?.Invoke(true);
        }
        catch (Exception)
        {
            ErrorMessage = "Không ghi nhận được thanh toán. Vui lòng kiểm tra kết nối rồi thử lại.";
        }
    }
}
