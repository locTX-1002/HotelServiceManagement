using BusinessObjects.Common;

namespace BusinessObjects.Entities
{
    // Tach hoan toan khoi Guest (ban ghi le tan quan ly) - Guest co the ton tai ma khong bao gio
    // co GuestAccount (chua tu kich hoat dang nhap). 1-1 qua GuestId.
    public class GuestAccount : BaseEntity
    {
        public int GuestId { get; set; }
        public virtual Guest Guest { get; set; } = null!;

        // Null khi tai khoan chi dang nhap bang Google (khong dat mat khau) - LoginAsync (SDT+mat
        // khau) tu nhien khong dang nhap duoc voi tai khoan nay vi khong co gi de so sanh.
        public string? PasswordHash { get; set; }

        // "sub" claim cua Google (dinh danh nguoi dung on dinh, khong doi kieu email) - null neu chua
        // tung lien ket Google. Dung de nhan ra ngay lan dang nhap Google sau, khong can hoi lai SDT.
        public string? GoogleSubjectId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
