using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Guests
{
    /// <summary>
    /// Module Khach hang: danh sach + tim kiem + CRUD qua dialog (theo mau RoomListViewModel).
    /// Khac module Phong o cho khong co tab con nen tu load va tu refresh sau moi thao tac ghi.
    /// </summary>
    public class GuestListViewModel : ViewModelBase
    {
        private readonly IGuestService _guestService = new GuestService();

        public ObservableCollection<GuestRow> Rows { get; } = [];
        public ICollectionView RowsView { get; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // 3 trang thai man hinh theo quy uoc: Loading / Empty / Error (loi thi hien banner + nut thu lai)
        private bool _isEmpty;
        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }

        private bool _loadFailed;
        public bool LoadFailed
        {
            get => _loadFailed;
            set => SetProperty(ref _loadFailed, value);
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
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand ReloadCommand { get; }

        public GuestListViewModel()
        {
            RowsView = new ListCollectionView(Rows) { Filter = FilterRow };
            AddCommand = new RelayCommand(_ => OpenEditDialog(null));
            EditCommand = new RelayCommand(p => OpenEditDialog(p as GuestRow));
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);
            ReloadCommand = new AsyncRelayCommand(_ => LoadAsync());
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            LoadFailed = false;
            try
            {
                var guests = await _guestService.GetAllAsync();
                Rows.Clear();
                foreach (var guest in guests)
                {
                    Rows.Add(new GuestRow(guest));
                }
                IsEmpty = Rows.Count == 0;
            }
            catch (Exception)
            {
                LoadFailed = true;
                Notify.Error("Không tải được danh sách khách hàng.");
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
            if (item is not GuestRow row)
            {
                return false;
            }
            var keyword = SearchText.Trim().ToLower();
            return row.Guest.FullName.ToLower().Contains(keyword)
                   || row.Guest.PhoneNumber.Contains(keyword)
                   || (row.Guest.Email ?? string.Empty).ToLower().Contains(keyword)
                   || (row.Guest.IdentityNumber ?? string.Empty).Contains(keyword);
        }

        private async void OpenEditDialog(GuestRow? existing)
        {
            var viewModel = new GuestEditDialogViewModel(existing?.Guest);
            var dialog = new GuestEditDialog(viewModel) { Owner = Rooms.RoomMapViewModel.ActiveWindow() };
            if (dialog.ShowDialog() == true)
            {
                await LoadAsync();
            }
        }

        private async Task DeleteAsync(object? parameter)
        {
            if (parameter is not GuestRow row)
            {
                return;
            }

            // MessageBox chi dung cho xac nhan xoa - dung quy uoc nhom
            var confirm = MessageBox.Show(
                $"Xoá khách \"{row.Guest.FullName}\"?\n\nKhách đã có lịch sử đặt phòng sẽ không xoá được (service sẽ chặn).",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await _guestService.DeleteAsync(row.Guest.Id);
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
    }
}
