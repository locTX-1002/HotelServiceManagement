namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    public class GuestResetPasswordWithTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
