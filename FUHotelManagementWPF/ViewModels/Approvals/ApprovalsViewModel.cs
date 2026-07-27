using System.Collections.ObjectModel;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Rooms;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Approvals;

public sealed record ApprovalTypeFilterOption(ApprovalRequestType? Value, string Text);

public sealed class ApprovalsViewModel : ViewModelBase
{
    private readonly IApprovalService _service = new ApprovalService();
    public ObservableCollection<ApprovalRow> Requests { get; } = [];

    public IReadOnlyList<ApprovalTypeFilterOption> TypeOptions { get; } =
    [
        new(null, "Tất cả loại"),
        new(ApprovalRequestType.ReservationCancel, "Huỷ đặt phòng"),
        new(ApprovalRequestType.InvoiceDiscount, "Giảm giá hoá đơn"),
        new(ApprovalRequestType.InvoiceCancel, "Huỷ hoá đơn"),
        new(ApprovalRequestType.PaymentVoid, "Huỷ giao dịch"),
        new(ApprovalRequestType.RoomMaintenance, "Bảo trì phòng")
    ];

    private ApprovalRow? _selectedRequest;
    public ApprovalRow? SelectedRequest
    {
        get => _selectedRequest;
        set
        {
            if (SetProperty(ref _selectedRequest, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(CanReviewSelection));
            }
        }
    }

    private ApprovalRequestStatus _selectedStatus = ApprovalRequestStatus.Pending;
    public ApprovalRequestStatus SelectedStatus
    {
        get => _selectedStatus;
        private set
        {
            if (!SetProperty(ref _selectedStatus, value)) return;
            SelectedRequest = null;
            OnPropertyChanged(nameof(IsPendingSelected));
            OnPropertyChanged(nameof(IsApprovedSelected));
            OnPropertyChanged(nameof(IsRejectedSelected));
            OnPropertyChanged(nameof(CanReviewSelection));
            _ = LoadAsync();
        }
    }

    public bool IsPendingSelected
    {
        get => SelectedStatus == ApprovalRequestStatus.Pending;
        set { if (value) SelectedStatus = ApprovalRequestStatus.Pending; }
    }
    public bool IsApprovedSelected
    {
        get => SelectedStatus == ApprovalRequestStatus.Approved;
        set { if (value) SelectedStatus = ApprovalRequestStatus.Approved; }
    }
    public bool IsRejectedSelected
    {
        get => SelectedStatus == ApprovalRequestStatus.Rejected;
        set { if (value) SelectedStatus = ApprovalRequestStatus.Rejected; }
    }

    private ApprovalTypeFilterOption _selectedType;
    public ApprovalTypeFilterOption SelectedType
    {
        get => _selectedType;
        set => SetProperty(ref _selectedType, value);
    }

    private bool _suppressRequesterAutoSearch;
    private string _requesterKeyword = string.Empty;
    public string RequesterKeyword
    {
        get => _requesterKeyword;
        set
        {
            if (!SetProperty(ref _requesterKeyword, value) || _suppressRequesterAutoSearch) return;
            // XAML dung Delay=350ms, nen chi truy van sau khi nguoi dung ngung go mot nhip.
            _ = LoadAsync();
        }
    }

    private DateTime? _fromDate;
    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    private DateTime? _toDate;
    public DateTime? ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public bool HasSelection => SelectedRequest != null;
    public bool CanReviewSelection => HasSelection && SelectedStatus == ApprovalRequestStatus.Pending;
    public bool IsEmpty => !_isLoading && Requests.Count == 0;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value)) OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand ApplyFilterCommand { get; }
    public AsyncRelayCommand ClearFilterCommand { get; }
    public AsyncRelayCommand ApproveCommand { get; }
    public AsyncRelayCommand RejectCommand { get; }

    public ApprovalsViewModel()
    {
        _selectedType = TypeOptions[0];
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
        ApplyFilterCommand = new AsyncRelayCommand(_ => LoadAsync());
        ClearFilterCommand = new AsyncRelayCommand(_ => ClearFilterAsync());
        ApproveCommand = new AsyncRelayCommand(_ => ReviewAsync(true));
        RejectCommand = new AsyncRelayCommand(_ => ReviewAsync(false));
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _service.SearchAsync(
                SelectedStatus,
                SelectedType.Value,
                string.IsNullOrWhiteSpace(RequesterKeyword) ? null : RequesterKeyword,
                FromDate,
                ToDate);
            Requests.Clear();
            SelectedRequest = null;
            if (!result.Ok || result.Data == null)
            {
                Notify.Error(result.Message);
                return;
            }
            foreach (var request in result.Data) Requests.Add(new ApprovalRow(request));
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch
        {
            Notify.Error("Không tải được danh sách phê duyệt.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearFilterAsync()
    {
        _suppressRequesterAutoSearch = true;
        try
        {
            SelectedType = TypeOptions[0];
            RequesterKeyword = string.Empty;
            FromDate = null;
            ToDate = null;
        }
        finally
        {
            _suppressRequesterAutoSearch = false;
        }
        await LoadAsync();
    }

    private async Task ReviewAsync(bool approve)
    {
        if (SelectedRequest == null || SelectedStatus != ApprovalRequestStatus.Pending) return;
        string? note;
        if (approve)
        {
            var confirmed = ConfirmDialog.Ask(
                $"Duyệt {SelectedRequest.TypeText.ToLowerInvariant()}?",
                $"Yêu cầu của {SelectedRequest.RequesterText} sẽ được thực hiện ngay.",
                SelectedRequest.Reason,
                "Duyệt");
            if (!confirmed) return;
            note = null;
        }
        else
        {
            note = ReasonDialog.Prompt("Lý do từ chối", RoomMapViewModel.ActiveWindow());
            if (note == null) return;
        }

        var result = await _service.ReviewAsync(SelectedRequest.Request.Id, approve, note);
        if (result.Ok)
        {
            Notify.Success(result.Message);
            await LoadAsync();
        }
        else
        {
            Notify.Error(result.Message);
            // Co the reviewer khac vua xu ly request. Tai lai de UI khong giu dong Pending cu.
            await LoadAsync();
        }
    }
}
