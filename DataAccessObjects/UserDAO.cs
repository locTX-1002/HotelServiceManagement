using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    /// <summary>
    /// DAO tang truy cap du lieu cho User - Singleton pattern theo dung khung diem cua de
    /// (moi DAO 1 Instance duy nhat, Repository chi bo ra goi lai). Moi thao tac mo 1 DbContext
    /// ngan han rieng de tranh giu context song lau trong app desktop (de dinh cache cu/loi track).
    /// QUY UOC NHOM: moi ham DAO deu la async (...Async) - cam ExecuteSync/.Result lam dung hinh UI.
    /// </summary>
    public class UserDAO
    {
        private static UserDAO? _instance;
        private static readonly object _lock = new();

        private UserDAO() { }

        public static UserDAO Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new UserDAO();
                }
            }
        }

        /// <summary>Tim user con hoat dong theo email (khong phan biet hoa thuong), kem Role de phan quyen.</summary>
        public async Task<User?> GetActiveByEmailAsync(string email)
        {
            var normalized = email.Trim().ToLower();
            await using var context = HotelDbContextFactory.Create();
            return await context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IsActive && u.Email.ToLower() == normalized);
        }
    }
}
