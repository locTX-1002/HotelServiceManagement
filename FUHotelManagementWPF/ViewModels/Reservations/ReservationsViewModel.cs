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
using FUHotelManagementWPF.ViewModels.Rooms;
using Services;

namespace FUHotelManagementWPF.ViewModels.Reservations
{
    /// <summary>Module Đặt phòng: danh sách D3 master-detail + lọc trạng thái + tạo/sửa/xác nhận/huỷ/no-show.</summary>
    public class ReservationsViewModel : ViewModelBase
    {
        private readonly IReservationService _service = new ReservationService();

        public ObservableCollection<ReservationRow> Rows { get; } = [];
        public ICollectionView RowsView { get; }

        public List<ReservationStatusFilter> StatusFilters { get; } =
        [
            new("Tất cả", null),
            new("Chờ xác nhận", ReservationStatus.Pending),
            new("Đã xác nhận", ReservationStatus.Confirmed),
            new("Đã check-in", ReservationStatus.CheckedIn),
            new("Hoàn tất", ReservationStatus.Completed),
            new("Đã huỷ", ReservationStatus.Cancelled),
            new("Không đến", ReservationStatus.NoShow),
        ];

        private ReservationStatusFilter _selectedFilter;
        public ReservationStatusFilter SelectedFilter
        {
            get => _selectedFilter;
            set { if (SetProperty(ref _selectedFilter, value)) { RowsView.Refresh(); } }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) { RowsView.Refresh(); } }
        }

        private ReservationRow? _selectedRow;
        public ReservationRow? SelectedRow
        {
            get => _selectedRow;
            set { if (SetProperty(ref _selectedRow, value)) { OnPropertyChanged(nameof(HasSelection)); } }
        }
        public bool HasSelection => _selectedRow != null;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }
        public bool IsEmpty => !IsLoading && Rows.Count == 0;

        public string TotalText => $"{Rows.Count} đặt phòng";

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public AsyncRelayCommand ConfirmCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public AsyncRelayCommand NoShowCommand { get; }

        public ReservationsViewModel()
        {
            RowsView = new ListCollectionView(Rows) { Filter = Filter };
            _selectedFilter = StatusFilters[0];
            AddCommand = new RelayCommand(_ => OpenCreateDialog());
            EditCommand = new RelayCommand(_ => OpenEditDialog());
            ConfirmCommand = new AsyncRelayCommand(_ => RunAction(r => _service.ConfirmAsync(r.Reservation.Id)));
            CancelCommand = new AsyncRelayCommand(_ => ConfirmThenCancel());
            NoShowCommand = new AsyncRelayCommand(_ => ConfirmThenNoShow());
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _service.GetAllAsync();
                var keepId = SelectedRow?.Reservation.Id;
                Rows.Clear();
                foreach (var r in list)
                {
                    Rows.Add(new ReservationRow(r));
                }
                SelectedRow = Rows.FirstOrDefault(r => r.Reservation.Id == keepId);
                OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception)
            {
                Notify.Error("Không tải được danh sách đặt phòng.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool Filter(object item)
        {
            if (item is not ReservationRow row)
            {
                return false;
            }
            if (SelectedFilter.Status is { } s && row.Status != s)
            {
                return false;
            }
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var k = SearchText.Trim().ToLower();
                return row.GuestName.ToLower().Contains(k)
                       || row.RoomNumber.ToLower().Contains(k)
                       || row.BookingCode.ToLower().Contains(k);
            }
            return true;
        }

        private async void OpenCreateDialog()
        {
            var dialog = new CreateReservationDialog(new CreateReservationDialogViewModel(null))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            };
            if (dialog.ShowDialog() == true)
            {
                await LoadAsync();
            }
        }

        private async void OpenEditDialog()
        {
            if (SelectedRow == null)
            {
                return;
            }
            var dialog = new CreateReservationDialog(new CreateReservationDialogViewModel(SelectedRow.Reservation))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            };
            if (dialog.ShowDialog() == true)
            {
                await LoadAsync();
            }
        }

        private async Task RunAction(Func<ReservationRow, Task<ServiceResult>> action)
        {
            if (SelectedRow == null)
            {
                return;
            }
            var result = await action(SelectedRow);
            if (result.Ok) { Notify.Success(result.Message); await LoadAsync(); }
            else { Notify.Error(result.Message); }
        }

        private Task ConfirmThenCancel()
        {
            if (SelectedRow == null)
            {
                return Task.CompletedTask;
            }
            var confirm = MessageBox.Show(
                $"Huỷ đặt phòng {SelectedRow.BookingCode} ({SelectedRow.GuestName})?",
                "Xác nhận huỷ", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return confirm == MessageBoxResult.Yes
                ? RunAction(r => _service.CancelAsync(r.Reservation.Id))
                : Task.CompletedTask;
        }

        private Task ConfirmThenNoShow()
        {
            if (SelectedRow == null)
            {
                return Task.CompletedTask;
            }
            var confirm = MessageBox.Show(
                $"Đánh dấu KHÔNG ĐẾN cho {SelectedRow.BookingCode} ({SelectedRow.GuestName})?\n\n" +
                "Tiền cọc (nếu có) sẽ không tự hoàn — xử lý thủ công ngoài hệ thống.",
                "Xác nhận Không đến", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return confirm == MessageBoxResult.Yes
                ? RunAction(r => _service.NoShowAsync(r.Reservation.Id))
                : Task.CompletedTask;
        }
    }
}
