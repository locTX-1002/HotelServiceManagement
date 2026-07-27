using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.GuestPortal;

/// <summary>
/// Form tu dat phong cua khach. Khong nhan GuestId tu UI; ReservationService tu lay
/// CurrentGuestId trong session de tranh dat phong cho ho so cua nguoi khac.
/// </summary>
public sealed class GuestBookingDialogViewModel : ViewModelBase
{
    private readonly IReservationService _service = new ReservationService();
    private List<Room> _allAvailableRooms = [];
    private int _searchVersion;

    public event Action<bool>? RequestClose;

    public DateTime MinCheckIn { get; } = DateTime.Today;

    private DateTime _checkIn = DateTime.Today.AddDays(1);
    public DateTime CheckIn
    {
        get => _checkIn;
        set
        {
            if (!SetProperty(ref _checkIn, value)) return;
            if (CheckOut.Date <= value.Date) CheckOut = value.Date.AddDays(1);
            _ = LoadRoomsAsync();
        }
    }

    private DateTime _checkOut = DateTime.Today.AddDays(2);
    public DateTime CheckOut
    {
        get => _checkOut;
        set
        {
            if (!SetProperty(ref _checkOut, value)) return;
            _ = LoadRoomsAsync();
        }
    }

    private int _numberOfGuests = 1;
    public int NumberOfGuests
    {
        get => _numberOfGuests;
        set
        {
            if (!SetProperty(ref _numberOfGuests, Math.Max(1, value))) return;
            ApplyRoomFilter();
        }
    }

    private string _specialRequests = string.Empty;
    public string SpecialRequests
    {
        get => _specialRequests;
        set => SetProperty(ref _specialRequests, value);
    }

    public ObservableCollection<Room> AvailableRooms { get; } = [];

    private Room? _selectedRoom;
    public Room? SelectedRoom
    {
        get => _selectedRoom;
        set => SetProperty(ref _selectedRoom, value);
    }

    private bool _isLoadingRooms;
    public bool IsLoadingRooms
    {
        get => _isLoadingRooms;
        private set
        {
            if (SetProperty(ref _isLoadingRooms, value))
            {
                OnPropertyChanged(nameof(HasRooms));
                OnPropertyChanged(nameof(HasNoRooms));
            }
        }
    }

    public bool HasRooms => !IsLoadingRooms && AvailableRooms.Count > 0;
    public bool HasNoRooms => !IsLoadingRooms && AvailableRooms.Count == 0;

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand RefreshRoomsCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand PickNightsCommand { get; }

    public GuestBookingDialogViewModel()
    {
        RefreshRoomsCommand = new AsyncRelayCommand(_ => LoadRoomsAsync());
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
        PickNightsCommand = new RelayCommand(PickNights);
        _ = LoadRoomsAsync();
    }

    private void PickNights(object? parameter)
    {
        if (parameter is string text && int.TryParse(text, out var nights) && nights > 0)
            CheckOut = CheckIn.Date.AddDays(nights);
    }

    private async Task LoadRoomsAsync()
    {
        var version = ++_searchVersion;
        ErrorMessage = string.Empty;

        if (CheckIn.Date < DateTime.Today)
        {
            IsLoadingRooms = false;
            _allAvailableRooms = [];
            ApplyRoomFilter();
            ErrorMessage = "Ngày nhận phòng không được ở quá khứ.";
            return;
        }
        if (CheckOut.Date <= CheckIn.Date)
        {
            IsLoadingRooms = false;
            _allAvailableRooms = [];
            ApplyRoomFilter();
            ErrorMessage = "Ngày trả phòng phải sau ngày nhận phòng.";
            return;
        }

        IsLoadingRooms = true;
        try
        {
            var result = await _service.GetAvailableRoomsAsync(CheckIn, CheckOut);
            if (version != _searchVersion) return;

            if (!result.Ok || result.Data == null)
            {
                _allAvailableRooms = [];
                ApplyRoomFilter();
                ErrorMessage = result.Message;
                return;
            }

            _allAvailableRooms = result.Data
                .OrderBy(x => x.RoomType.BasePrice)
                .ThenBy(x => x.RoomNumber)
                .ToList();
            ApplyRoomFilter();
        }
        catch
        {
            if (version != _searchVersion) return;
            _allAvailableRooms = [];
            ApplyRoomFilter();
            ErrorMessage = "Không tải được phòng trống. Vui lòng thử lại.";
        }
        finally
        {
            if (version == _searchVersion) IsLoadingRooms = false;
        }
    }

    private void ApplyRoomFilter()
    {
        var previousId = SelectedRoom?.Id;
        var filtered = _allAvailableRooms
            .Where(x => x.RoomType.Capacity >= NumberOfGuests)
            .ToList();

        AvailableRooms.Clear();
        foreach (var room in filtered) AvailableRooms.Add(room);

        SelectedRoom = previousId.HasValue
            ? filtered.FirstOrDefault(x => x.Id == previousId.Value)
            : null;
        SelectedRoom ??= filtered.FirstOrDefault();

        OnPropertyChanged(nameof(HasRooms));
        OnPropertyChanged(nameof(HasNoRooms));
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        if (SelectedRoom == null)
        {
            ErrorMessage = "Vui lòng chọn một phòng còn trống.";
            return;
        }
        if (SpecialRequests.Trim().Length > 500)
        {
            ErrorMessage = "Yêu cầu đặc biệt tối đa 500 ký tự.";
            return;
        }

        var result = await _service.CreateForCurrentGuestAsync(
            SelectedRoom.Id,
            NumberOfGuests,
            CheckIn,
            CheckOut,
            SpecialRequests);

        if (!result.Ok)
        {
            ErrorMessage = result.Message;
            // Phong co the vua bi nguoi khac dat. Tai lai de guest khong tiep tuc chon room cu.
            if (result.Message.Contains("phòng", StringComparison.OrdinalIgnoreCase))
                await LoadRoomsAsync();
            return;
        }

        Notify.Success(result.Message);
        RequestClose?.Invoke(true);
    }
}
