using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>Dong hien thi loai phong tren card-row.</summary>
    public class RoomTypeRow
    {
        public RoomType RoomType { get; }
        public int RoomCount => RoomType.Rooms?.Count ?? 0;
        public string ActiveText => RoomType.IsActive ? "Đang dùng" : "Ngừng dùng";
        public bool IsActive => RoomType.IsActive;

        public string Thumbnail => RoomImages.Thumbnail(RoomType.Id, RoomType.TypeName);
        public string SubText => string.IsNullOrWhiteSpace(RoomType.Description)
            ? $"{RoomType.Capacity} khách"
            : RoomType.Description!;
        public string CapacityText => $"{RoomType.Capacity} khách";
        public string PriceText => $"{RoomType.BasePrice:N0} đ/đêm";
        public string RoomCountText => $"{RoomCount} phòng";
        public string? DescriptionText => RoomType.Description;
        public bool HasDescription => !string.IsNullOrWhiteSpace(RoomType.Description);

        public RoomTypeRow(RoomType roomType) => RoomType = roomType;
    }

    /// <summary>Tab loai phong: DataGrid + CRUD popup + xac nhan xoa.</summary>
    public class RoomTypeListViewModel : ViewModelBase
    {
        private readonly IRoomTypeService _roomTypeService = new RoomTypeService();
        private readonly Func<Task> _refreshAll;

        public ObservableCollection<RoomTypeRow> Rows { get; } = [];

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

        /// <summary>Phan biet "chua co loai phong nao" voi "tim khong ra".</summary>
        public string EmptyText => Rows.Count == 0
            ? "Chưa có loại phòng nào — bấm + Thêm loại phòng để tạo hạng đầu tiên."
            : "Không có loại phòng nào khớp từ khoá.";

        public ICollectionView RowsView { get; }

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

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public RoomTypeListViewModel(Func<Task> refreshAll)
        {
            _refreshAll = refreshAll;
            RowsView = new ListCollectionView(Rows) { Filter = FilterRow };
            AddCommand = new RelayCommand(_ => OpenEditDialog(null));
            EditCommand = new RelayCommand(p => OpenEditDialog(p as RoomTypeRow));
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);
        }

        private bool FilterRow(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }
            if (item is not RoomTypeRow row)
            {
                return false;
            }
            var keyword = SearchText.Trim().ToLower();
            return row.RoomType.TypeName.ToLower().Contains(keyword)
                   || (row.RoomType.Description ?? string.Empty).ToLower().Contains(keyword);
        }

        public string TotalText => $"{Rows.Count} loại phòng";

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var roomTypes = await _roomTypeService.GetAllAsync();
                Rows.Clear();
                foreach (var roomType in roomTypes)
                {
                    Rows.Add(new RoomTypeRow(roomType));
                }
                OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception)
            {
                Notify.Error("Không tải được danh sách loại phòng.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OpenEditDialog(RoomTypeRow? existing)
        {
            var viewModel = new RoomTypeEditDialogViewModel(existing?.RoomType);
            var dialog = new RoomTypeEditDialog(viewModel) { Owner = RoomMapViewModel.ActiveWindow() };
            if (dialog.ShowDialog() == true)
            {
                await _refreshAll();
            }
        }

        private async Task DeleteAsync(object? parameter)
        {
            if (parameter is not RoomTypeRow row)
            {
                return;
            }

            var confirm = MessageBox.Show(
                $"Xoá loại phòng \"{row.RoomType.TypeName}\"?\n\nLoại đang được phòng sử dụng sẽ chuyển sang ngừng dùng thay vì xoá hẳn.",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await _roomTypeService.DeleteAsync(row.RoomType.Id);
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
