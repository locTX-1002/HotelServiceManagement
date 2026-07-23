using System;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.CheckInOut
{
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
                    OnPropertyChanged(nameof(PreviewText));
                    OnPropertyChanged(nameof(DiffText));
                    OnPropertyChanged(nameof(IsShortening));
                }
            }
        }

        /// <summary>Tong tien phong neu tra vao ngay dang chon - de le tan bao gia ngay cho khach.</summary>
        public string PreviewText
        {
            get
            {
                var nights = Math.Max(1, (NewCheckOut.Date - _stay.ActualCheckIn.Date).Days);
                return $"{nights} đêm × {_basePrice:N0} đ = {nights * _basePrice:N0} đ";
            }
        }

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
