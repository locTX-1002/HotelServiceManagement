using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities
{
    // Tach hoan toan khoi Guest (ban ghi le tan quan ly) - Guest co the ton tai ma khong bao gio
    // co GuestAccount (chua tu kich hoat dang nhap). 1-1 qua GuestId.
    public class GuestAccount : BaseEntity
    {
        public int GuestId { get; set; }
        public virtual Guest Guest { get; set; } = null!;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
