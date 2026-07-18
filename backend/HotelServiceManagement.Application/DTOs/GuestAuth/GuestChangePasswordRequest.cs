namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    public class GuestChangePasswordRequest
    {
        // Rong neu tai khoan chua tung dat mat khau (chi dang nhap Google tu truoc den gio).
        public string? CurrentPassword { get; set; }
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
