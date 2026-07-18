namespace HotelServiceManagement.Application.DTOs.GuestAuth
{
    public class GuestProfileResponse
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool HasPassword { get; set; }
    }
}
