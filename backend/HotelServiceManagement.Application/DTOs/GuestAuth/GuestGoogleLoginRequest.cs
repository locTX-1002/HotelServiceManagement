namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    public class GuestGoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;

        // Chi can luc dau tien dang nhap bang Google nay (chua tung lien ket) - dung de khop/tao
        // Guest, vi Google khong tra ve so dien thoai.
        public string? PhoneNumber { get; set; }
    }
}
