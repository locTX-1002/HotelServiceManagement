namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    public class UpdateGuestProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
