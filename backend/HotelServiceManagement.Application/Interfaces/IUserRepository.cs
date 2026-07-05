using System.Threading.Tasks;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailWithRoleAsync(string email);
        Task<User?> GetByIdWithRoleAsync(int id);
    }
}
