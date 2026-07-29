using BusinessObjects;
using BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Reports
{
    /// <summary>
    /// Module Bao cao: chon khoang ngay -> 6 the tong quan + bang doanh thu theo ngay
    /// + cong suat phong, kem nut xuat CSV. Chi Admin/Manager duoc xem.
    /// </summary>
    public class ReportViewModel : ViewModelBase
    {
        private readonly IReportService _service = new ReportService();

        // Be ngang toi da cua thanh cong suat (px). VM tu quy % ra pixel de XAML
        // khong can viet converter rieng - dung dung 1 thanh nen + 1 thanh to mau.
        private const double BarTrackWidth = 260;

        /// <summary>Doanh thu tung ngay - da sap xep ngay giam dan truoc khi do vao day.</summary>
        public ObservableCollection<RevenueByDay> Rows { get; } = [];

        // ---- Quyen xem: doc 1 lan luc mo man, khong doi giua chung phien ----
        public bool HasPermission { get; } = AuthorizationPolicy.CanViewReports;
        public bool NoPermission => !HasPermission;

        // ---- Khoang ngay ----
        private DateTime _fromDate = DateTime.Today.AddDays(-6);
        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                {
                    OnPropertyChanged(nameof(RangeText));
                    if (HasPermission) { _ = LoadAsync(false); }
                }
            }
        }

        private DateTime _toDate = DateTime.Today;
        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                {
                    OnPropertyChanged(nameof(RangeText));
                    if (HasPermission) { _ = LoadAsync(false); }
                }
            }
        }

        public string RangeText => $"Từ {FromDate:dd/MM/yyyy} đến {ToDate:dd/MM/yyyy}";

        // ---- 3 trang thai man hinh ----
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

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool IsEmpty => !IsLoading && !HasError && HasPermission && Rows.Count == 0;

        // ---- 6 the tong quan ----
        private decimal _roomRevenue;
        public decimal RoomRevenue
        {
            get => _roomRevenue;
            private set => SetProperty(ref _roomRevenue, value);
        }

        private decimal _serviceRevenue;
        public decimal ServiceRevenue
        {
            get => _serviceRevenue;
            private set => SetProperty(ref _serviceRevenue, value);
        }

        private decimal _surchargeRevenue;
        public decimal SurchargeRevenue
        {
            get => _surchargeRevenue;
            private set => SetProperty(ref _surchargeRevenue, value);
        }

        private decimal _discountAmount;
        public decimal DiscountAmount
        {
            get => _discountAmount;
            private set { if (SetProperty(ref _discountAmount, value)) { OnPropertyChanged(nameof(DiscountText)); } }
        }

        // Giam gia hien kem dau tru cho de doc, nhung khong hien "-0 d" khi khong giam gi
        public string DiscountText => DiscountAmount > 0 ? $"-{DiscountAmount:N0} đ" : "0 đ";

        private decimal _invoiceRevenue;
        public decimal InvoiceRevenue
        {
            get => _invoiceRevenue;
            private set => SetProperty(ref _invoiceRevenue, value);
        }

        private decimal _collectedAmount;
        public decimal CollectedAmount
        {
            get => _collectedAmount;
            private set => SetProperty(ref _collectedAmount, value);
        }

        // ---- Cong suat phong (chup tai thoi diem xem, khong theo khoang ngay) ----
        private int _totalRooms;
        public int TotalRooms
        {
            get => _totalRooms;
            private set => SetProperty(ref _totalRooms, value);
        }

        private int _availableRooms;
        public int AvailableRooms
        {
            get => _availableRooms;
            private set => SetProperty(ref _availableRooms, value);
        }

        private int _reservedRooms;
        public int ReservedRooms
        {
            get => _reservedRooms;
            private set => SetProperty(ref _reservedRooms, value);
        }

        private int _occupiedRooms;
        public int OccupiedRooms
        {
            get => _occupiedRooms;
            private set => SetProperty(ref _occupiedRooms, value);
        }

        private decimal _occupancyRate;
        public decimal OccupancyRate
        {
            get => _occupancyRate;
            private set
            {
                if (SetProperty(ref _occupancyRate, value))
                {
                    OnPropertyChanged(nameof(OccupancyBarWidth));
                }
            }
        }

        /// <summary>Be rong phan da to cua thanh cong suat (px), cat trong khoang 0..BarTrackWidth.</summary>
        public double OccupancyBarWidth
        {
            get
            {
                var ratio = (double)OccupancyRate / 100.0;
                if (ratio < 0) { ratio = 0; }
                if (ratio > 1) { ratio = 1; }
                return Math.Round(BarTrackWidth * ratio);
            }
        }

        public double OccupancyBarTrackWidth => BarTrackWidth;

        public string DayCountText => $"{Rows.Count} ngày có phát sinh";

        // ---- Chi tiet hoa don theo ngay ----
        private List<Invoice> _allInvoices = [];

        private RevenueByDay? _selectedRow;
        public RevenueByDay? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (SetProperty(ref _selectedRow, value))
                {
                    OnPropertyChanged(nameof(HasSelectedDay));
                    OnPropertyChanged(nameof(SelectedDayText));
                    RefreshDayInvoices();
                }
            }
        }

        public bool HasSelectedDay => _selectedRow != null;
        public string SelectedDayText => _selectedRow != null
            ? $"Hoá đơn ngày {_selectedRow.Date:dd/MM/yyyy}"
            : string.Empty;

        public ObservableCollection<InvoiceRow> DayInvoices { get; } = [];

        private void RefreshDayInvoices()
        {
            DayInvoices.Clear();
            if (_selectedRow == null) return;
            var date = _selectedRow.Date.Date;
            var filtered = _allInvoices.Where(x => x.InvoiceDate.Date == date)
                .OrderByDescending(x => x.TotalAmount).ToList();
            foreach (var inv in filtered)
            {
                DayInvoices.Add(new InvoiceRow(inv));
            }
        }

        public AsyncRelayCommand LoadCommand { get; }
        public AsyncRelayCommand ExportCsvCommand { get; }
        public RelayCommand Last7DaysCommand { get; }
        public RelayCommand Last30DaysCommand { get; }
        public RelayCommand ThisMonthCommand { get; }

        public ReportViewModel()
        {
            LoadCommand = new AsyncRelayCommand(_ => LoadAsync(true));
            ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync);
            Last7DaysCommand = new RelayCommand(_ => SetRange(DateTime.Today.AddDays(-6), DateTime.Today));
            Last30DaysCommand = new RelayCommand(_ => SetRange(DateTime.Today.AddDays(-29), DateTime.Today));
            ThisMonthCommand = new RelayCommand(_ => SetRange(DateTime.Today.AddMonths(-3), DateTime.Today));

            // Khong co quyen thi khong goi service (service cung se tu choi) - chi hien thong bao
            if (HasPermission)
            {
                _ = LoadAsync(false);
            }
        }

        // Chip nhanh: doi ca 2 dau ngay roi tai lai luon cho khoi bam them nut Xem
        private void SetRange(DateTime from, DateTime to)
        {
            _fromDate = from;
            _toDate = to;
            OnPropertyChanged(nameof(FromDate));
            OnPropertyChanged(nameof(ToDate));
            OnPropertyChanged(nameof(RangeText));
            _ = LoadAsync(true);
        }

        public async Task LoadAsync(bool showNotification = true)
        {
            if (!HasPermission)
            {
                return;
            }

            // Chan tu UI truoc khi goi DB: sai khoang ngay thi bao ngay, khong ton 1 vong query
            if (ToDate.Date < FromDate.Date)
            {
                Notify.Warning("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var revenue = await _service.GetRevenueAsync(FromDate, ToDate);
                if (!revenue.Ok || revenue.Data == null)
                {
                    ShowError(revenue.Message);
                    return;
                }

                Apply(revenue.Data);

                var occupancy = await _service.GetOccupancyAsync();
                if (occupancy.Ok && occupancy.Data != null)
                {
                    Apply(occupancy.Data);
                }
                else
                {
                    Notify.Error(occupancy.Message);
                }

                if (showNotification)
                {
                    Notify.Success($"Đã tải báo cáo ({FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy})");
                }
            }
            catch (Exception)
            {
                ShowError("Không tải được báo cáo. Kiểm tra kết nối SQL Server rồi thử lại.");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(DayCountText));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        private void Apply(RevenueReport report)
        {
            RoomRevenue = report.RoomRevenue;
            ServiceRevenue = report.ServiceRevenue;
            SurchargeRevenue = report.SurchargeRevenue;
            DiscountAmount = report.DiscountAmount;
            InvoiceRevenue = report.InvoiceRevenue;
            CollectedAmount = report.CollectedAmount;

            _allInvoices = report.Invoices.ToList();
            SelectedRow = null;

            Rows.Clear();
            foreach (var day in report.ByDay)
            {
                Rows.Add(day);
            }
        }

        private void Apply(OccupancyReport report)
        {
            TotalRooms = report.TotalRooms;
            AvailableRooms = report.AvailableRooms;
            ReservedRooms = report.ReservedRooms;
            OccupiedRooms = report.OccupiedRooms;
            OccupancyRate = report.OccupancyRate;
        }

        // Loi thi xoa so lieu cu di, tranh nguoi dung tuong so cu la so moi
        private void ShowError(string message)
        {
            Rows.Clear();
            RoomRevenue = ServiceRevenue = SurchargeRevenue = 0;
            DiscountAmount = InvoiceRevenue = CollectedAmount = 0;
            ErrorMessage = string.IsNullOrWhiteSpace(message)
                ? "Không tải được báo cáo."
                : message;
        }

        private async Task ExportCsvAsync(object? _)
        {
            if (!HasPermission)
            {
                Notify.Warning("Bạn không có quyền xem báo cáo.");
                return;
            }
            if (ToDate.Date < FromDate.Date)
            {
                Notify.Warning("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
                return;
            }

            var result = await _service.ExportRevenueCsvAsync(FromDate, ToDate);
            if (!result.Ok || result.Data == null)
            {
                Notify.Error(result.Message);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"bao-cao-doanh-thu-{FromDate:yyyyMMdd}-{ToDate:yyyyMMdd}.csv",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                // UTF8 CO BOM (true): thieu BOM thi Excel mo ra tieng Viet bi loi font
                await File.WriteAllTextAsync(dialog.FileName, result.Data, new UTF8Encoding(true));
                Notify.Success($"Đã xuất báo cáo: {dialog.FileName}");
            }
            catch (Exception)
            {
                Notify.Error("Không ghi được file CSV. Chọn thư mục khác rồi thử lại.");
            }
        }
    }

    public class InvoiceRow
    {
        private static readonly Dictionary<BusinessObjects.Enums.InvoiceStatus, string> StatusLabels = new()
        {
            [BusinessObjects.Enums.InvoiceStatus.Unpaid] = "Chưa thanh toán",
            [BusinessObjects.Enums.InvoiceStatus.PartiallyPaid] = "Thanh toán 1 phần",
            [BusinessObjects.Enums.InvoiceStatus.Paid] = "Đã thanh toán",
            [BusinessObjects.Enums.InvoiceStatus.Cancelled] = "Đã huỷ",
        };

        public InvoiceRow(Invoice inv)
        {
            Id = inv.Id;
            RoomNumber = inv.Stay?.Reservation?.Room?.RoomNumber ?? "—";
            InvoiceDate = inv.InvoiceDate;
            RoomCharge = inv.RoomCharge;
            ServiceCharge = inv.ServiceCharge;
            SurchargeAmount = inv.SurchargeAmount;
            DiscountAmount = inv.DiscountAmount;
            TotalAmount = inv.TotalAmount;
            StatusText = StatusLabels.GetValueOrDefault(inv.Status, inv.Status.ToString());
        }

        public int Id { get; }
        public string RoomNumber { get; }
        public DateTime InvoiceDate { get; }
        public decimal RoomCharge { get; }
        public decimal ServiceCharge { get; }
        public decimal SurchargeAmount { get; }
        public decimal DiscountAmount { get; }
        public decimal TotalAmount { get; }
        public string StatusText { get; }
    }
}
