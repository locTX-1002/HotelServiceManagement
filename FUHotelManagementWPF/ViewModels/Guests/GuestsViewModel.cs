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

        public GuestsViewModel()
        {
            AddCommand = new RelayCommand(_ => OpenDialog(null));
            EditCommand = new RelayCommand(_ => OpenDialog(SelectedRow?.Guest));
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
    }
}
