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
    public class RoomMapViewModel : ViewModelBase
    {
        private readonly IRoomService _roomService = new RoomService();
        private readonly Func<Task> _refreshAll;

        public ObservableCollection<RoomRow> Rooms { get; } = [];

        /// <summary>Ban nhom theo GroupTitle - moi tang 1 header tu dong, khong xep tay.</summary>
        public ICollectionView RoomsView { get; }

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

        public bool IsEmpty => !IsLoading && Rooms.Count == 0;

        public RelayCommand ChangeStatusCommand { get; }

        public RoomMapViewModel(Func<Task> refreshAll)
        {
            _refreshAll = refreshAll;
            RoomsView = new ListCollectionView(Rooms);
            RoomsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RoomRow.GroupTitle)));
            ChangeStatusCommand = new RelayCommand(OpenStatusDialog);
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var rooms = (await _roomService.GetAllAsync())
                    .Where(r => r.IsActive)
                    .ToList();

                // Header nhom: "TẦNG {n} · {cac loai phong tren tang do}"
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
                OnPropertyChanged(nameof(IsEmpty));
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

        private async void OpenStatusDialog(object? parameter)
        {
            if (parameter is not RoomRow row)
            {
                return;
            }

            var viewModel = new RoomStatusDialogViewModel(row.Room);
            var dialog = new RoomStatusDialog(viewModel) { Owner = ActiveWindow() };
            if (dialog.ShowDialog() == true)
            {
                await _refreshAll();
            }
        }

        internal static Window? ActiveWindow()
            => Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
               ?? Application.Current.MainWindow;
    }
}
