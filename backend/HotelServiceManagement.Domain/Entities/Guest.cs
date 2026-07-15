using System.Collections.Generic;
using HotelServiceManagement.Domain.Common;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Domain.Entities
{
    public class Guest : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string IdentityNumber { get; set; } = string.Empty;
        public GuestTag Tag { get; set; } = GuestTag.None;
        public string? TagNote { get; set; }

        // Navigation property
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
