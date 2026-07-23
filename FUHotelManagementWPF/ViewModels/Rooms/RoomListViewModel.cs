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
            // Chip trang thai va o tim kiem cong don voi nhau
            if (_selectedChip.Status != null && row.Room.Status != _selectedChip.Status)
            {
                return false;
            }
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
