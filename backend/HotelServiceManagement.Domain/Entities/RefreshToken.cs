using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // null = con song. Danh dau khi bi thu hoi (logout) hoac bi xoay vong (da dung de refresh 1 lan).
        public DateTime? RevokedAt { get; set; }

        // Token moi da thay the token nay khi xoay vong - null neu chua bi thay.
        public string? ReplacedByToken { get; set; }
    }
}
