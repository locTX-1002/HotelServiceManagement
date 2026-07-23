using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Rooms;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.CheckInOut
{
    public enum FlowKind
    {
        Arrival,
        Stay,
    }

    /// <summary>
    /// Một việc trên dòng thời gian của lễ tân: hoặc khách sắp đến (cần check-in),
    /// hoặc khách đang ở (cần check-out). Gộp chung để quét một mạch theo độ ưu tiên.
    /// </summary>
    public class FlowItem
    {
        public FlowKind Kind { get; }
        public Reservation Reservation { get; }
        public Stay? Stay { get; }

        public string Thumbnail => RoomImages.Thumbnail(
            Reservation.Room?.RoomTypeId ?? 0, Reservation.Room?.RoomType?.TypeName ?? string.Empty);
        public string RoomNumber => Reservation.Room?.RoomNumber ?? string.Empty;
        public string GuestName => Reservation.Guest?.FullName ?? string.Empty;
        public string ActionLabel => Kind == FlowKind.Arrival ? "Check-in" : "Check-out";

        /// <summary>Quá hạn: khách chưa đến dù đã qua ngày nhận, hoặc chưa trả dù đã qua ngày trả.</summary>
        public bool IsOverdue => Kind == FlowKind.Arrival
            ? Reservation.CheckInDate.Date < DateTime.Today
            : DateTime.Today > Reservation.CheckOutDate.Date;

        public bool IsToday => Kind == FlowKind.Arrival
            ? Reservation.CheckInDate.Date == DateTime.Today
            : Reservation.CheckOutDate.Date == DateTime.Today;

        public string StatusText
        {
            get
            {
                if (Kind == FlowKind.Arrival)
                {
                    var days = (Reservation.CheckInDate.Date - DateTime.Today).Days;
                    return days switch
                    {
                        < 0 => $"Quá hạn đến {-days} ngày",
                        0 => "Đến hôm nay",
                        _ => $"Đến sau {days} ngày",
                    };
                }
                var left = (Reservation.CheckOutDate.Date - DateTime.Today).Days;
                return left switch
                {
                    < 0 => $"Quá hạn trả {-left} ngày",
                    0 => "Trả hôm nay",
                    _ => $"Còn {left} đêm",
                };
            }
        }

        public string SubText => Kind == FlowKind.Arrival
            ? $"{Reservation.Room?.RoomType?.TypeName} · {Reservation.NumberOfGuests} khách · {Reservation.CheckInDate:dd/MM} → {Reservation.CheckOutDate:dd/MM}"
            : $"{Reservation.Room?.RoomType?.TypeName} · vào {Stay!.ActualCheckIn:dd/MM HH:mm} · {Nights} đêm";

        public int Nights => Stay == null ? 0 : Math.Max(1, (DateTime.Today - Stay.ActualCheckIn.Date).Days);

        // ---- Cho bang lam viec ben phai ----
        public string TypeName => Reservation.Room?.RoomType?.TypeName ?? string.Empty;
        public string GuestPhone => Reservation.Guest?.PhoneNumber ?? "—";
        public string BookingCode => Reservation.BookingCode;
        public string GuestCountText => $"{Reservation.NumberOfGuests} khách";
        public string HeaderText => $"Phòng {RoomNumber} · {TypeName}";
        public string? SpecialRequests => Reservation.SpecialRequests;
        public bool HasSpecialRequests => !string.IsNullOrWhiteSpace(Reservation.SpecialRequests);

        public string CheckInLabel => Kind == FlowKind.Arrival ? "NGÀY NHẬN" : "VÀO LÚC";
        public string CheckInValue => Kind == FlowKind.Arrival
            ? Reservation.CheckInDate.ToString("dd/MM/yyyy")
            : Stay!.ActualCheckIn.ToString("dd/MM/yyyy HH:mm");

        public string PlannedOutText => Reservation.CheckOutDate.ToString("dd/MM/yyyy");

        /// <summary>
        /// So dem tinh tien. Khach chua den thi theo don. Khach dang o thi tinh tu luc vao
        /// den HOM NAY neu da qua ngay tra - o qua han la phai tra tien nhung dem o them,
        /// giong cach ban web tinh theo ngay tra thuc te.
        /// </summary>
        public int ChargeableNights
        {
            get
            {
                if (Kind == FlowKind.Arrival)
                {
                    return Math.Max(1, (Reservation.CheckOutDate.Date - Reservation.CheckInDate.Date).Days);
                }
                var until = Reservation.CheckOutDate.Date > DateTime.Today
                    ? Reservation.CheckOutDate.Date
                    : DateTime.Today;
                return Math.Max(1, (until - Stay!.ActualCheckIn.Date).Days);
            }
        }

        /// <summary>So dem o qua so voi don - hien rieng de le tan giai thich duoc voi khach.</summary>
        public int ExtraNights => Kind == FlowKind.Stay && IsOverdue
            ? (DateTime.Today - Reservation.CheckOutDate.Date).Days
            : 0;

        public bool HasExtraNights => ExtraNights > 0;
        public string ExtraNightsText => $"Trong đó {ExtraNights} đêm quá hạn so với đơn";

        public decimal BasePrice => Reservation.Room?.RoomType?.BasePrice ?? 0;
        public decimal RoomCharge => ChargeableNights * BasePrice;

        /// <summary>Tong phu thu da ghi cho luot nay - ViewModel gan sau khi tai.</summary>
        public decimal SurchargeTotal { get; set; }

        public bool HasSurcharge => SurchargeTotal > 0;
        public decimal TotalCharge => RoomCharge + SurchargeTotal;

        public string NightsText => $"{ChargeableNights} đêm × {BasePrice:N0} đ";
        public string RoomChargeText => $"{RoomCharge:N0} đ";
        public string SurchargeText => $"{SurchargeTotal:N0} đ";
        public string TotalChargeText => $"{TotalCharge:N0} đ";
        public string EstimateLabel => Kind == FlowKind.Arrival ? "Dự kiến cả kỳ" : "Tạm tính";

        /// <summary>Chi khach dang o moi gia han va ghi phu thu duoc.</summary>
        public bool CanExtend => Kind == FlowKind.Stay;

        // ---- Nut phu tren dong, chi hien voi viec qua han ----
        // Khach qua han den thi le tan con phai danh dau khong den hoac huy don,
        // truoc day phai roi man nay sang Dat phong tim lai don moi lam duoc.
        public bool ShowNoShow => Kind == FlowKind.Arrival && IsOverdue;
        public bool ShowCancel => Kind == FlowKind.Arrival && IsOverdue;

        /// <summary>Khach qua han tra thi gia han la viec hay lam nhat - dua thang len dong.</summary>
        public bool ShowExtendOnRow => Kind == FlowKind.Stay && IsOverdue;

        /// <summary>Thứ tự ưu tiên: quá hạn → việc hôm nay → còn lại (theo ngày gần nhất).</summary>
        public int SortRank => IsOverdue ? 0 : IsToday ? 1 : 2;
        public DateTime SortDate => Kind == FlowKind.Arrival ? Reservation.CheckInDate : Reservation.CheckOutDate;

        public FlowItem(Reservation reservation)
        {
            Kind = FlowKind.Arrival;
            Reservation = reservation;
        }

        public FlowItem(Stay stay)
        {
            Kind = FlowKind.Stay;
            Stay = stay;
            Reservation = stay.Reservation;
        }
    }

    /// <summary>Chip loc. IsSelected de chip dang chon to mau, giong chip o cac man khac.</summary>
    public class FlowFilter : ViewModelBase
    {
        public string Label { get; }
        public Func<FlowItem, bool>? Predicate { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private int _count;
        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        /// <summary>Chip "Qua han" to chu do de bat mat ngay ca khi chua chon.</summary>
        public bool IsUrgent { get; init; }

        public FlowFilter(string label, Func<FlowItem, bool>? predicate)
        {
            Label = label;
            Predicate = predicate;
        }
    }

    /// <summary>Module Check-in / Check-out: một dòng thời gian việc cần làm + chip lọc.</summary>
    public class CheckInOutViewModel : ViewModelBase
    {
        private readonly IStayService _service = new StayService();

        public ObservableCollection<FlowItem> Items { get; } = [];
        public ICollectionView ItemsView { get; }

        public List<FlowFilter> Filters { get; }

        private FlowFilter _selectedFilter;
        public FlowFilter SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    foreach (var f in Filters)
                    {
                        f.IsSelected = ReferenceEquals(f, value);
                    }
                    ItemsView.Refresh();
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        // Bang lam viec ben phai: chon mot viec de xem du thong tin truoc khi bam
        private FlowItem? _selectedItem;
        public FlowItem? SelectedItem
        {
            get => _selectedItem;
            set { if (SetProperty(ref _selectedItem, value)) { OnPropertyChanged(nameof(HasSelection)); } }
        }

        public bool HasSelection => _selectedItem != null;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        public bool IsEmpty => !IsLoading && ItemsView.Cast<object>().Any() == false;

        public string TodayText => $"Hôm nay {DateTime.Today:dd/MM/yyyy}";

        public AsyncRelayCommand ActionCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }
        public RelayCommand PickFilterCommand { get; }
        public AsyncRelayCommand ExtendCommand { get; }
        public AsyncRelayCommand SurchargeCommand { get; }
        public AsyncRelayCommand NoShowCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        private readonly ISurchargeService _surchargeService = new SurchargeService();
        private readonly IReservationService _reservationService = new ReservationService();

        /// <summary>Khach qua han den ma khong toi: giai phong phong de con ban cho nguoi khac.</summary>
        private async Task MarkNoShowAsync(object? parameter)
        {
            if (parameter is not FlowItem { Kind: FlowKind.Arrival } item)
            {
                return;
            }
            var ok = ConfirmDialog.Ask(
                $"Ghi nhận {item.GuestName} không đến?",
                $"Phòng {item.RoomNumber} sẽ được trả về trạng thái trống để bán cho khách khác.",
                "Tiền cọc nếu có sẽ không tự hoàn lại — phần đó xử lý ngoài hệ thống.",
                "Ghi không đến", isDanger: true);
            if (!ok)
            {
                return;
            }

            var result = await _reservationService.NoShowAsync(item.Reservation.Id);
            await ApplyResultAsync(result.Ok, result.Message);
        }

        /// <summary>Khach goi bao khong toi nua - huy han don thay vi de treo.</summary>
        private async Task CancelReservationAsync(object? parameter)
        {
            if (parameter is not FlowItem { Kind: FlowKind.Arrival } item)
            {
                return;
            }
            var ok = ConfirmDialog.Ask(
                $"Huỷ đặt phòng của {item.GuestName}?",
                $"Đơn {item.BookingCode} sẽ chuyển sang Đã huỷ và phòng {item.RoomNumber} được giải phóng.",
                "Đơn đã huỷ không khôi phục lại được, muốn đặt lại thì tạo đơn mới.",
                "Huỷ đơn", isDanger: true);
            if (!ok)
            {
                return;
            }

            var result = await _reservationService.CancelAsync(item.Reservation.Id);
            await ApplyResultAsync(result.Ok, result.Message);
        }

        private async Task ApplyResultAsync(bool ok, string message)
        {
            if (ok)
            {
                Notify.Success(message);
                SelectedItem = null;
                await LoadAsync();
            }
            else
            {
                Notify.Error(message);
            }
        }

        /// <summary>Khach o them: doi ngay tra ngay tren don dang co.</summary>
        private async Task ExtendAsync(object? parameter)
        {
            if (parameter is not FlowItem { Kind: FlowKind.Stay, Stay: not null } item)
            {
                return;
            }
            var dialog = new ExtendStayDialog(new ExtendStayDialogViewModel(item.Stay))
            {
                Owner = Rooms.RoomMapViewModel.ActiveWindow(),
            };
            if (dialog.ShowDialog() == true)
            {
                await LoadAsync();
            }
        }

        /// <summary>Kiem do trong phong: ghi thu khach lam hong hoac mat truoc khi tra phong.</summary>
        private async Task OpenSurchargeAsync(object? parameter)
        {
            if (parameter is not FlowItem { Kind: FlowKind.Stay, Stay: not null } item)
            {
                return;
            }
            var dialog = new SurchargeDialog(new SurchargeDialogViewModel(item.Stay))
            {
                Owner = Rooms.RoomMapViewModel.ActiveWindow(),
            };
            dialog.ShowDialog();
            // Dialog dong bang nut Xong nen khong tra DialogResult - cu tai lai cho chac
            await LoadAsync();
        }

        public CheckInOutViewModel()
        {
            Filters =
            [
                new("Tất cả", null),
                new("Hôm nay", i => i.IsToday),
                new("Sắp đến", i => i.Kind == FlowKind.Arrival),
                new("Đang ở", i => i.Kind == FlowKind.Stay),
                new("Quá hạn", i => i.IsOverdue) { IsUrgent = true },
            ];
            _selectedFilter = Filters[0];
            _selectedFilter.IsSelected = true;

            ItemsView = new ListCollectionView(Items) { Filter = Filter };
            ActionCommand = new AsyncRelayCommand(RunActionAsync);
            RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
            PickFilterCommand = new RelayCommand(p => { if (p is FlowFilter f) { SelectedFilter = f; } });
            ExtendCommand = new AsyncRelayCommand(ExtendAsync);
            SurchargeCommand = new AsyncRelayCommand(OpenSurchargeAsync);
            NoShowCommand = new AsyncRelayCommand(MarkNoShowAsync);
            CancelCommand = new AsyncRelayCommand(CancelReservationAsync);
            _ = LoadAsync();
        }

        private bool Filter(object item)
            => item is FlowItem flow && (SelectedFilter.Predicate?.Invoke(flow) ?? true);

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var arrivals = await _service.GetArrivalsAsync();
                var stays = await _service.GetActiveAsync();

                var all = arrivals.Select(r => new FlowItem(r))
                    .Concat(stays.Select(s => new FlowItem(s)))
                    .OrderBy(i => i.SortRank)
                    .ThenBy(i => i.SortDate)
                    .ToList();

                // Lay tong phu thu cua tat ca luot dang o trong 1 truy van, khoi goi lap
                var totals = await _surchargeService.GetTotalsAsync(stays.Select(s => s.Id));
                foreach (var item in all)
                {
                    if (item.Stay != null && totals.TryGetValue(item.Stay.Id, out var total))
                    {
                        item.SurchargeTotal = total;
                    }
                }

                Items.Clear();
                foreach (var item in all)
                {
                    Items.Add(item);
                }

                foreach (var filter in Filters)
                {
                    filter.Count = filter.Predicate == null
                        ? Items.Count
                        : Items.Count(filter.Predicate);
                }
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception)
            {
                Notify.Error("Không tải được danh sách check-in/out.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RunActionAsync(object? parameter)
        {
            if (parameter is not FlowItem item)
            {
                return;
            }

            ServiceResult result;
            if (item.Kind == FlowKind.Arrival)
            {
                var checkIn = await _service.CheckInAsync(item.Reservation.Id);
                result = checkIn.Ok
                    ? ServiceResult.Success(checkIn.Message)
                    : ServiceResult.Failure(checkIn.Message);
            }
            else
            {
                var confirmed = ConfirmDialog.Ask(
                    $"Cho {item.GuestName} trả phòng {item.RoomNumber}?",
                    $"Khách đã ở {item.ChargeableNights} đêm, tạm tính {item.TotalChargeText}. "
                    + "Phòng sẽ chuyển sang Đang dọn.",
                    "Kiểm đồ trong phòng và ghi phụ thu trước khi trả — trả rồi không ghi thêm được. "
                    + "Hoá đơn chính thức lập ở màn Hoá đơn.",
                    "Cho trả phòng");
                if (!confirmed)
                {
                    return;
                }
                var checkOut = await _service.CheckOutAsync(item.Stay!.Id);
                result = checkOut.Ok
                    ? ServiceResult.Success(checkOut.Message)
                    : ServiceResult.Failure(checkOut.Message);
            }

            if (result.Ok)
            {
                Notify.Success(result.Message);
                // Viec vua xong bien khoi danh sach, bo chon de bang ben phai khong tro vao don da chet
                SelectedItem = null;
                await LoadAsync();
            }
            else
            {
                Notify.Error(result.Message);
            }
        }
    }
}
