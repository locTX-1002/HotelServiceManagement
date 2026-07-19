namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    // Nhan EMAIL chu khong phai so dien thoai: link dat lai von di gui qua email, hoi so dien thoai
    // roi gui di dau thi khach khong biet - va khach dang ky khong kem email se bam mai khong ra gi.
    public class GuestForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
