using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
