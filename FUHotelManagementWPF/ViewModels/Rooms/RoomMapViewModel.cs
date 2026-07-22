using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    public class FloorGroup
    {
        public int Floor { get; init; }
        public string FloorTitle => $"Tầng {Floor}";
        public List<RoomRow> Rooms { get; init; } = [];
    }

    /// <summary>Tab so do: card phong nhom theo tang, bam card de doi trang thai van hanh.</summary>
    public class RoomMapViewModel : ViewModelBase
    {
        private readonly IRoomService _roomService = new RoomService();
        private readonly Func<Task> _refreshAll;

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

        private List<FloorGroup> _floors = [];
        public List<FloorGroup> Floors
        {
            get => _floors;
            set
            {
                if (SetProperty(ref _floors, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool IsEmpty => !IsLoading && Floors.Count == 0;

        public RelayCommand ChangeStatusCommand { get; }

        public RoomMapViewModel(Func<Task> refreshAll)
        {
            _refreshAll = refreshAll;
            ChangeStatusCommand = new RelayCommand(OpenStatusDialog);
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var rooms = await _roomService.GetAllAsync();
                Floors = rooms
                    .Where(r => r.IsActive)
                    .GroupBy(r => r.Floor)
                    .OrderBy(g => g.Key)
                    .Select(g => new FloorGroup
                    {
                        Floor = g.Key,
                        Rooms = g.Select(r => new RoomRow(r)).ToList(),
                    })
                    .ToList();
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
