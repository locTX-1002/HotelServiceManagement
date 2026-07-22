using System.Collections.Generic;
using BusinessObjects.Common;
using BusinessObjects.Enums;

namespace BusinessObjects.Entities
{
    public class Guest : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;

        // Co the de trong khi tao ho so tam; phai duoc xac minh truoc khi check-in.
        public string? IdentityNumber { get; set; }
        public GuestTag Tag { get; set; } = GuestTag.None;
        public string? TagNote { get; set; }

        // Navigation property
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
