using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities
{
    // Doi xung voi PasswordResetToken cua nhan vien nhung tach bang rieng, FK toi GuestAccount.
    public class GuestPasswordResetToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public int GuestAccountId { get; set; }
        public virtual GuestAccount GuestAccount { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
