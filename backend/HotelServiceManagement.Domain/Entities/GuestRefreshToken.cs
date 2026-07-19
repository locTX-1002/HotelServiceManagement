using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities
{
    // Doi xung voi RefreshToken (cua User) nhung tach bang rieng - khong dung chung de khong dung
    // toi luong refresh nhan vien da test/hardened, va vi FK tro toi GuestAccount chu khong phai User.
    public class GuestRefreshToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public int GuestAccountId { get; set; }
        public virtual GuestAccount GuestAccount { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }
    }
}
