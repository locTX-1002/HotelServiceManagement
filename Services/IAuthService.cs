using BusinessObjects.Entities;

namespace Services
{
    public interface IAuthService
    {
        /// <summary>Tra ve User (kem Role) neu email + mat khau dung; nguoc lai null.</summary>
        Task<User?> LoginAsync(string email, string password);
    }
}
