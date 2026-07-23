using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Rooms;
using Services;

namespace FUHotelManagementWPF.ViewModels.CheckInOut
{
    /// <summary>Dòng "chờ check-in" — đặt phòng đã xác nhận, khách chưa đến quầy.</summary>
    public class ArrivalRow
    {
        public Reservation Reservation { get; }
        public string Thumbnail => RoomImages.Thumbnail(
            Reservation.Room?.RoomTypeId ?? 0, Reservation.Room?.RoomType?.TypeName ?? string.Empty);
        public string GuestName => Reservation.Guest?.FullName ?? string.Empty;
        public string RoomNumber => Reservation.Room?.RoomNumber ?? string.Empty;
        public string SubText =>
            $"{RoomNumber} · {Reservation.Room?.RoomType?.TypeName} · {Reservation.NumberOfGuests} khách";
        public string DateText => $"{Reservation.CheckInDate:dd/MM} → {Reservation.CheckOutDate:dd/MM/yyyy}";
        public string BookingCode => Reservation.BookingCode;

        /// <summary>Hôm nay là ngày nhận phòng → nổi bật để lễ tân ưu tiên.</summary>
        public bool IsToday => Reservation.CheckInDate.Date == DateTime.Today;
        public string DayLabel => Reservation.CheckInDate.Date == DateTime.Today ? "Hôm nay"
            : Reservation.CheckInDate.Date < DateTime.Today ? "Quá hạn đến"
            : $"Còn {(Reservation.CheckInDate.Date - DateTime.Today).Days} ngày";

        public ArrivalRow(Reservation reservation) => Reservation = reservation;
    }

    /// <summary>Dòng "đang lưu trú" — khách đã check-in, chờ trả phòng.</summary>
    public class StayRow
    {
        public Stay Stay { get; }
        private Reservation Res => Stay.Reservation;

        public string Thumbnail => RoomImages.Thumbnail(
            Res.Room?.RoomTypeId ?? 0, Res.Room?.RoomType?.TypeName ?? string.Empty);
        public string GuestName => Res.Guest?.FullName ?? string.Empty;
        public string RoomNumber => Res.Room?.RoomNumber ?? string.Empty;
        public string SubText => $"{RoomNumber} · {Res.Room?.RoomType?.TypeName} · vào {Stay.ActualCheckIn:dd/MM HH:mm}";
        public string PlannedOutText => $"Dự kiến trả: {Res.CheckOutDate:dd/MM/yyyy}";
        public int Nights => Math.Max(1, (DateTime.Today - Stay.ActualCheckIn.Date).Days);
        public string NightsText => $"{Nights} đêm";

        /// <summary>Đã quá ngày trả phòng dự kiến → cảnh báo lễ tân.</summary>
        public bool IsOverdue => DateTime.Today > Res.CheckOutDate.Date;
        public string DayLabel => IsOverdue ? "Quá hạn trả"
            : Res.CheckOutDate.Date == DateTime.Today ? "Trả hôm nay"
            : $"Còn {(Res.CheckOutDate.Date - DateTime.Today).Days} đêm";

        public StayRow(Stay stay) => Stay = stay;
    }

    /// <summary>Module Check-in / Check-out: 2 danh sách chờ đến và đang ở.</summary>
    public class CheckInOutViewModel : ViewModelBase
    {
        private readonly IStayService _service = new StayService();

        public ObservableCollection<ArrivalRow> Arrivals { get; } = [];
        public ObservableCollection<StayRow> Stays { get; } = [];

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsArrivalsEmpty));
                    OnPropertyChanged(nameof(IsStaysEmpty));
                }
            }
        }

        public bool IsArrivalsEmpty => !IsLoading && Arrivals.Count == 0;
        public bool IsStaysEmpty => !IsLoading && Stays.Count == 0;

        public string ArrivalsTitle => $"Chờ check-in ({Arrivals.Count})";
        public string StaysTitle => $"Đang lưu trú ({Stays.Count})";
        public string TodayText => $"Hôm nay {DateTime.Today:dd/MM/yyyy}";

        public AsyncRelayCommand CheckInCommand { get; }
        public AsyncRelayCommand CheckOutCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }

        public CheckInOutViewModel()
        {
            CheckInCommand = new AsyncRelayCommand(CheckInAsync);
            CheckOutCommand = new AsyncRelayCommand(CheckOutAsync);
            RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var arrivals = await _service.GetArrivalsAsync();
                var stays = await _service.GetActiveAsync();

                Arrivals.Clear();
                foreach (var r in arrivals)
                {
                    Arrivals.Add(new ArrivalRow(r));
                }
                Stays.Clear();
                foreach (var s in stays)
                {
                    Stays.Add(new StayRow(s));
                }

                OnPropertyChanged(nameof(ArrivalsTitle));
                OnPropertyChanged(nameof(StaysTitle));
                OnPropertyChanged(nameof(IsArrivalsEmpty));
                OnPropertyChanged(nameof(IsStaysEmpty));
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

        private async Task CheckInAsync(object? parameter)
        {
            if (parameter is not ArrivalRow row)
            {
                return;
            }
            var result = await _service.CheckInAsync(row.Reservation.Id, AppSession.CurrentUser?.Id ?? 0);
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

        private async Task CheckOutAsync(object? parameter)
        {
            if (parameter is not StayRow row)
            {
                return;
            }

            var confirm = MessageBox.Show(
                $"Check-out phòng {row.RoomNumber} ({row.GuestName})?\n\n" +
                $"Đã ở {row.NightsText}. Phòng sẽ chuyển sang Đang dọn, hoá đơn lập ở màn Hoá đơn.",
                "Xác nhận check-out", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await _service.CheckOutAsync(row.Stay.Id, AppSession.CurrentUser?.Id ?? 0);
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
