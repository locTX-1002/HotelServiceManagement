namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    // He thong chua co ha tang gui email nen dung lai chinh co che xac minh cua dang ky (BookingCode +
    // ho ten + SDT) de chung minh chu so huu thay vi gui token qua email.
    public class GuestResetPasswordRequest
    {
        public string BookingCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
