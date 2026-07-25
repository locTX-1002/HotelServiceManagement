using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.GuestPortal
{
    /// <summary>
    /// Man "Đặt phòng của tôi" - man chinh cua khu danh cho khach tu dang nhap.
    /// Chi goi GetMyReservationsAsync (ham khong kiem quyen theo vai tro), vi khach
    /// co RoleName rong nen moi ham nghiep vu khac deu bi chan.
    /// </summary>
    public class MyReservationsViewModel : ViewModelBase
    {
        private readonly IReservationService _service = new ReservationService();

        public ObservableCollection<MyReservationRow> Rows { get; } = [];

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set { if (SetProperty(ref _hasError, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>Rong that su: khong phai dang tai, khong phai loi, ma van khong co don nao.</summary>
        public bool IsEmpty => !IsLoading && !HasError && Rows.Count == 0;

        private int _upcomingCount;
        private int _stayingCount;

        public string TotalText => $"{Rows.Count} đơn";
        public string UpcomingText => $"{_upcomingCount} đơn";
        public string StayingText => $"{_stayingCount} đơn";

        public AsyncRelayCommand ReloadCommand { get; }

        public MyReservationsViewModel()
        {
            ReloadCommand = new AsyncRelayCommand(ReloadAsync);
            _ = LoadAsync();
        }

        /// <summary>Tai danh sach don cua chinh khach dang dang nhap. Tra ve true neu tai duoc.</summary>
        public async Task<bool> LoadAsync()
        {
            IsLoading = true;
            try
            {
                var result = await _service.GetMyReservationsAsync();
                if (!result.Ok)
                {
                    ShowError(result.Message);
                    return false;
                }

                var rows = (result.Data ?? []).Select(r => new MyReservationRow(r)).ToList();

                // Don dang o / sap toi len tren (gan ngay nhan phong nhat truoc),
                // don da tra phong hoac da huy xuong duoi (moi nhat truoc).
                var dangHieuLuc = rows.Where(r => r.IsActive)
                    .OrderBy(r => r.SortRank)
                    .ThenBy(r => r.CheckInDate);
                var daKetThuc = rows.Where(r => !r.IsActive)
                    .OrderByDescending(r => r.CheckInDate);

                Rows.Clear();
                foreach (var row in dangHieuLuc.Concat(daKetThuc))
                {
                    Rows.Add(row);
                }

                _upcomingCount = rows.Count(r => r.IsUpcoming);
                _stayingCount = rows.Count(r => r.IsStaying);

                HasError = false;
                ErrorMessage = string.Empty;
                RaiseSummaryChanged();
                return true;
            }
            catch (Exception)
            {
                ShowError("Không tải được danh sách đặt phòng. Kiểm tra kết nối rồi thử lại.");
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Nut "Tải lại": bao thanh cong de khach biet du lieu vua duoc lam moi;
        // that bai da co banner loi nen khong bao them.
        private async Task ReloadAsync(object? _)
        {
            if (await LoadAsync())
            {
                Notify.Success("Đã tải lại danh sách đặt phòng.");
            }
        }

        // Loi thi xoa danh sach cu di, tranh viec khach nhin so lieu cu ma tuong la moi.
        private void ShowError(string message)
        {
            Rows.Clear();
            _upcomingCount = 0;
            _stayingCount = 0;
            ErrorMessage = string.IsNullOrWhiteSpace(message)
                ? "Không tải được danh sách đặt phòng."
                : message;
            HasError = true;
            RaiseSummaryChanged();
        }

        private void RaiseSummaryChanged()
        {
            OnPropertyChanged(nameof(TotalText));
            OnPropertyChanged(nameof(UpcomingText));
            OnPropertyChanged(nameof(StayingText));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
}
