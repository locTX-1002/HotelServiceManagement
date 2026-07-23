using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Reservations
{
    /// <summary>
    /// Dialog tạo/sửa đặt phòng. Luồng: tìm/tạo khách theo CCCD-SĐT → chọn ngày → tìm phòng trống
    /// → chọn phòng → nhập số khách/ghi chú → lưu. Rule kiểm ở service, đây chỉ điều phối UI.
    /// </summary>
    public class CreateReservationDialogViewModel : ValidatableViewModelBase
    {
        private readonly IReservationService _reservationService = new ReservationService();
        private readonly IGuestService _guestService = new GuestService();
        private readonly Reservation? _existing;

        public event Action<bool>? RequestClose;

        public bool IsEdit => _existing != null;
        /// <summary>Khi sửa: giữ nguyên khách, không cho đổi (khoá ô tìm khách).</summary>
        public bool CanEditGuest => !IsEdit;
        public string Title => IsEdit ? $"Sửa đặt phòng {_existing!.BookingCode}" : "Tạo đặt phòng mới";
        public string Subtitle => IsEdit
            ? "Chỉ sửa được khi đặt phòng còn Chờ / Đã xác nhận."
            : "Tìm khách, chọn ngày và phòng trống rồi lưu.";
        public string HeaderIcon => IsEdit ? "" : "";

        // --- Khách ---
        private string _guestKeyword = string.Empty;
        public string GuestKeyword
        {
            get => _guestKeyword;
            set
            {
                if (SetProperty(ref _guestKeyword, value))
                {
                    OnPropertyChanged(nameof(IdentityHint));
                }
            }
        }

        private Guest? _matchedGuest;
        public Guest? MatchedGuest
        {
            get => _matchedGuest;
            private set
            {
                if (SetProperty(ref _matchedGuest, value))
                {
                    OnPropertyChanged(nameof(GuestInfoText));
                    OnPropertyChanged(nameof(HasMatchedGuest));
                    OnPropertyChanged(nameof(ShowNewGuestFields));
                    OnPropertyChanged(nameof(IsBlacklisted));
                }
            }
        }

        public bool HasMatchedGuest => _matchedGuest != null;
        public bool IsBlacklisted => _matchedGuest?.Tag == GuestTag.Blacklisted;
        public string GuestInfoText => _matchedGuest == null
            ? string.Empty
            : $"{_matchedGuest.FullName} · {_matchedGuest.PhoneNumber}"
              + (_matchedGuest.Tag == GuestTag.Vip ? "  (VIP)"
                 : _matchedGuest.Tag == GuestTag.Blacklisted ? "  (Cảnh báo)" : string.Empty);

        private bool _guestSearched;
        /// <summary>Đã bấm tìm mà không thấy → hiện form tạo khách mới.</summary>
        public bool ShowNewGuestFields => _guestSearched && _matchedGuest == null;

        /// <summary>Noi ro chuoi vua go se duoc luu vao dau, khoi le tan phai doan.</summary>
        public string IdentityHint
        {
            get
            {
                var key = GuestKeyword.Trim();
                if (string.IsNullOrEmpty(key))
                {
                    return string.Empty;
                }
                return $"CCCD/CMND lưu theo ô tìm ở trên: {key}";
            }
        }

        public string NewFullName { get; set; } = string.Empty;
        public string NewPhone { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;

        // --- Ngày ---
        private DateTime _checkIn = DateTime.Today;
        public DateTime CheckIn
        {
            get => _checkIn;
            set => SetProperty(ref _checkIn, value);
        }

        private DateTime _checkOut = DateTime.Today.AddDays(1);
        public DateTime CheckOut
        {
            get => _checkOut;
            set => SetProperty(ref _checkOut, value);
        }

        // --- Phòng trống ---
        private List<Room> _availableRooms = [];
        public List<Room> AvailableRooms
        {
            get => _availableRooms;
            private set
            {
                if (SetProperty(ref _availableRooms, value))
                {
                    OnPropertyChanged(nameof(HasSearchedRooms));
                }
            }
        }

        private bool _roomsSearched;
        public bool HasSearchedRooms => _roomsSearched;

        private Room? _selectedRoom;
        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (SetProperty(ref _selectedRoom, value) && value != null && NumberOfGuests > value.RoomType.Capacity)
                {
                    NumberOfGuests = value.RoomType.Capacity;
                }
            }
        }

        private int _numberOfGuests = 1;
        public int NumberOfGuests
        {
            get => _numberOfGuests;
            set => SetProperty(ref _numberOfGuests, value);
        }

        public List<ReservationStatusFilter> StatusOptions { get; } =
        [
            new("Chờ xác nhận", ReservationStatus.Pending),
            new("Đã xác nhận", ReservationStatus.Confirmed),
        ];

        private ReservationStatusFilter _selectedStatus;
        public ReservationStatusFilter SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        /// <summary>Chon nhanh so dem: dat ngay tra = ngay nhan + n.</summary>
        public RelayCommand PickNightsCommand { get; }

        private void PickNights(object? parameter)
        {
            if (parameter is string text && int.TryParse(text, out var nights) && nights > 0)
            {
                CheckOut = CheckIn.Date.AddDays(nights);
            }
        }

        public string SpecialRequests { get; set; } = string.Empty;

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public AsyncRelayCommand FindGuestCommand { get; }
        public AsyncRelayCommand FindRoomsCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }

        public CreateReservationDialogViewModel(Reservation? existing)
        {
            _existing = existing;
            // Don tao tai quay thi chinh le tan la nguoi xac nhan -> vao thang Da xac nhan,
            // khach den la co trong danh sach check-in ngay. Muon ha xuong Cho xac nhan
            // thi doi o man Dat phong.
            _selectedStatus = StatusOptions[1];

            PickNightsCommand = new RelayCommand(PickNights);
            FindGuestCommand = new AsyncRelayCommand(_ => FindGuestAsync());
            FindRoomsCommand = new AsyncRelayCommand(_ => FindRoomsAsync());
            SaveCommand = new AsyncRelayCommand(SaveAsync, _ => !IsBusy);

            if (existing != null)
            {
                _guestKeyword = existing.Guest?.IdentityNumber ?? existing.Guest?.PhoneNumber ?? string.Empty;
                _matchedGuest = existing.Guest;
                _guestSearched = true;
                _checkIn = existing.CheckInDate;
                _checkOut = existing.CheckOutDate;
                _numberOfGuests = existing.NumberOfGuests;
                _selectedStatus = StatusOptions.FirstOrDefault(o => o.Status == existing.Status) ?? StatusOptions[0];
                SpecialRequests = existing.SpecialRequests ?? string.Empty;
                // phòng hiện tại đưa vào danh sách để chọn sẵn
                if (existing.Room != null)
                {
                    _availableRooms = [existing.Room];
                    _selectedRoom = existing.Room;
                    _roomsSearched = true;
                }
            }
        }

        private async Task FindGuestAsync()
        {
            ErrorMessage = null;
            _guestSearched = true;
            var key = GuestKeyword.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MatchedGuest = null;
                OnPropertyChanged(nameof(ShowNewGuestFields));
                ErrorMessage = "Nhập CCCD hoặc số điện thoại để tìm khách.";
                return;
            }
            try
            {
                MatchedGuest = await _guestService.FindExactAsync(key);
                // MatchedGuest tu null sang null thi setter khong ban thong bao, phai bao tay -
                // khong co dong nay thi form khach moi khong bao gio hien ra.
                OnPropertyChanged(nameof(ShowNewGuestFields));
                if (MatchedGuest == null)
                {
                    // gợi ý điền sẵn: nếu keyword toàn số coi như SĐT, ngược lại CCCD
                    if (key.All(char.IsDigit) && key.Length >= 9)
                    {
                        NewPhone = key;
                    }
                    OnPropertyChanged(nameof(NewPhone));
                    Notify.Info("Chưa có khách này — điền thông tin bên dưới để tạo mới.");
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không tìm được khách. Kiểm tra kết nối SQL Server.";
            }
        }

        private async Task FindRoomsAsync()
        {
            ErrorMessage = null;
            _roomsSearched = true;
            try
            {
                var result = await _reservationService.GetAvailableRoomsAsync(CheckIn, CheckOut);
                if (!result.Ok)
                {
                    ErrorMessage = result.Message;
                    AvailableRooms = [];
                    return;
                }
                AvailableRooms = result.Data!;
                SelectedRoom = AvailableRooms.FirstOrDefault();
                if (AvailableRooms.Count == 0)
                {
                    Notify.Warning("Không còn phòng trống trong khoảng ngày này.");
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không tải được phòng trống.";
            }
        }

        private async Task SaveAsync(object? _)
        {
            ErrorMessage = null;

            // 1. Bảo đảm có khách: khớp sẵn, hoặc tạo mới từ form
            var guestId = MatchedGuest?.Id ?? 0;
            if (guestId == 0)
            {
                // Le tan go CCCD roi bam Luu luon ma quen bam Tim - tu tra giup thay vi bao loi
                if (!_guestSearched && !string.IsNullOrWhiteSpace(GuestKeyword))
                {
                    await FindGuestAsync();
                    guestId = MatchedGuest?.Id ?? 0;
                }

                if (guestId == 0)
                {
                    if (!ShowNewGuestFields)
                    {
                        ErrorMessage = "Nhập CCCD hoặc số điện thoại của khách trước.";
                        return;
                    }

                    var created = await _guestService.CreateAsync(NewFullName, NewPhone, GuestKeyword.Trim(), NewEmail);
                    if (!created.Ok)
                    {
                        ErrorMessage = created.Message;
                        return;
                    }
                    MatchedGuest = created.Data;
                    guestId = created.Data!.Id;
                }
            }

            if (SelectedRoom == null)
            {
                ErrorMessage = "Bấm Tìm phòng trống rồi chọn một phòng.";
                return;
            }

            IsBusy = true;
            try
            {
                ServiceResult result;
                if (IsEdit)
                {
                    result = await _reservationService.UpdateAsync(_existing!.Id, guestId, SelectedRoom.Id,
                        NumberOfGuests, CheckIn, CheckOut, SelectedStatus.Status!.Value, SpecialRequests);
                }
                else
                {
                    var created = await _reservationService.CreateAsync(guestId, SelectedRoom.Id, NumberOfGuests,
                        CheckIn, CheckOut, SelectedStatus.Status!.Value, SpecialRequests, AppSession.CurrentUser?.Id);
                    result = created.Ok ? ServiceResult.Success(created.Message) : ServiceResult.Failure(created.Message);
                }

                if (result.Ok)
                {
                    Notify.Success(result.Message);
                    RequestClose?.Invoke(true);
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không lưu được. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
