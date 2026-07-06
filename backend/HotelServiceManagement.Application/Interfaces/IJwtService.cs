using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IJwtService
    {
        (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
        string GenerateToken(User user);
    }
}
