using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Rooms;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Reservations
{
    /// <summary>Mot cot ngay tren dau bang lich.</summary>
    public class CalendarDay
    {
        public DateTime Date { get; }
        public int Column { get; }
        public bool IsToday => Date.Date == DateTime.Today;
        public string DayName { get; }
        public string DayNumber => Date.ToString("dd/MM");

        public CalendarDay(DateTime date, int column)
        {
            Date = date;
            Column = column;
            DayName = date.DayOfWeek switch
            {
                DayOfWeek.Monday => "T2",
                DayOfWeek.Tuesday => "T3",
                DayOfWeek.Wednesday => "T4",
                DayOfWeek.Thursday => "T5",
                DayOfWeek.Friday => "T6",
                DayOfWeek.Saturday => "T7",
                _ => "CN",
            };
        }
    }

    /// <summary>O trong tren luoi: bam vao la dat phong dung phong do, dung ngay do.</summary>
    public class CalendarCell
    {
        public Room Room { get; }
        public DateTime Date { get; }
        public int Column { get; }
        public bool IsPast => Date.Date < DateTime.Today;

        public CalendarCell(Room room, DateTime date, int column)
        {
            Room = room;
            Date = date;
            Column = column;
        }
    }

    /// <summary>Thanh dat phong trai qua nhieu ngay tren mot dong phong.</summary>
    public class CalendarBar
    {
        public Reservation Reservation { get; }
        public int Column { get; }
        public int Span { get; }
        public ReservationStatus Status => Reservation.Status;
        public string GuestName => Reservation.Guest?.FullName ?? string.Empty;

        /// <summary>
        /// Luon hien ten khach, hep qua thi de no tu cat bot (TextTrimming ben XAML).
        ///
        /// Truoc day thanh hep chi hien so khach kieu "2k" cho khoi cat chu. Nhung mot don
        /// trai qua hai tuan se hien "2k" o tuan bi cat con lai mot ngay va hien ten o tuan
        /// kia - nhin ra thanh HAI don khac nhau cua hai khach khac nhau. Ten bi cat cut
        /// van doc ra la ten; "2k" thi khong.
        /// </summary>
        public string Label => GuestName;

        public string Tooltip =>
            $"{Reservation.BookingCode} · {GuestName}\n"
            + $"{Reservation.CheckInDate:dd/MM} → {Reservation.CheckOutDate:dd/MM/yyyy}\n"
            + $"{ReservationService.StatusText(Reservation.Status)} · bấm để sửa";

        /// <summary>Khoang dat keo dai ra ngoai tuan dang xem - phia truoc / phia sau.</summary>
        public bool ContinuesBefore { get; }
        public bool ContinuesAfter { get; }

        public CalendarBar(Reservation reservation, int column, int span,
            bool continuesBefore = false, bool continuesAfter = false)
        {
            Reservation = reservation;
            Column = column;
            Span = span;
            ContinuesBefore = continuesBefore;
            ContinuesAfter = continuesAfter;
        }
    }

    /// <summary>Mot dong = mot phong, gom 7 o trong lam nen va cac thanh dat phong de len tren.</summary>
    public class CalendarRoomRow
    {
        public Room Room { get; }
        public string RoomNumber => Room.RoomNumber;
        public string TypeName => Room.RoomType?.TypeName ?? string.Empty;
        public List<CalendarCell> Cells { get; }
        public List<CalendarBar> Bars { get; }

        public CalendarRoomRow(Room room, List<CalendarCell> cells, List<CalendarBar> bars)
        {
            Room = room;
            Cells = cells;
            Bars = bars;
        }
    }

    /// <summary>
    /// Tab "Lich phong": moi dong mot phong, moi cot mot ngay trong tuan.
    /// Nhin phat thay phong nao trong ngay nao, bam o trong la dat luon.
    /// </summary>
    public class RoomCalendarViewModel : ViewModelBase
    {
        private const int DayCount = 7;

        private readonly IRoomService _roomService = new RoomService();
        private readonly IReservationService _reservationService = new ReservationService();
        private readonly Func<Task> _refreshAll;

        /// <summary>Trang thai giu phong - dung ba trang thai nhu quy tac tim phong trong.</summary>
        private static readonly ReservationStatus[] Blocking =
            [ReservationStatus.Pending, ReservationStatus.Confirmed, ReservationStatus.CheckedIn];

        public ObservableCollection<CalendarRoomRow> Rows { get; } = [];
        public ObservableCollection<CalendarDay> Days { get; } = [];

        private DateTime _weekStart = StartOfWeek(DateTime.Today);
        public DateTime WeekStart
        {
            get => _weekStart;
            private set
            {
                if (SetProperty(ref _weekStart, value))
                {
                    OnPropertyChanged(nameof(WeekLabel));
                }
            }
        }

        public string WeekLabel =>
            $"{WeekStart:dd/MM} – {WeekStart.AddDays(DayCount - 1):dd/MM/yyyy}";

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        public bool IsEmpty => !IsLoading && Rows.Count == 0;

        public AsyncRelayCommand PrevWeekCommand { get; }
        public AsyncRelayCommand NextWeekCommand { get; }
        public AsyncRelayCommand ThisWeekCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }
        public AsyncRelayCommand BookCellCommand { get; }
        public AsyncRelayCommand OpenBarCommand { get; }

        public RoomCalendarViewModel(Func<Task> refreshAll)
        {
            _refreshAll = refreshAll;
            PrevWeekCommand = new AsyncRelayCommand(_ => ShiftWeek(-DayCount));
            NextWeekCommand = new AsyncRelayCommand(_ => ShiftWeek(DayCount));
            ThisWeekCommand = new AsyncRelayCommand(_ => { WeekStart = StartOfWeek(DateTime.Today); return LoadAsync(); });
            RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
            BookCellCommand = new AsyncRelayCommand(BookCellAsync);
            OpenBarCommand = new AsyncRelayCommand(OpenBarAsync);
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            // Tuan bat dau thu Hai cho hop thoi quen o Viet Nam
            var diff = ((int)date.DayOfWeek + 6) % 7;
            return date.Date.AddDays(-diff);
        }

        private Task ShiftWeek(int days)
        {
            WeekStart = WeekStart.AddDays(days);
            return LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var rooms = await _roomService.GetAllAsync();
                var reservations = await _reservationService.GetAllAsync();

                Days.Clear();
                for (var i = 0; i < DayCount; i++)
                {
                    Days.Add(new CalendarDay(WeekStart.AddDays(i), i));
                }

                var weekEnd = WeekStart.AddDays(DayCount);
                Rows.Clear();
                foreach (var room in rooms.Where(r => r.IsActive).OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber))
                {
                    var cells = new List<CalendarCell>();
                    for (var i = 0; i < DayCount; i++)
                    {
                        cells.Add(new CalendarCell(room, WeekStart.AddDays(i), i));
                    }

                    var bars = reservations
                        .Where(r => r.RoomId == room.Id
                                    && Blocking.Contains(r.Status)
                                    && r.CheckInDate.Date < weekEnd
                                    && r.CheckOutDate.Date > WeekStart)
                        .Select(r => ToBar(r))
                        .Where(b => b.Span > 0)
                        .ToList();

                    Rows.Add(new CalendarRoomRow(room, cells, bars));
                }

                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception)
            {
                Notify.Error("Không tải được lịch phòng.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cat khoang dat phong cho vua trong tuan dang xem. Ngay tra khong tinh la mot dem.
        ///
        /// Khach nhan phong SOM hon ngay tren don thi ve tu ngay nhan that: neu cu ve theo
        /// don, phong da co nguoi tu hom qua ma tren lich van trong, le tan nhin vao tuong
        /// con ban duoc.
        /// </summary>
        private CalendarBar ToBar(Reservation reservation)
        {
            var from = reservation.CheckInDate.Date;
            var to = reservation.CheckOutDate.Date;

            if (reservation.Stay is { } stay)
            {
                // Nhan phong SOM hon don thi keo dau thanh ve ngay vao that.
                if (stay.ActualCheckIn.Date < from) { from = stay.ActualCheckIn.Date; }

                // Va keo DUOI thanh theo thuc te: khach chua tra phong thi dem nay van con
                // trong phong, du don ghi ngay tra la hom qua. Thieu ve nay thi phong dang
                // co nguoi ma o lich trong nhu da tra - le tan tuong ban lai duoc.
                // Moc cuoi KHONG tinh la mot dem, nen con o thi phai la ngay mai.
                var actualTo = stay.ActualCheckOut?.Date ?? DateTime.Today.AddDays(1);
                if (actualTo > to) { to = actualTo; }
            }

            var rawStart = (from - WeekStart).Days;
            var rawEnd = (to - WeekStart).Days;
            var start = Math.Max(0, rawStart);
            var end = Math.Min(DayCount, rawEnd);
            // Bi cat thi phai bao: thanh cat trong y het thanh bat dau dung dau tuan, nhin
            // vao la doc sai ngay nhan phong.
            return new CalendarBar(reservation, start, Math.Max(0, end - start),
                continuesBefore: rawStart < 0, continuesAfter: rawEnd > DayCount);
        }

        private async Task BookCellAsync(object? parameter)
        {
            if (parameter is not CalendarCell cell)
            {
                return;
            }

            var viewModel = new CreateReservationDialogViewModel(null)
            {
                CheckIn = cell.Date,
                CheckOut = cell.Date.AddDays(1),
                PreferredRoomId = cell.Room.Id,
            };
            await viewModel.PrefillRoomsAsync();

            var dialog = new CreateReservationDialog(viewModel) { Owner = RoomMapViewModel.ActiveWindow() };
            if (dialog.ShowDialog() == true)
            {
                await _refreshAll();
            }
        }

        private async Task OpenBarAsync(object? parameter)
        {
            if (parameter is not CalendarBar bar)
            {
                return;
            }
            var dialog = new CreateReservationDialog(new CreateReservationDialogViewModel(bar.Reservation))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            };
            if (dialog.ShowDialog() == true)
            {
                await _refreshAll();
            }
        }
    }
}
