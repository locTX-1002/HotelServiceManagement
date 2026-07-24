using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.CheckInOut
{
    /// <summary>Mot khoan trong bang tam tinh tien phong luc gia han.</summary>
    public class ChargeLine
    {
        public string Label { get; init; } = string.Empty;
        public string RangeText { get; init; } = string.Empty;
        public int Nights { get; init; }
        public string NightsText { get; init; } = string.Empty;
        public string AmountText { get; init; } = string.Empty;
        public bool IsOverdue { get; init; }
    }

    /// <summary>
    /// Gia han luu tru: khach dang o muon o them thi doi ngay tra ngay tren don,
    /// khong phai check-out roi tao don moi (lam hong lich su va sai doanh thu).
    /// </summary>
    public class ExtendStayDialogViewModel : ViewModelBase
    {
        private readonly IStayService _service = new StayService();
        private readonly Stay _stay;
        private readonly decimal _basePrice;

        public string RoomText { get; }
        public string GuestText { get; }
        public string CurrentText { get; }

        private DateTime _newCheckOut;
        public DateTime NewCheckOut
        {
            get => _newCheckOut;
            set
            {
                if (SetProperty(ref _newCheckOut, value))
                {
                    RebuildCharges();
                    OnPropertyChanged(nameof(DiffText));
                    OnPropertyChanged(nameof(IsShortening));
                }
            }
        }

        /// <summary>
        /// Tam tinh tien phong, tach ra tung khoan de le tan doc duoc dang thu tien cho
        /// nhung dem nao. Khach o qua han thi dem qua han VAN tinh tien - va phai nhin
        /// thay dong do, khong nhet chung vao mot con so tong roi de nguoi doc tu doan.
        /// </summary>
        public ObservableCollection<ChargeLine> ChargeLines { get; } = [];

        private int TotalNights => ChargeLines.Sum(line => line.Nights);
        public string TotalNightsText => $"{TotalNights} đêm";
        public string TotalAmountText => $"{TotalNights * _basePrice:N0} đ";

        /// <summary>
        /// Chia khoang [ngay vao that -> ngay tra moi] thanh 3 doan noi duoi nhau,
        /// khong chong lan nen cong lai dung bang tong so dem:
        ///   1. Da o theo don : ngay vao   -> han tra tren don
        ///   2. Qua han       : han tra    -> hom nay
        ///   3. O them        : hom nay    -> ngay tra moi
        /// Doan nao bi ngay tra moi cat cho am thi bo han, nho vay le tan chon ngay
        /// som hon cung khong ra so dem am.
        /// </summary>
        private void RebuildCharges()
        {
            ChargeLines.Clear();

            var arrived = _stay.ActualCheckIn.Date;
            var planned = _stay.Reservation.CheckOutDate.Date;
            var today = DateTime.Today;
            var target = NewCheckOut.Date;

            AddCharge("Đã ở theo đơn", arrived, Earlier(planned, target), false);
            AddCharge("Quá hạn", planned, Earlier(today, target), true);
            AddCharge("Ở thêm", Later(planned, today), target, false);

            OnPropertyChanged(nameof(TotalNightsText));
            OnPropertyChanged(nameof(TotalAmountText));
        }

        private void AddCharge(string label, DateTime from, DateTime to, bool isOverdue)
        {
            var nights = (to - from).Days;
            if (nights <= 0)
            {
                return;
            }
            ChargeLines.Add(new ChargeLine
            {
                Label = label,
                RangeText = $"{from:dd/MM} → {to:dd/MM}",
                Nights = nights,
                NightsText = $"{nights} đêm",
                AmountText = $"{nights * _basePrice:N0} đ",
                IsOverdue = isOverdue,
            });
        }

        private static DateTime Earlier(DateTime a, DateTime b) => a < b ? a : b;
        private static DateTime Later(DateTime a, DateTime b) => a > b ? a : b;

        public string DiffText
        {
            get
            {
                var diff = (NewCheckOut.Date - _stay.Reservation.CheckOutDate.Date).Days;
                return diff switch
                {
                    > 0 => $"Ở thêm {diff} đêm so với đơn hiện tại",
                    < 0 => $"Rút ngắn {-diff} đêm so với đơn hiện tại",
                    _ => "Trùng với ngày trả hiện tại — chọn ngày khác",
                };
            }
        }

        public bool IsShortening => NewCheckOut.Date < _stay.Reservation.CheckOutDate.Date;

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { if (SetProperty(ref _errorMessage, value)) { OnPropertyChanged(nameof(HasError)); } }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public RelayCommand PickNightsCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }

        /// <summary>Dialog dong voi ket qua true khi luu thanh cong.</summary>
        public event Action<bool>? RequestClose;

        public ExtendStayDialogViewModel(Stay stay)
        {
            _stay = stay;
            _basePrice = stay.Reservation.Room?.RoomType?.BasePrice ?? 0;

            RoomText = $"Phòng {stay.Reservation.Room?.RoomNumber} · {stay.Reservation.Room?.RoomType?.TypeName}";
            GuestText = stay.Reservation.Guest?.FullName ?? string.Empty;
            CurrentText = $"Vào {stay.ActualCheckIn:dd/MM/yyyy} · đơn hẹn trả {stay.Reservation.CheckOutDate:dd/MM/yyyy}";

            // Mac dinh de nghi them 1 dem so voi ngay tra hien tai
            _newCheckOut = stay.Reservation.CheckOutDate.Date.AddDays(1);
            RebuildCharges();

            PickNightsCommand = new RelayCommand(p =>
            {
                if (p is string text && int.TryParse(text, out var days))
                {
                    NewCheckOut = stay.Reservation.CheckOutDate.Date.AddDays(days);
                }
            });

            SaveCommand = new AsyncRelayCommand(SaveAsync);
        }

        private async Task SaveAsync(object? _)
        {
            ErrorMessage = null;
            var result = await _service.ExtendAsync(_stay.Id, NewCheckOut);
            if (result.Ok)
            {
                Notify.Success(result.Message);
                RequestClose?.Invoke(true);
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
    }
}
