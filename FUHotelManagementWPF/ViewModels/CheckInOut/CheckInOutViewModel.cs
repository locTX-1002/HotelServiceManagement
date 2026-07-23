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

    public record FlowFilter(string Label, Func<FlowItem, bool>? Predicate);

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
            set { if (SetProperty(ref _selectedFilter, value)) { ItemsView.Refresh(); OnPropertyChanged(nameof(IsEmpty)); } }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        public bool IsEmpty => !IsLoading && ItemsView.Cast<object>().Any() == false;

        // Số đếm cho từng chip
        public int CountAll => Items.Count;
        public int CountToday => Items.Count(i => i.IsToday);
        public int CountArrival => Items.Count(i => i.Kind == FlowKind.Arrival);
        public int CountStay => Items.Count(i => i.Kind == FlowKind.Stay);
        public int CountOverdue => Items.Count(i => i.IsOverdue);
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
                new("Quá hạn", i => i.IsOverdue),
            ];
            _selectedFilter = Filters[0];

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

                OnPropertyChanged(nameof(CountAll));
                OnPropertyChanged(nameof(CountToday));
                OnPropertyChanged(nameof(CountArrival));
                OnPropertyChanged(nameof(CountStay));
                OnPropertyChanged(nameof(CountOverdue));
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
                await LoadAsync();
            }
            else
            {
                Notify.Error(result.Message);
            }
        }
    }
}
