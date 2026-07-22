using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>Dong hien thi loai phong.</summary>
    public class RoomTypeRow
    {
        public RoomType RoomType { get; }
        public int RoomCount => RoomType.Rooms?.Count ?? 0;
        public string ActiveText => RoomType.IsActive ? "Đang dùng" : "Ngừng dùng";

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
            set => SetProperty(ref _isLoading, value);
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public RoomTypeListViewModel(Func<Task> refreshAll)
        {
            _refreshAll = refreshAll;
            AddCommand = new RelayCommand(_ => OpenEditDialog(null));
            EditCommand = new RelayCommand(p => OpenEditDialog(p as RoomTypeRow));
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);
        }

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
