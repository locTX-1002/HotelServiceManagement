using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Invoices;

public sealed class SurchargeEditDialogViewModel : ValidatableViewModelBase
{
    private readonly ISurchargeService _surchargeService = new SurchargeService();
    private readonly int _stayId;
    private readonly Surcharge? _existing;

    public event Action<bool>? RequestClose;

    public string Title => _existing == null ? "Thêm phụ thu" : "Sửa phụ thu";
    public bool IsEditing => _existing != null;
    public bool CanSelectItem => _existing == null;

    private List<SurchargeItem> _items = [];
    public List<SurchargeItem> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    private SurchargeItem? _selectedItem;
    public SurchargeItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private string _quantityText = "1";
    public string QuantityText
    {
        get => _quantityText;
        set => SetProperty(ref _quantityText, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand SaveCommand { get; }

    public SurchargeEditDialogViewModel(int stayId, Surcharge? existing)
    {
        _stayId = stayId;
        _existing = existing;
        if (existing != null)
        {
            _quantityText = existing.Quantity.ToString();
        }

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        _ = LoadItemsAsync();
    }

    private async Task LoadItemsAsync()
    {
        try
        {
            Items = (await _surchargeService.GetItemsAsync())
                .Where(x => x.IsActive || x.Id == _existing?.SurchargeItemId)
                .ToList();
            SelectedItem = _existing == null
                ? Items.FirstOrDefault()
                : Items.FirstOrDefault(x => x.Id == _existing.SurchargeItemId);
        }
        catch (Exception)
        {
            ErrorMessage = "Không tải được danh mục phụ thu.";
        }
    }

    private async Task SaveAsync(object? _)
    {
        ClearAllErrors();
        ErrorMessage = null;

        if (_existing == null && SelectedItem == null)
        {
            ErrorMessage = "Chưa chọn loại phụ thu.";
        }

        if (!int.TryParse(QuantityText, out var quantity) || quantity <= 0)
        {
            AddError(nameof(QuantityText), "Số lượng phải là số nguyên lớn hơn 0.");
        }

        if (HasErrors || ErrorMessage != null)
        {
            return;
        }

        try
        {
            var result = _existing == null
                ? await _surchargeService.AddToStayAsync(_stayId, SelectedItem!.Id, quantity)
                : await _surchargeService.UpdateAsync(_existing.Id, quantity);

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
            ErrorMessage = "Không lưu được phụ thu. Vui lòng kiểm tra kết nối rồi thử lại.";
        }
    }
}
