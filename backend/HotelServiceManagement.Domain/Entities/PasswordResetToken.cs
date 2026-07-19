using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities
{
    // Token 1 lan dung, song rat ngan (30 phut) - khac RefreshToken (song nhieu ngay, dung nhieu lan
    // qua xoay vong). Danh cho luong "Quen mat khau" qua email cua nhan vien.
    public class PasswordResetToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // null = con dung duoc. Danh dau ngay sau khi doi mat khau thanh cong de khong dung lai duoc.
        public DateTime? UsedAt { get; set; }
    }
}
