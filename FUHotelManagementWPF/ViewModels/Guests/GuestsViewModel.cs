using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Rooms;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Guests
{
    /// <summary>Dòng khách hàng cho card-row + panel chi tiết.</summary>
    public class GuestRow
    {
        public Guest Guest { get; }

        public string Initial => string.IsNullOrWhiteSpace(Guest.FullName)
            ? "?" : Guest.FullName.Trim()[..1].ToUpper();
        public string SubText => $"{Guest.PhoneNumber}"
            + (string.IsNullOrWhiteSpace(Guest.IdentityNumber) ? "" : $" · CCCD {Guest.IdentityNumber}");
        public string EmailText => string.IsNullOrWhiteSpace(Guest.Email) ? "—" : Guest.Email!;
        public int ReservationCount => Guest.Reservations?.Count ?? 0;
        public string ReservationText => $"{ReservationCount} lần đặt";

        public GuestTag Tag => Guest.Tag;
        public bool HasTag => Guest.Tag != GuestTag.None;
        public string TagText => Guest.Tag switch
        {
            GuestTag.Vip => "VIP",
            GuestTag.Blacklisted => "Cảnh báo",
            _ => string.Empty,
        };
        public string? TagNote => Guest.TagNote;
        public bool HasTagNote => !string.IsNullOrWhiteSpace(Guest.TagNote);

        public GuestRow(Guest guest) => Guest = guest;
    }

    /// <summary>Module Khách hàng: danh sách D3 master-detail + tìm kiếm + CRUD popup.</summary>
    public class GuestsViewModel : ViewModelBase
    {
        private readonly IGuestService _service = new GuestService();

        public ObservableCollection<GuestRow> Rows { get; } = [];

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) { _ = LoadAsync(); } }
        }

        private GuestRow? _selectedRow;
        public GuestRow? SelectedRow
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

        public string TotalText => $"{Rows.Count} khách hàng";

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand ActivateAccountCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public GuestsViewModel()
        {
            AddCommand = new RelayCommand(_ => OpenDialog(null));
            EditCommand = new RelayCommand(_ => OpenDialog(SelectedRow?.Guest));
            ActivateAccountCommand = new RelayCommand(_ => OpenActivateDialog());
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _service.SearchAsync(SearchText);
                var keepId = SelectedRow?.Guest.Id;
                Rows.Clear();
                foreach (var g in list)
                {
                    Rows.Add(new GuestRow(g));
                }
                SelectedRow = Rows.FirstOrDefault(r => r.Guest.Id == keepId);
                OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception)
            {
                Notify.Error("Không tải được danh sách khách hàng.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OpenDialog(Guest? existing)
        {
            var dialog = new GuestEditDialog(new GuestEditDialogViewModel(existing))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            };
            if (dialog.ShowDialog() == true)
            {
                await LoadAsync();
            }
        }

        // Xoa khach dang chon - service tu chan neu khach da co lich su dat phong (hien nguyen Message).
        private async System.Threading.Tasks.Task DeleteAsync(object? _)
        {
            if (SelectedRow == null)
            {
                return;
            }

            var confirmed = Views.Dialogs.ConfirmDialog.Ask(
                $"Xoá khách \"{SelectedRow.Guest.FullName}\"?",
                "Hồ sơ sẽ bị xoá hẳn khỏi hệ thống.",
                "Khách đã có lịch sử đặt phòng sẽ không xoá được — hệ thống sẽ báo lại.",
                "Xoá khách",
                isDanger: true);
            if (!confirmed)
            {
                return;
            }

            var result = await _service.DeleteAsync(SelectedRow.Guest.Id);
            if (result.Ok)
            {
                Notify.Success(result.Message);
                await LoadAsync();
            }
            else
            {
                Notify.Error(result.Message);
            }
        }

        // Kich hoat tai khoan dat phong cho khach dang chon (dang nhap bang SDT).
        // Da co tai khoan hay chua do service tu kiem tra va bao Message.
        private void OpenActivateDialog()
        {
            if (SelectedRow == null)
            {
                return;
            }

            new ActivateAccountDialog(new ActivateAccountDialogViewModel(SelectedRow.Guest))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            }.ShowDialog();
        }
    }
}
