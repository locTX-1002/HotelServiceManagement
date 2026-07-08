using System;
using HotelServiceManagement.Domain.Common;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Domain.Entities
{
    public class Reservation : BaseEntity
    {
        public string BookingCode { get; set; } = string.Empty;
        public int GuestId { get; set; }
        public virtual Guest Guest { get; set; } = null!;
        public int RoomId { get; set; }
        public virtual Room Room { get; set; } = null!;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        public int? CreatedByUserId { get; set; }
        public virtual User? CreatedByUser { get; set; }

        // Navigation property for 0..1 relationship
        public virtual Stay? Stay { get; set; }
    }
}
