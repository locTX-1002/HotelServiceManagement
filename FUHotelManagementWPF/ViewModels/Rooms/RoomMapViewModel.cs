using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>Mot o tren thanh thong ke dau man so do.</summary>
    public record StatusCount(RoomStatus Status, string Label, int Count);

    /// <summary>
    /// Tab so do: thanh thong ke + luoi card phong (thumbnail theo loai) nhom theo tang
    /// bang CollectionView GroupDescriptions. Bam card de doi trang thai van hanh.
    /// </summary>
    public record StatusFilterOption(string Label, RoomStatus? Status);

    public class RoomMapViewModel : ViewModelBase
    {
        private readonly IRoomService _roomService = new RoomService();
        private readonly Func<Task> _refreshAll;

        public ObservableCollection<RoomRow> Rooms { get; } = [];

        /// <summary>Ban nhom theo GroupTitle - moi tang 1 header tu dong, khong xep tay.</summary>
        public ICollectionView RoomsView { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        private StatusFilterOption _selectedStatusOption;
        public StatusFilterOption SelectedStatusOption
        {
            get => _selectedStatusOption;
            set
            {
                if (SetProperty(ref _selectedStatusOption, value))
                {
                    ApplyFilter();
                }
            }
        }

        public List<StatusFilterOption> StatusOptions { get; }

        private List<StatusCount> _statusCounts = [];
        public List<StatusCount> StatusCounts
        {
            get => _statusCounts;
            set => SetProperty(ref _statusCounts, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        /// <summary>
        /// Rong = khong con the nao SAU KHI loc, chu khong phai khach san khong co phong.
        /// Dung ICollectionView.IsEmpty thay vi dem tay: dem tay phai duyet het view moi
        /// lan binding doc property nay.
        /// </summary>
        public bool IsEmpty => !IsLoading && RoomsView.IsEmpty;

        public RelayCommand ChangeStatusCommand { get; }
        public RelayCommand SelectStatusFilterCommand { get; }

        /// <summary>
        /// Khong the an the phong (an het thi so do trong tron), nen van cho bam - dialog se noi ro
        /// vi sao khong doi duoc va con loi tat sang man Check-in/out, Dat phong (Le tan dung duoc).
        /// Chi sua tooltip de khong hua truoc mot viec vai tro do khong lam duoc.
        /// </summary>
        public string CardHint => AuthorizationPolicy.CanManageRooms
            ? "Bấm để đổi trạng thái vận hành"
            : "Bấm để xem chi tiết phòng";

        public RoomMapViewModel(Func<Task> refreshAll)
        {
            _refreshAll = refreshAll;
            StatusOptions =
            [
                new("Tất cả trạng thái", null),
                new("Trống", RoomStatus.Available),
                new("Đang ở", RoomStatus.Occupied),
                new("Đã đặt", RoomStatus.Reserved),
                new("Đang dọn", RoomStatus.Cleaning),
                new("Bảo trì", RoomStatus.Maintenance),
            ];
            _selectedStatusOption = StatusOptions[0];

            RoomsView = new ListCollectionView(Rooms);
            RoomsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RoomRow.GroupTitle)));
            ChangeStatusCommand = new RelayCommand(OpenStatusDialog);
            SelectStatusFilterCommand = new RelayCommand(SelectStatusFilter);
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var rooms = (await _roomService.GetAllAsync())
                    .Where(r => r.IsActive)
                    .ToList();

                var typesByFloor = rooms
                    .GroupBy(r => r.Floor)
                    .ToDictionary(
                        g => g.Key,
                        g => string.Join(" + ", g
                            .Select(r => r.RoomType?.TypeName ?? "?")
                            .Distinct()
                            .Select(t => t.ToUpperInvariant())));

                Rooms.Clear();
                foreach (var room in rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber))
                {
                    Rooms.Add(new RoomRow(room)
                    {
                        GroupTitle = $"TẦNG {room.Floor} · {typesByFloor[room.Floor]}",
                    });
                }

                StatusCounts =
                [
                    new(RoomStatus.Available, "Trống", rooms.Count(r => r.Status == RoomStatus.Available)),
                    new(RoomStatus.Occupied, "Đang ở", rooms.Count(r => r.Status == RoomStatus.Occupied)),
                    new(RoomStatus.Reserved, "Đã đặt", rooms.Count(r => r.Status == RoomStatus.Reserved)),
                    new(RoomStatus.Cleaning, "Đang dọn", rooms.Count(r => r.Status == RoomStatus.Cleaning)),
                    new(RoomStatus.Maintenance, "Bảo trì", rooms.Count(r => r.Status == RoomStatus.Maintenance)),
                ];

                ApplyFilter();
            }
            catch (Exception)
            {
                Notify.Error("Không tải được sơ đồ phòng. Kiểm tra kết nối SQL Server.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            var keyword = _searchText.Trim();
            var targetStatus = _selectedStatusOption.Status;

            RoomsView.Filter = item =>
            {
                if (item is not RoomRow row) return false;

                if (targetStatus.HasValue && row.Room.Status != targetStatus.Value)
                    return false;

                if (!string.IsNullOrEmpty(keyword))
                {
                    return row.Room.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || (row.Room.RoomType?.TypeName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || row.StatusText.Contains(keyword, StringComparison.OrdinalIgnoreCase);
                }

                return true;
            };

            OnPropertyChanged(nameof(IsEmpty));
        }

        private void SelectStatusFilter(object? param)
        {
            if (param is StatusCount count)
            {
                var opt = StatusOptions.FirstOrDefault(o => o.Status == count.Status);
                if (opt != null)
                {
                    SelectedStatusOption = opt;
                }
            }
        }

        private async void OpenStatusDialog(object? parameter)
        {
            if (parameter is not RoomRow row)
            {
                return;
            }

            try
            {
                var viewModel = new RoomStatusDialogViewModel(row.Room);
                var dialog = new RoomStatusDialog(viewModel) { Owner = ActiveWindow() };
                if (dialog.ShowDialog() == true)
                {
                    await _refreshAll();
                }
            }
            catch (Exception ex)
            {
                Notify.Error($"Lỗi: {ex.Message}");
            }
        }

        internal static Window? ActiveWindow()
            => Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible && w.IsActive)
               ?? Application.Current.MainWindow;
    }
}
