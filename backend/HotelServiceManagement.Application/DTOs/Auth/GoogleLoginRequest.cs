namespace HotelServiceManagement.Application.DTOs.Auth
{
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
