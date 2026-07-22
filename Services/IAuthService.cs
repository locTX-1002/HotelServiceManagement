using BusinessObjects.Entities;

namespace Services
{
    public interface IAuthService
    {
        /// <summary>Tra ve User (kem Role) neu email + mat khau dung; nguoc lai null.</summary>
        User? Login(string email, string password);
    }
}
