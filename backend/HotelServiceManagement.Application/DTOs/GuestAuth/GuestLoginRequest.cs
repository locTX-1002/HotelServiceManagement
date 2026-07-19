namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    public class GuestLoginRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
