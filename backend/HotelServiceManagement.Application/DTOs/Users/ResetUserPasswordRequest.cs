namespace HotelServiceManagement.Application.DTOs.Users
{
    public class ResetUserPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
