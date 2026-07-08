namespace HotelServiceManagement.Application.DTOs.Guests;

public class CreateGuestRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = string.Empty;
}
