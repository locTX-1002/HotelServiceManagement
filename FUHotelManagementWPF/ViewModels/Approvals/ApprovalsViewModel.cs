using System.Collections.ObjectModel;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Rooms;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Approvals;

public sealed class ApprovalsViewModel : ViewModelBase
{
    private readonly IApprovalService _service = new ApprovalService();
    public ObservableCollection<ApprovalRow> Requests { get; } = [];

    private ApprovalRow? _selectedRequest;
    public ApprovalRow? SelectedRequest
    {
        get => _selectedRequest;
        set
        {
            if (SetProperty(ref _selectedRequest, value))
                OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool HasSelection => SelectedRequest != null;
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
    public AsyncRelayCommand ApproveCommand { get; }
    public AsyncRelayCommand RejectCommand { get; }

    public ApprovalsViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
        ApproveCommand = new AsyncRelayCommand(_ => ReviewAsync(true));
        RejectCommand = new AsyncRelayCommand(_ => ReviewAsync(false));
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _service.GetPendingAsync();
            Requests.Clear();
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

    private async Task ReviewAsync(bool approve)
    {
        if (SelectedRequest == null) return;
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
        }
    }
}
