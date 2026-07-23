using BusinessObjects.Entities;
using BusinessObjects.Enums;
using FUHotelManagementWPF.ViewModels.Rooms;
using Services;

namespace FUHotelManagementWPF.ViewModels.Reservations
{
    /// <summary>Dòng đặt phòng cho card-row + panel chi tiết (D3).</summary>
    public class ReservationRow
    {
        public Reservation Reservation { get; }
        public string StatusText { get; }

        public string Thumbnail => RoomImages.Thumbnail(Reservation.Room?.RoomType?.TypeName ?? string.Empty);
        public string GuestName => Reservation.Guest?.FullName ?? string.Empty;
        public string RoomNumber => Reservation.Room?.RoomNumber ?? string.Empty;
        public string TypeName => Reservation.Room?.RoomType?.TypeName ?? string.Empty;
        public string DateRange => $"{Reservation.CheckInDate:dd/MM} → {Reservation.CheckOutDate:dd/MM/yyyy}";
        public string SubText => $"{RoomNumber} · {TypeName} · {DateRange}";
        public string GuestCountText => $"{Reservation.NumberOfGuests} khách";
        public string BookingCode => Reservation.BookingCode;
        public string? SpecialRequests => Reservation.SpecialRequests;
        public bool HasSpecialRequests => !string.IsNullOrWhiteSpace(Reservation.SpecialRequests);

        public ReservationStatus Status => Reservation.Status;
        public bool CanConfirm => Reservation.Status == ReservationStatus.Pending;
        public bool CanEdit => Reservation.Status is ReservationStatus.Pending or ReservationStatus.Confirmed;
        public bool CanCancel => CanEdit;
        public bool CanNoShow => Reservation.Status == ReservationStatus.Confirmed;

        public ReservationRow(Reservation reservation)
        {
            Reservation = reservation;
            StatusText = ReservationService.StatusText(reservation.Status);
        }
    }

    public record ReservationStatusFilter(string Label, ReservationStatus? Status);
}
