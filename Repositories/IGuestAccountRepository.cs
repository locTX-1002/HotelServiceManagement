using BusinessObjects.Entities;
namespace Repositories; public interface IGuestAccountRepository { Task<GuestAccount?> GetByGuestIdAsync(int id); Task<GuestAccount?> GetByPhoneAsync(string phone); Task SaveAsync(GuestAccount x, bool add); }
