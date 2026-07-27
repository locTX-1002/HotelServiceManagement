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
    /// <summary>
    /// Tab danh sach phong: DataGrid + CRUD qua popup dialog + xac nhan xoa.
    /// MAU CHUAN danh sach cho ca nhom: ObservableCollection + ICollectionView filter.
    /// </summary>
    public class RoomListViewModel : ViewModelBase
    {
        private readonly IRoomService _roomService = new RoomService();
        private readonly IReservationService _reservationService = new ReservationService();
        private readonly Func<Task> _refreshAll;

        public ObservableCollection<RoomRow> Rows { get; } = [];
        public ICollectionView RowsView { get; }

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

        public bool IsEmpty => !IsLoading && !RowsView.Cast<object>().Any();

        /// <summary>Phan biet "chua co phong nao" voi "bo loc khong ra ket qua".</summary>
        public string EmptyText => Rows.Count == 0
            ? "Chưa có phòng nào — bấm + Thêm phòng để tạo phòng đầu tiên."
            : "Không có phòng nào khớp bộ lọc hiện tại.";

        // D3 master-detail: chon dong ben trai -> panel chi tiet ben phai
        private RoomRow? _selectedRow;
        public RoomRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (SetProperty(ref _selectedRow, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    ResetGallery();
                    _ = LoadRecentReservationsAsync();
                }
            }
        }

        public bool HasSelection => _selectedRow != null;

        // --- Gallery ảnh trong panel chi tiết (thay cho dialog Chi tiết cũ) ---
        private List<string> _gallery = [];
        private int _galleryIndex;

        public string DetailImage => _gallery.Count > 0 ? _gallery[_galleryIndex] : string.Empty;
        public string GalleryCounter => _gallery.Count > 0 ? $"{_galleryIndex + 1} / {_gallery.Count}" : string.Empty;

        public RelayCommand PrevImageCommand { get; }
        public RelayCommand NextImageCommand { get; }

        private void ResetGallery()
        {
            _gallery = _selectedRow == null
                ? []
                : RoomImages.Gallery(_selectedRow.Room.RoomTypeId, _selectedRow.TypeName);
            _galleryIndex = 0;
            OnPropertyChanged(nameof(DetailImage));
            OnPropertyChanged(nameof(GalleryCounter));
        }

        private void MoveGallery(int delta)
        {
            if (_gallery.Count == 0)
            {
                return;
            }
            _galleryIndex = (_galleryIndex + delta + _gallery.Count) % _gallery.Count;
            OnPropertyChanged(nameof(DetailImage));
            OnPropertyChanged(nameof(GalleryCounter));
        }

        // --- Lịch sử đặt phòng gần đây ---
        public ObservableCollection<RecentReservationRow> RecentReservations { get; } = [];
        private bool _isLoadingHistory;
        public bool IsLoadingHistory
        {
            get => _isLoadingHistory;
            set => SetProperty(ref _isLoadingHistory, value);
        }

        private async Task LoadRecentReservationsAsync()
        {
            RecentReservations.Clear();
            if (_selectedRow == null || !CanViewReservations) return;
            IsLoadingHistory = true;
            try
            {
                var list = await _reservationService.GetRecentByRoomAsync(_selectedRow.Room.Id);
                foreach (var r in list)
                {
                    RecentReservations.Add(new RecentReservationRow(r));
                }
            }
            catch
            {
                // Soft failure for history load
            }
            finally
            {
                IsLoadingHistory = false;
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RowsView.Refresh();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(EmptyText));
                }
            }
        }

        // --- Bộ lọc Tầng ---
        public ObservableCollection<string> FloorOptions { get; } = [];
        private string _selectedFloor = "Tất cả tầng";
        public string SelectedFloor
        {
            get => _selectedFloor;
            set
            {
                if (SetProperty(ref _selectedFloor, value))
                {
                    RowsView.Refresh();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(EmptyText));
                }
            }
        }

        // --- Bộ lọc Loại phòng ---
        public ObservableCollection<RoomTypeOption> RoomTypeOptions { get; } = [];
        private RoomTypeOption? _selectedRoomType;
        public RoomTypeOption? SelectedRoomType
        {
            get => _selectedRoomType;
            set
            {
                if (SetProperty(ref _selectedRoomType, value))
                {
                    RowsView.Refresh();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(EmptyText));
                }
            }
        }

        // --- Sắp xếp ---
        public ObservableCollection<RoomSortOption> SortOptions { get; } = [];
        private RoomSortOption? _selectedSortOption;
        public RoomSortOption? SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, value))
                {
                    ApplySort();
                }
            }
        }

        private void ApplySort()
        {
            RowsView.SortDescriptions.Clear();
            if (_selectedSortOption == null) return;

            switch (_selectedSortOption.Key)
            {
                case 1:
                    RowsView.SortDescriptions.Add(new SortDescription("Room.RoomNumber", ListSortDirection.Ascending));
                    break;
                case 2:
                    RowsView.SortDescriptions.Add(new SortDescription("Room.RoomNumber", ListSortDirection.Descending));
                    break;
                case 3:
                    RowsView.SortDescriptions.Add(new SortDescription("BasePrice", ListSortDirection.Ascending));
                    break;
                case 4:
                    RowsView.SortDescriptions.Add(new SortDescription("BasePrice", ListSortDirection.Descending));
                    break;
            }
        }

        public void SelectRoomTypeFilter(int roomTypeId)
        {
            var target = RoomTypeOptions.FirstOrDefault(t => t.Id == roomTypeId);
            if (target != null)
            {
                SelectedRoomType = target;
            }
        }

        // --- Chip loc trang thai: Tat ca / Trong / Da dat / Dang o / Dang don / Bao tri ---
        public ObservableCollection<RoomStatusChip> StatusChips { get; } = [];
        private RoomStatusChip _selectedChip;

        private void PickChip(RoomStatusChip chip)
        {
            foreach (var item in StatusChips)
            {
                item.IsSelected = ReferenceEquals(item, chip);
            }
            _selectedChip = chip;
            RowsView.Refresh();
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(EmptyText));
            // Dong dang chon co the bi loc mat -> bo chon cho panel chi tiet khoi hien nham
            if (SelectedRow != null && !FilterRow(SelectedRow))
            {
                SelectedRow = null;
            }
        }

        private void RefreshChipCounts()
        {
            foreach (var chip in StatusChips)
            {
                chip.Count = chip.Status == null
                    ? Rows.Count
                    : Rows.Count(r => r.Room.Status == chip.Status);
            }
        }

        /// <summary>An nut them/sua/xoa/doi trang thai voi vai tro khong duoc phep - service van la lop chan cuoi.</summary>
        public bool CanManageRooms => AuthorizationPolicy.CanManageRooms;
        public bool CanViewReservations => AuthorizationPolicy.CanViewReservations;

        public RelayCommand PickStatusCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand ChangeStatusCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public RoomListViewModel(Func<Task> refreshAll)
        {
            _refreshAll = refreshAll;
            RowsView = new ListCollectionView(Rows) { Filter = FilterRow };

            StatusChips.Add(new RoomStatusChip("Tất cả", null));
            foreach (var status in Enum.GetValues<RoomStatus>())
            {
                StatusChips.Add(new RoomStatusChip(RoomService.RoomStatusText(status), status));
            }
            _selectedChip = StatusChips[0];
            _selectedChip.IsSelected = true;
            PickStatusCommand = new RelayCommand(p => { if (p is RoomStatusChip chip) { PickChip(chip); } });

            RoomTypeOptions.Add(new RoomTypeOption(null, "Tất cả loại phòng"));
            _selectedRoomType = RoomTypeOptions[0];

            SortOptions.Add(new RoomSortOption(1, "Số phòng (A-Z)"));
            SortOptions.Add(new RoomSortOption(2, "Số phòng (Z-A)"));
            SortOptions.Add(new RoomSortOption(3, "Giá phòng (thấp -> cao)"));
            SortOptions.Add(new RoomSortOption(4, "Giá phòng (cao -> thấp)"));
            _selectedSortOption = SortOptions[0];
            ApplySort();

            AddCommand = new RelayCommand(_ => OpenEditDialog(null));
            EditCommand = new RelayCommand(p => OpenEditDialog(p as RoomRow));
            ChangeStatusCommand = new RelayCommand(_ => OpenStatusDialog());
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);
            PrevImageCommand = new RelayCommand(_ => MoveGallery(-1));
            NextImageCommand = new RelayCommand(_ => MoveGallery(1));
        }

        private async void OpenStatusDialog()
        {
            if (SelectedRow == null)
            {
                return;
            }
            var dialog = new RoomStatusDialog(new RoomStatusDialogViewModel(SelectedRow.Room))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            };
            if (dialog.ShowDialog() == true)
            {
                await _refreshAll();
            }
        }

        /// <summary>Dong tom tat canh o tim kiem, lap khoang trong toolbar.</summary>
        public string TotalText
            => $"{Rows.Count} phòng · {Rows.Count(r => r.Room.IsActive)} đang dùng";

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var rooms = await _roomService.GetAllAsync();
                Rows.Clear();
                foreach (var room in rooms)
                {
                    Rows.Add(new RoomRow(room));
                }

                // Cập nhật bộ lọc Tầng
                var floors = rooms.Select(r => r.Floor).Distinct().OrderBy(f => f).Select(f => $"Tầng {f}").ToList();
                FloorOptions.Clear();
                FloorOptions.Add("Tất cả tầng");
                foreach (var f in floors) FloorOptions.Add(f);
                if (!FloorOptions.Contains(SelectedFloor)) SelectedFloor = "Tất cả tầng";

                // Cập nhật bộ lọc Loại phòng
                var types = rooms.Where(r => r.RoomType != null).Select(r => r.RoomType!).GroupBy(t => t.Id).Select(g => g.First()).OrderBy(t => t.TypeName).ToList();
                var currentTypeId = SelectedRoomType?.Id;
                RoomTypeOptions.Clear();
                RoomTypeOptions.Add(new RoomTypeOption(null, "Tất cả loại phòng"));
                foreach (var t in types) RoomTypeOptions.Add(new RoomTypeOption(t.Id, t.TypeName));
                SelectedRoomType = RoomTypeOptions.FirstOrDefault(t => t.Id == currentTypeId) ?? RoomTypeOptions[0];

                RefreshChipCounts();
                OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception)
            {
                Notify.Error("Không tải được danh sách phòng.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool FilterRow(object item)
        {
            if (item is not RoomRow row)
            {
                return false;
            }
            // Chip trang thai
            if (_selectedChip.Status != null && row.Room.Status != _selectedChip.Status)
            {
                return false;
            }

            // Lọc theo Tầng
            if (SelectedFloor != "Tất cả tầng" && $"Tầng {row.Room.Floor}" != SelectedFloor)
            {
                return false;
            }

            // Lọc theo Loại phòng
            if (SelectedRoomType?.Id != null && row.Room.RoomTypeId != SelectedRoomType.Id)
            {
                return false;
            }

            // Tìm kiếm theo từ khoá
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }
            var keyword = SearchText.Trim().ToLower();
            return row.Room.RoomNumber.ToLower().Contains(keyword)
                   || row.TypeName.ToLower().Contains(keyword);
        }

        private async void OpenEditDialog(RoomRow? existing)
        {
            var viewModel = new RoomEditDialogViewModel(existing?.Room);
            var dialog = new RoomEditDialog(viewModel) { Owner = RoomMapViewModel.ActiveWindow() };
            if (dialog.ShowDialog() == true)
            {
                await _refreshAll();
            }
        }

        private async Task DeleteAsync(object? parameter)
        {
            if (parameter is not RoomRow row)
            {
                return;
            }

            // MessageBox chi dung cho xac nhan xoa - dung quy uoc nhom
            var ok = ConfirmDialog.Ask(
                $"Xoá phòng {row.Room.RoomNumber}?",
                "Phòng sẽ biến mất khỏi sơ đồ và không đặt được nữa.",
                "Nếu phòng đã từng có khách đặt thì hệ thống chỉ chuyển sang Ngừng dùng "
                + "chứ không xoá hẳn, để giữ lịch sử và số liệu báo cáo.",
                "Xoá phòng", isDanger: true);
            if (!ok)
            {
                return;
            }

            var result = await _roomService.DeleteAsync(row.Room.Id);
            if (result.Ok)
            {
                Notify.Success(result.Message);
                await _refreshAll();
            }
            else
            {
                Notify.Error(result.Message);
            }
        }
    }
}
