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
            set => SetProperty(ref _guestKeyword, value);
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

        // --- Form tao khach moi ---
        // Truoc day ba o nay la auto-property tran: khong ban PropertyChanged, khong kiem gi
        // ca, va CCCD thi lay thang chuoi trong o TIM KIEM nhet vao. Ma o tim nhan "CCCD HOAC
        // so dien thoai", nen le tan tim bang SDT roi tao khach moi la so dien thoai bi luu
        // thanh CCCD, den luc bam Luu moi bao loi. Gio moi o mot property that, kiem ngay luc
        // go bang InputPolicy - dung bo quy tac ma tang service dang dung.

        private string _newFullName = string.Empty;
        public string NewFullName
        {
            get => _newFullName;
            set { if (SetProperty(ref _newFullName, value)) { ValidateFullName(); } }
        }

        private string _newIdentity = string.Empty;
        public string NewIdentity
        {
            get => _newIdentity;
            set { if (SetProperty(ref _newIdentity, value)) { ValidateIdentity(); } }
        }

        private string _newPhone = string.Empty;
        public string NewPhone
        {
            get => _newPhone;
            set { if (SetProperty(ref _newPhone, value)) { ValidatePhone(); } }
        }

        private string _newEmail = string.Empty;
        public string NewEmail
        {
            get => _newEmail;
            set { if (SetProperty(ref _newEmail, value)) { ValidateEmail(); } }
        }

        private void ValidateFullName()
        {
            ClearErrors(nameof(NewFullName));
            if (ShowNewGuestFields && string.IsNullOrWhiteSpace(NewFullName))
            {
                AddError(nameof(NewFullName), "Nhập họ tên khách.");
            }
        }

        private void ValidateIdentity()
        {
            ClearErrors(nameof(NewIdentity));
            var error = InputPolicy.ValidateIdentity(NewIdentity, required: ShowNewGuestFields);
            if (error != null) { AddError(nameof(NewIdentity), error); }
        }

        private void ValidatePhone()
        {
            ClearErrors(nameof(NewPhone));
            var error = InputPolicy.ValidatePhone(NewPhone, required: ShowNewGuestFields);
            if (error != null) { AddError(nameof(NewPhone), error); }
        }

        private void ValidateEmail()
        {
            ClearErrors(nameof(NewEmail));
            var error = InputPolicy.ValidateEmail(NewEmail, required: false);
            if (error != null) { AddError(nameof(NewEmail), error); }
        }

        private void ValidateNewGuestForm()
        {
            ValidateFullName(); ValidateIdentity(); ValidatePhone(); ValidateEmail();
        }

        // --- Ngày ---
        // Doi ngay la tu tim lai phong trong luon. Truoc day phai bam nut "Tim phong trong";
        // quen bam thi danh sach van la phong trong cua khoang ngay CU - chon nham mot phong
        // that ra da co nguoi, den luc bam Luu moi bi service tu choi.
        private DateTime _checkIn = DateTime.Today;
        public DateTime CheckIn
        {
            get => _checkIn;
            set
            {
                if (SetProperty(ref _checkIn, value))
                {
                    if (CheckOut.Date <= value.Date) { CheckOut = value.Date.AddDays(1); }
                    ValidateDates();
                    RefreshRooms();
                }
            }
        }

        private DateTime _checkOut = DateTime.Today.AddDays(1);
        public DateTime CheckOut
        {
            get => _checkOut;
            set
            {
                if (SetProperty(ref _checkOut, value))
                {
                    ValidateDates();
                    RefreshRooms();
                }
            }
        }

        /// <summary>Ngay som nhat cho chon tren lich - khong dat lui ve qua khu.</summary>
        public DateTime MinCheckIn { get; } = DateTime.Today;

        private void ValidateDates()
        {
            ClearErrors(nameof(CheckIn));
            ClearErrors(nameof(CheckOut));

            // Don da tao roi thi ngay nhan nam o qua khu la binh thuong, khong bat loi nguoc.
            if (!IsEdit && CheckIn.Date < DateTime.Today)
            {
                AddError(nameof(CheckIn), "Ngày nhận không được ở quá khứ.");
            }
            if (CheckOut.Date <= CheckIn.Date)
            {
                AddError(nameof(CheckOut), "Ngày trả phải sau ngày nhận ít nhất một đêm.");
            }
        }

        private bool HasDateError =>
            GetErrors(nameof(CheckIn)).Cast<object>().Any()
            || GetErrors(nameof(CheckOut)).Cast<object>().Any();

        /// <summary>Tim lai phong trong sau khi doi ngay; bo qua khi ngay dang sai.</summary>
        private void RefreshRooms()
        {
            if (!HasDateError) { _ = FindRoomsAsync(); }
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

        // Tien coc (tuy chon, chi khi TAO don - service khong nhan coc luc sua).
        // Nhap dang text roi parse de bao loi ro rang thay vi binding decimal im lang fail.
        public string DepositText { get; set; } = string.Empty;

        public record DepositMethodOption(PaymentMethod Method, string Label);

        public List<DepositMethodOption> DepositMethods { get; } =
        [
            new(PaymentMethod.Cash, "Tiền mặt"),
            new(PaymentMethod.BankTransfer, "Chuyển khoản"),
        ];

        private DepositMethodOption? _selectedDepositMethod;
        public DepositMethodOption? SelectedDepositMethod
        {
            get => _selectedDepositMethod;
            set => SetProperty(ref _selectedDepositMethod, value);
        }

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

            // Khong con nut "Tim phong trong" nen phai tu tim ngay luc mo. Doi ngay sau do
            // se tu tim lai qua setter cua CheckIn/CheckOut.
            _ = FindRoomsAsync();
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
                    // Dien san dung o theo HINH DANG chuoi vua go, khong doan bua: 12 so bat
                    // dau bang 0 la CCCD, 10 so bat dau bang 0 la so dien thoai. Khong khop
                    // dang nao thi de trong ca hai cho le tan tu dien.
                    if (InputPolicy.ValidateIdentity(key, required: true) == null) { NewIdentity = key; }
                    else if (InputPolicy.ValidatePhone(key, required: true) == null) { NewPhone = key; }
                    ValidateNewGuestForm();
                    Notify.Info("Chưa có khách này — điền thông tin bên dưới để tạo mới.");
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không tìm được khách. Kiểm tra kết nối SQL Server.";
            }
        }

        /// <summary>
        /// Phong muon chon san khi mo tu lich phong. Dat truoc roi goi PrefillRoomsAsync
        /// de dialog bat san danh sach phong trong va tro thang vao phong nay.
        /// </summary>
        public int? PreferredRoomId { get; set; }

        /// <summary>Tim san phong trong cho khoang ngay dang co, dung khi mo tu lich phong.</summary>
        public Task PrefillRoomsAsync() => FindRoomsAsync();

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
                var rooms = result.Data!;
                // Sua don: phong hien tai bi CHINH don nay chiem cho nen khong nam trong danh
                // sach phong trong. Khong them lai thi le tan mo ra la mat phong dang chon.
                if (IsEdit && _existing!.Room != null && rooms.All(r => r.Id != _existing.RoomId))
                {
                    rooms.Insert(0, _existing.Room);
                }
                AvailableRooms = rooms;
                // Mo tu lich phong thi tro thang vao phong da bam, khong thi lay phong dau
                // Giu phong dang chon neu doi ngay xong no van trong; het trong thi bo chon
                // han chu khong de nguyen cho le tan tuong van dat duoc.
                var keeping = SelectedRoom is { } current
                    ? AvailableRooms.FirstOrDefault(r => r.Id == current.Id)
                    : null;
                SelectedRoom = keeping
                               ?? (PreferredRoomId is { } wanted
                                   ? AvailableRooms.FirstOrDefault(r => r.Id == wanted)
                                   : null)
                               ?? AvailableRooms.FirstOrDefault();
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

                    ValidateNewGuestForm();
                    if (HasErrors)
                    {
                        ErrorMessage = FirstError();
                        return;
                    }

                    var created = await _guestService.CreateAsync(NewFullName, NewEmail, NewPhone,
                        NewIdentity.Trim(), BusinessObjects.Enums.GuestTag.None, null);
                    if (!created.Ok)
                    {
                        ErrorMessage = created.Message;
                        return;
                    }
                    MatchedGuest = created.Data;
                    guestId = created.Data!.Id;
                }
            }

            ValidateDates();
            if (HasDateError)
            {
                ErrorMessage = FirstError();
                return;
            }

            if (SelectedRoom == null)
            {
                ErrorMessage = "Chọn một phòng trống trong danh sách.";
                return;
            }

            IsBusy = true;
            try
            {
                ServiceResult result;
                if (IsEdit)
                {
                    // Doi khach cua don da tao khong con ho tro o tang service - chi doi phong, ngay, so khach
                    var updated = await _reservationService.UpdateAsync(_existing!.Id, SelectedRoom.Id,
                        NumberOfGuests, CheckIn, CheckOut, SpecialRequests);
                    result = updated.Ok
                        ? ServiceResult.Success(updated.Message)
                        : ServiceResult.Failure(updated.Message);
                }
                else
                {
                    // Tien coc tuy chon theo phan cong: bo trong = khong coc; co nhap thi phai la so hop le
                    decimal? deposit = null;
                    if (!string.IsNullOrWhiteSpace(DepositText))
                    {
                        if (!decimal.TryParse(DepositText.Trim(), out var parsed) || parsed < 0)
                        {
                            ErrorMessage = "Tiền cọc phải là số không âm (bỏ trống nếu không thu cọc).";
                            return;
                        }
                        if (parsed > 0 && SelectedDepositMethod == null)
                        {
                            ErrorMessage = "Chọn phương thức thanh toán cọc.";
                            return;
                        }
                        deposit = parsed > 0 ? parsed : null;
                    }

                    var created = await _reservationService.CreateAsync(guestId, SelectedRoom.Id,
                        NumberOfGuests, CheckIn, CheckOut, SpecialRequests,
                        deposit, deposit != null ? SelectedDepositMethod!.Method : null);
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
