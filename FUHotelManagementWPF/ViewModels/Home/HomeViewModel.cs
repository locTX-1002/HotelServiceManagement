using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Reservations;
using FUHotelManagementWPF.ViewModels.Rooms;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Home
{
    /// <summary>Mot o hang phong tren luoi mosaic: anh phu kin, chu de len day anh.</summary>
    public class RoomTypeTile
    {
        public RoomType RoomType { get; }
        public string Name => RoomType.TypeName;
        public string Image => RoomImages.Thumbnail(RoomType.Id, RoomType.TypeName);
        public string MetaText => $"{RoomType.Capacity} khách · {RoomType.BasePrice:N0} đ/đêm";
        public string? Description => RoomType.Description;
        public bool HasDescription => !string.IsNullOrWhiteSpace(RoomType.Description);

        public RoomTypeTile(RoomType roomType) => RoomType = roomType;
    }

    /// <summary>
    /// Trang chu kieu landing: hero anh khach san, thanh tra cuu phong trong,
    /// gioi thieu, luoi hang phong, va dai so lieu van hanh mong o duoi cung.
    /// </summary>
    public class HomeViewModel : ViewModelBase
    {
        private readonly IRoomService _roomService = new RoomService();
        private readonly IRoomTypeService _roomTypeService = new RoomTypeService();
        private readonly IReservationService _reservationService = new ReservationService();
        private readonly IStayService _stayService = new StayService();

        // --- Hero: bo anh xoay vong ---
        private static readonly string[] HeroImages =
        [
            "pack://application:,,,/Assets/Brand/hero-1.jpg",
            "pack://application:,,,/Assets/Brand/hero-2.jpg",
            "pack://application:,,,/Assets/Brand/hero-3.jpg",
        ];

        private int _heroIndex;
        public string HeroImage => HeroImages[_heroIndex];
        public string HeroCounter => $"{_heroIndex + 1} / {HeroImages.Length}";
        public string AboutImage => "pack://application:,,,/Assets/Brand/about-1.jpg";

        public RelayCommand PrevHeroCommand { get; }
        public RelayCommand NextHeroCommand { get; }

        private void MoveHero(int delta)
        {
            _heroIndex = (_heroIndex + delta + HeroImages.Length) % HeroImages.Length;
            OnPropertyChanged(nameof(HeroImage));
            OnPropertyChanged(nameof(HeroCounter));
        }

        // --- Loi chao ---
        public string HotelName => HotelInfo.Name;
        public string Tagline => $"{HotelInfo.Tagline} · từ năm {HotelInfo.EstablishedYear}";
        public string AboutText => HotelInfo.About;

        public string Greeting
        {
            get
            {
                var name = AppSession.CurrentUser?.FullName ?? string.Empty;
                var hour = DateTime.Now.Hour;
                var part = hour < 12 ? "Chào buổi sáng" : hour < 18 ? "Chào buổi chiều" : "Chào buổi tối";
                return string.IsNullOrWhiteSpace(name) ? part : $"{part}, {name}";
            }
        }

        public string TodayText => DateTime.Today.ToString("dddd, dd/MM/yyyy",
            new System.Globalization.CultureInfo("vi-VN"));

        // --- Thanh tra cuu phong trong ---
        private DateTime _checkIn = DateTime.Today;
        public DateTime CheckIn
        {
            get => _checkIn;
            set
            {
                if (SetProperty(ref _checkIn, value) && _checkOut <= value)
                {
                    CheckOut = value.AddDays(1);
                }
            }
        }

        private DateTime _checkOut = DateTime.Today.AddDays(1);
        public DateTime CheckOut
        {
            get => _checkOut;
            set => SetProperty(ref _checkOut, value);
        }

        private int _guests = 2;
        public int Guests
        {
            get => _guests;
            set => SetProperty(ref _guests, value);
        }

        private string _searchResult = string.Empty;
        public string SearchResult
        {
            get => _searchResult;
            set
            {
                if (SetProperty(ref _searchResult, value))
                {
                    OnPropertyChanged(nameof(HasSearchResult));
                }
            }
        }

        public bool HasSearchResult => !string.IsNullOrEmpty(SearchResult);

        public AsyncRelayCommand SearchCommand { get; }

        private async Task SearchAsync(object? _)
        {
            if (CheckOut <= CheckIn)
            {
                Notify.Warning("Ngày trả phòng phải sau ngày nhận phòng.");
                return;
            }

            var result = await _reservationService.GetAvailableRoomsAsync(CheckIn, CheckOut);
            if (!result.Ok)
            {
                Notify.Error(result.Message);
                return;
            }

            // Loc them theo suc chua de con so hien ra dung voi so khach da chon
            var rooms = (result.Data ?? []).Where(r => (r.RoomType?.Capacity ?? 0) >= Guests).ToList();
            var nights = Math.Max(1, (CheckOut.Date - CheckIn.Date).Days);

            SearchResult = rooms.Count == 0
                ? $"Không còn phòng trống cho {Guests} khách trong khoảng này."
                : $"Còn {rooms.Count} phòng trống cho {Guests} khách · {nights} đêm · "
                  + $"giá từ {rooms.Min(r => r.RoomType?.BasePrice ?? 0):N0} đ/đêm";
        }

        /// <summary>Mo thang dialog dat phong voi ngay va so khach da dien san tu thanh tra cuu.</summary>
        public RelayCommand BookCommand { get; }

        private async void OpenBookDialog(RoomType? preferredType)
        {
            var viewModel = new CreateReservationDialogViewModel(null)
            {
                CheckIn = CheckIn,
                CheckOut = CheckOut > CheckIn ? CheckOut : CheckIn.AddDays(1),
                NumberOfGuests = preferredType != null ? Math.Min(Guests, preferredType.Capacity) : Guests,
            };
            var dialog = new CreateReservationDialog(viewModel)
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            };
            if (dialog.ShowDialog() == true)
            {
                Notify.Success("Đã tạo đặt phòng.");
                await LoadAsync();
            }
        }

        // --- Gioi thieu: so lieu dem tu database ---
        private int _roomCount;
        public int RoomCount
        {
            get => _roomCount;
            private set => SetProperty(ref _roomCount, value);
        }

        private int _roomTypeCount;
        public int RoomTypeCount
        {
            get => _roomTypeCount;
            private set => SetProperty(ref _roomTypeCount, value);
        }

        public int EstablishedYear => HotelInfo.EstablishedYear;

        // --- Luoi hang phong mosaic: 1 o lon + 2 o doc + 1 o ngang ---
        private RoomTypeTile? _featuredTile;
        public RoomTypeTile? FeaturedTile
        {
            get => _featuredTile;
            private set { if (SetProperty(ref _featuredTile, value)) { OnPropertyChanged(nameof(HasFeatured)); } }
        }

        private RoomTypeTile? _secondTile;
        public RoomTypeTile? SecondTile
        {
            get => _secondTile;
            private set { if (SetProperty(ref _secondTile, value)) { OnPropertyChanged(nameof(HasSecond)); } }
        }

        private RoomTypeTile? _thirdTile;
        public RoomTypeTile? ThirdTile
        {
            get => _thirdTile;
            private set { if (SetProperty(ref _thirdTile, value)) { OnPropertyChanged(nameof(HasThird)); } }
        }

        private RoomTypeTile? _wideTile;
        public RoomTypeTile? WideTile
        {
            get => _wideTile;
            private set { if (SetProperty(ref _wideTile, value)) { OnPropertyChanged(nameof(HasWide)); } }
        }

        // Khach san co it hon 4 hang phong thi o thua tu an di, luoi khong bi thung
        public bool HasFeatured => _featuredTile != null;
        public bool HasSecond => _secondTile != null;
        public bool HasThird => _thirdTile != null;
        public bool HasWide => _wideTile != null;

        public string PriceFromText => _cheapestPrice > 0 ? $"Từ {_cheapestPrice:N0} đ một đêm" : string.Empty;
        private decimal _cheapestPrice;

        public RelayCommand OpenRoomTypesCommand { get; }

        // --- Dai so lieu van hanh o duoi cung ---
        private int _availableRooms;
        public int AvailableRooms
        {
            get => _availableRooms;
            private set => SetProperty(ref _availableRooms, value);
        }

        private int _stayingRooms;
        public int StayingRooms
        {
            get => _stayingRooms;
            private set => SetProperty(ref _stayingRooms, value);
        }

        private int _arrivalsToday;
        public int ArrivalsToday
        {
            get => _arrivalsToday;
            private set => SetProperty(ref _arrivalsToday, value);
        }

        private int _overdueTasks;
        public int OverdueTasks
        {
            get => _overdueTasks;
            private set
            {
                if (SetProperty(ref _overdueTasks, value))
                {
                    OnPropertyChanged(nameof(HasOverdue));
                }
            }
        }

        public bool HasOverdue => _overdueTasks > 0;

        public RelayCommand OpenCheckInOutCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public HomeViewModel()
        {
            PrevHeroCommand = new RelayCommand(_ => MoveHero(-1));
            NextHeroCommand = new RelayCommand(_ => MoveHero(1));
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            BookCommand = new RelayCommand(p => OpenBookDialog(p as RoomType));
            OpenRoomTypesCommand = new RelayCommand(_ => NavigationService.NavigateTo("Sơ đồ phòng"));
            OpenCheckInOutCommand = new RelayCommand(_ => NavigationService.NavigateTo("Check-in / Check-out"));
            RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var rooms = await _roomService.GetAllAsync();
                var types = await _roomTypeService.GetActiveAsync();
                var arrivals = await _stayService.GetArrivalsAsync();
                var stays = await _stayService.GetActiveAsync();

                RoomCount = rooms.Count(r => r.IsActive);
                RoomTypeCount = types.Count;

                AvailableRooms = rooms.Count(r => r.IsActive && r.Status == RoomStatus.Available);
                StayingRooms = stays.Count;
                ArrivalsToday = arrivals.Count(r => r.CheckInDate.Date == DateTime.Today);
                OverdueTasks = arrivals.Count(r => r.CheckInDate.Date < DateTime.Today)
                               + stays.Count(s => s.Reservation.CheckOutDate.Date < DateTime.Today);

                // Hang dat tien nhat lam o lon, phan con lai xep vao 3 o phu
                var ordered = types.OrderByDescending(t => t.BasePrice).ToList();
                _cheapestPrice = ordered.Count > 0 ? ordered.Min(t => t.BasePrice) : 0;
                OnPropertyChanged(nameof(PriceFromText));

                FeaturedTile = Tile(ordered, 0);
                SecondTile = Tile(ordered, 1);
                ThirdTile = Tile(ordered, 2);
                WideTile = Tile(ordered, 3);
            }
            catch (Exception)
            {
                Notify.Error("Không tải được dữ liệu trang chủ.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static RoomTypeTile? Tile(List<RoomType> types, int index)
            => index < types.Count ? new RoomTypeTile(types[index]) : null;
    }
}
