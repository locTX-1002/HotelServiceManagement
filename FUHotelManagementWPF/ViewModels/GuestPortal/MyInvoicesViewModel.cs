using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Enums;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.GuestPortal
{
    /// <summary>
    /// Man "Hoa don cua toi" trong khu danh cho khach. Khach khong co vai tro nhan vien
    /// nen chi goi duoc hai ham khong kiem quyen: GetMyReservationsAsync (tu loc theo
    /// phien khach) va GetByStayAsync.
    /// </summary>
    public class MyInvoicesViewModel : ViewModelBase
    {
        private readonly IReservationService _reservationService = new ReservationService();
        private readonly IInvoiceService _invoiceService = new InvoiceService();

        public ObservableCollection<MyInvoiceRow> Rows { get; } = [];

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        // Rong that su khac voi dang tai va khac voi loi - ba trang thai khong duoc de lan nhau.
        public bool IsEmpty => !IsLoading && !HasError && Rows.Count == 0;

        public bool HasRows => Rows.Count > 0;

        public string TotalCountText => $"{Rows.Count} hoá đơn";

        /// <summary>Tong tien cua cac hoa don da thanh toan xong.</summary>
        public decimal PaidTotal => Rows.Where(r => r.Invoice.Status == InvoiceStatus.Paid)
                                        .Sum(r => r.Invoice.TotalAmount);

        public AsyncRelayCommand ReloadCommand { get; }

        public MyInvoicesViewModel()
        {
            ReloadCommand = new AsyncRelayCommand(_ => LoadAsync());
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var result = await _reservationService.GetMyReservationsAsync();
                if (!result.Ok)
                {
                    ErrorMessage = result.Message;
                    Notify.Error(result.Message);
                    return;
                }

                Rows.Clear();
                foreach (var reservation in result.Data ?? [])
                {
                    // Chua nhan phong thi chua co ky luu tru, ma chua co ky luu tru thi
                    // le tan chua lap duoc hoa don - bo qua don do.
                    if (reservation.Stay == null)
                    {
                        continue;
                    }

                    var invoice = await _invoiceService.GetByStayAsync(reservation.Stay.Id);
                    if (invoice != null)
                    {
                        Rows.Add(new MyInvoiceRow(reservation, invoice));
                    }
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không tải được hoá đơn. Kiểm tra kết nối rồi thử lại.";
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasRows));
                OnPropertyChanged(nameof(TotalCountText));
                OnPropertyChanged(nameof(PaidTotal));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }
}
