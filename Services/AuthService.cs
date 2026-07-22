using BusinessObjects.Entities;
using Repositories;

namespace Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository = new UserRepository();

        public User? Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var user = _userRepository.GetActiveByEmail(email);
            if (user == null)
            {
                return null;
            }

            // BCrypt giong het ban web - DB seed san 4 tai khoan (admin/manager/receptionist/
            // service @hotel.com) nen dang nhap duoc ngay sau lan chay dau tien.
            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
        }
    }
}
