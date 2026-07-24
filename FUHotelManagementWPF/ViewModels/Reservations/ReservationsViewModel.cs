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

        /// <summary>Tab "Lich phong" - cung du lieu, khac cach nhin.</summary>
        public RoomCalendarViewModel Calendar { get; }

        // Loc trang thai bang dropdown: bay 7 chip ra mot hang thi 5 cai thuong la 0,
        // chiem tron mot dong ma khong noi them duoc gi.
        public ObservableCollection<ReservationChip> Chips { get; } = [];

        private ReservationChip _selectedChip = null!;
        public ReservationChip SelectedChip
        {
            get => _selectedChip;
            set
            {
                if (value != null && !ReferenceEquals(_selectedChip, value))
                {
                    _selectedChip = value;
                    OnPropertyChanged();
                    RowsView.Refresh();
                    OnPropertyChanged(nameof(IsEmpty));
                    // Dong dang chon co the bi loc mat -> bo chon cho khoi tro nham
                    if (SelectedRow != null && !Filter(SelectedRow))
                    {
                        SelectedRow = null;
                    }
                }
            }
        }

        private void RefreshChipCounts()
        {
            foreach (var chip in Chips)
            {
                chip.Count = chip.Status == null
                    ? Rows.Count
                    : Rows.Count(r => r.Status == chip.Status);
            }
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

            Chips.Add(new ReservationChip("Tất cả", null));
            Chips.Add(new ReservationChip("Chờ xác nhận", ReservationStatus.Pending));
            Chips.Add(new ReservationChip("Đã xác nhận", ReservationStatus.Confirmed));
            Chips.Add(new ReservationChip("Đang ở", ReservationStatus.CheckedIn));
            Chips.Add(new ReservationChip("Hoàn tất", ReservationStatus.Completed));
            Chips.Add(new ReservationChip("Đã huỷ", ReservationStatus.Cancelled));
            Chips.Add(new ReservationChip("Không đến", ReservationStatus.NoShow));
            _selectedChip = Chips[0];

            // Lich phong tai lai ca hai tab de danh sach va lich khong lech nhau
            Calendar = new RoomCalendarViewModel(async () =>
            {
                await LoadAsync();
                await Calendar!.LoadAsync();
            });
            _ = Calendar.LoadAsync();

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
                RefreshChipCounts();
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
            if (_selectedChip.Status is { } s && row.Status != s)
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

        private async Task RunAction(Func<ReservationRow, Task<ServiceResult<BusinessObjects.Entities.Reservation>>> action)
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
            var ok = ConfirmDialog.Ask(
                $"Huỷ đặt phòng của {SelectedRow.GuestName}?",
                $"Đơn {SelectedRow.BookingCode} sẽ chuyển sang Đã huỷ và phòng {SelectedRow.RoomNumber} được giải phóng.",
                "Đơn đã huỷ không khôi phục lại được, muốn đặt lại thì tạo đơn mới.",
                "Huỷ đơn", isDanger: true);
            return ok
                ? RunAction(r => _service.CancelAsync(r.Reservation.Id))
                : Task.CompletedTask;
        }

        private Task ConfirmThenNoShow()
        {
            if (SelectedRow == null)
            {
                return Task.CompletedTask;
            }
            // Khach khong den thi mat coc - noi thang so tien de le tan tra loi khach duoc ngay.
            var deposit = SelectedRow.Reservation.DepositAmount;
            var ok = ConfirmDialog.Ask(
                $"Ghi nhận {SelectedRow.GuestName} không đến?",
                $"Phòng {SelectedRow.RoomNumber} sẽ được trả về trạng thái trống để bán cho khách khác.",
                deposit > 0 ? $"Khách mất cọc {deposit:N0} đ — không hoàn lại." : null,
                "Ghi không đến", isDanger: true);
            return ok
                ? RunAction(r => _service.NoShowAsync(r.Reservation.Id))
                : Task.CompletedTask;
        }
    }
}
