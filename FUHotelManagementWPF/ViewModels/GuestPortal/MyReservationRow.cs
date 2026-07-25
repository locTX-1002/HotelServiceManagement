using System;
using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace FUHotelManagementWPF.ViewModels.GuestPortal
{
    /// <summary>
    /// Mot dong don dat phong hien cho KHACH xem (chi doc). Gom san cac chuoi da dinh dang
    /// de View khong phai dung converter rieng cho tung o.
    /// </summary>
    public class MyReservationRow
    {
        public Reservation Reservation { get; }

        public MyReservationRow(Reservation reservation) => Reservation = reservation;

        public string BookingCode => Reservation.BookingCode;

        // Binh thuong EF da Include Room + RoomType, nhung van dung ?. de mot don du lieu
        // thieu khong lam vo ca man hinh cua khach.
        public string RoomText
        {
            get
            {
                var number = Reservation.Room?.RoomNumber ?? "—";
                var type = Reservation.Room?.RoomType?.TypeName;
                return string.IsNullOrWhiteSpace(type) ? $"Phòng {number}" : $"Phòng {number} · {type}";
            }
        }

        public string GuestCountText => $"{Reservation.NumberOfGuests} khách";

        public string DateRangeText
            => $"{Reservation.CheckInDate:dd/MM/yyyy} → {Reservation.CheckOutDate:dd/MM/yyyy}";

        // So dem tinh theo ngay (bo phan gio) - dat trong ngay van coi la 1 dem cho khach de hieu.
        public int Nights
        {
            get
            {
                var days = (Reservation.CheckOutDate.Date - Reservation.CheckInDate.Date).Days;
                return days < 1 ? 1 : days;
            }
        }

        public string NightText => $"{Nights} đêm";

        public bool HasDeposit => Reservation.DepositAmount is > 0;
        public decimal DepositAmount => Reservation.DepositAmount ?? 0;

        public bool HasSpecialRequests => !string.IsNullOrWhiteSpace(Reservation.SpecialRequests);
        public string SpecialRequests => Reservation.SpecialRequests ?? string.Empty;

        public ReservationStatus Status => Reservation.Status;

        public string StatusText => Reservation.Status switch
        {
            ReservationStatus.Pending => "Chờ xác nhận",
            ReservationStatus.Confirmed => "Đã xác nhận",
            ReservationStatus.CheckedIn => "Đang ở",
            ReservationStatus.Completed => "Đã trả phòng",
            ReservationStatus.Cancelled => "Đã huỷ",
            ReservationStatus.NoShow => "Không đến",
            _ => "—",
        };

        /// <summary>Don khach con phai quan tam (dang o hoac sap toi) - nhom nay xep len tren.</summary>
        public bool IsActive => Reservation.Status is ReservationStatus.CheckedIn
            or ReservationStatus.Confirmed
            or ReservationStatus.Pending;

        /// <summary>Don sap toi: da dat nhung chua nhan phong.</summary>
        public bool IsUpcoming => Reservation.Status is ReservationStatus.Confirmed
            or ReservationStatus.Pending;

        public bool IsStaying => Reservation.Status == ReservationStatus.CheckedIn;

        // Thu tu trong nhom "con hieu luc": dang o truoc, roi den don da xac nhan, cuoi la don cho duyet.
        public int SortRank => Reservation.Status switch
        {
            ReservationStatus.CheckedIn => 0,
            ReservationStatus.Confirmed => 1,
            ReservationStatus.Pending => 2,
            _ => 3,
        };

        public DateTime CheckInDate => Reservation.CheckInDate;
    }
}
