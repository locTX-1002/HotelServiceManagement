using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Rooms;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Guests
{
    public class GuestRow
    {
        public Guest Guest { get; }
        public GuestAccount? Account { get; }

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

        public bool HasAccount => Account != null;
        public bool IsAccountActive => Account?.IsActive == true;
        public string AccountStatusText => Account == null
            ? "Chưa có tài khoản"
            : Account.IsActive ? "Tài khoản hoạt động" : "Tài khoản đã khoá";
        public string AccountBadgeText => Account == null
            ? "Chưa cấp"
            : Account.IsActive ? "Đang hoạt động" : "Đã khoá";

        public bool CanToggleAccount => HasAccount;
        public string ToggleAccountText => IsAccountActive ? "Khoá tài khoản" : "Mở khoá tài khoản";
        public bool CanResetPassword => HasAccount;

        public GuestRow(Guest guest, GuestAccount? account = null)
        {
            Guest = guest;
            Account = account;
        }
    }

    public class GuestsViewModel : ViewModelBase
    {
        private readonly IGuestService _service = new GuestService();
        private readonly IGuestAccountService _accountService = new GuestAccountService();

        public bool CanManageGuests => AuthorizationPolicy.CanOperateFrontDesk;

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
        public AsyncRelayCommand ToggleAccountActiveCommand { get; }
        public RelayCommand ResetAccountPasswordCommand { get; }

        public GuestsViewModel()
        {
            AddCommand = new RelayCommand(_ => OpenDialog(null));
            EditCommand = new RelayCommand(_ => OpenDialog(SelectedRow?.Guest));
            ActivateAccountCommand = new RelayCommand(_ => OpenActivateDialog());
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);
            ToggleAccountActiveCommand = new AsyncRelayCommand(ToggleAccountActiveAsync);
            ResetAccountPasswordCommand = new RelayCommand(_ => OpenResetPasswordDialog());
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var guests = await _service.SearchAsync(SearchText);

                var accountResult = await _accountService.GetAllAsync();
                var accountMap = new Dictionary<int, GuestAccount>();
                if (accountResult.Ok && accountResult.Data != null)
                {
                    foreach (var acc in accountResult.Data)
                        accountMap[acc.GuestId] = acc;
                }

                var keepId = SelectedRow?.Guest.Id;
                Rows.Clear();
                foreach (var g in guests)
                {
                    accountMap.TryGetValue(g.Id, out var account);
                    Rows.Add(new GuestRow(g, account));
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

        private async Task DeleteAsync(object? _)
        {
            if (SelectedRow == null)
            {
                return;
            }

            var confirmed = ConfirmDialog.Ask(
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

            _ = LoadAsync();
        }

        private async Task ToggleAccountActiveAsync(object? _)
        {
            if (SelectedRow?.Account == null) return;

            var willLock = SelectedRow.IsAccountActive;
            var question = willLock
                ? $"Khoá tài khoản khách \"{SelectedRow.Guest.FullName}\"?\n\nKhách sẽ không đăng nhập được cho tới khi được mở khoá."
                : $"Mở khoá tài khoản khách \"{SelectedRow.Guest.FullName}\"?";
            var caption = willLock ? "Khoá tài khoản" : "Mở khoá tài khoản";
            var owner = RoomMapViewModel.ActiveWindow();

            var answer = owner == null
                ? MessageBox.Show(question, caption, MessageBoxButton.YesNo, MessageBoxImage.Question)
                : MessageBox.Show(owner, question, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes) return;

            var result = await _accountService.SetActiveAsync(SelectedRow.Guest.Id, !willLock);
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

        private void OpenResetPasswordDialog()
        {
            if (SelectedRow?.Account == null) return;

            var dialog = new GuestResetPasswordDialog(
                new GuestResetPasswordDialogViewModel(SelectedRow.Guest))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            };
            dialog.ShowDialog();
        }
    }
}
