using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
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

        public bool IsEmpty => !IsLoading && Rows.Count == 0;

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
                }
            }
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand ChangeStatusCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public RoomListViewModel(Func<Task> refreshAll)
        {
            _refreshAll = refreshAll;
            RowsView = new ListCollectionView(Rows) { Filter = FilterRow };
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
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }
            if (item is not RoomRow row)
            {
                return false;
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
            var confirm = MessageBox.Show(
                $"Xoá phòng {row.Room.RoomNumber}?\n\nPhòng đã có lịch sử đặt sẽ chuyển sang ngừng dùng thay vì xoá hẳn.",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
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
