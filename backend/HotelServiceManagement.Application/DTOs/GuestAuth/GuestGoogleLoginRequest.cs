namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    public class GuestGoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;

        // Chi can luc dau tien dang nhap bang Google nay (chua tung lien ket) - dung de khop/tao
        // Guest, vi Google khong tra ve so dien thoai.
        public string? PhoneNumber { get; set; }

        // Tuy chon, chi ap dung khi tao Guest MOI (khong khop SDT voi Guest co san) - Guest co san
        // giu nguyen ten da duoc le tan xac minh luc dat phong.
        public string? FullName { get; set; }

        // Tuy chon - dat mat khau ngay luc lien ket Google de sau nay dang nhap lai duoc bang SDT +
        // mat khau, khong bat buoc phai qua Google.
        public string? Password { get; set; }
    }
}
