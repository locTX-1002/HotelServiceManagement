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

        // Null khi Guest duoc tao tu tu dang ky guest portal (khong con bat buoc CCCD/CMND luc dang
        // ky vi ly do rieng tu - xac minh danh tinh that van dien ra o quay le tan luc check-in). Van
        // bat buoc khi le tan tao Guest thu cong (GuestService.Validate).
        public string? IdentityNumber { get; set; }
        public GuestTag Tag { get; set; } = GuestTag.None;
        public string? TagNote { get; set; }

        // Navigation property
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
