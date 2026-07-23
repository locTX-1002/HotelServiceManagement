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

        public string NightsText => $"{ChargeableNights} đêm × {BasePrice:N0} đ";
        public string RoomChargeText => $"{RoomCharge:N0} đ";
        public string EstimateLabel => Kind == FlowKind.Arrival ? "Dự kiến cả kỳ" : "Tạm tính";

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
                result = await _service.CheckInAsync(item.Reservation.Id, AppSession.CurrentUser?.Id ?? 0);
            }
            else
            {
                var confirm = MessageBox.Show(
                    $"Check-out phòng {item.RoomNumber} ({item.GuestName})?\n\n" +
                    $"Đã ở {item.Nights} đêm. Phòng sẽ chuyển sang Đang dọn, hoá đơn lập ở màn Hoá đơn.",
                    "Xác nhận check-out", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }
                result = await _service.CheckOutAsync(item.Stay!.Id, AppSession.CurrentUser?.Id ?? 0);
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
