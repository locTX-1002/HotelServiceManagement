namespace HotelServiceManagement.Application.DTOs.Users
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int RoleId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
