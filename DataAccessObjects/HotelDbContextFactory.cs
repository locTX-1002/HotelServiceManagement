using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    /// <summary>
    /// Diem tao DbContext DUY NHAT luc runtime - moi DAO goi qua day de dam bao ca app dung chung
    /// 1 connection string doc tu appsettings.json (nam canh file .exe). Khong hardcode chuoi ket
    /// noi rai rac trong tung DAO.
    /// </summary>
    public static class HotelDbContextFactory
    {
        public static HotelDbContext Create()
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>().Options;
            return new HotelDbContext(options);
        }

        /// <summary>
        /// Ap moi migration con thieu vao database (tu tao DB neu chua co) - goi 1 lan luc app
        /// khoi dong; thanh vien moi chi can F5 de app tu chuan bi database.
        /// </summary>
        public static async Task EnsureMigratedAsync()
        {
            await using var context = Create();
            await context.Database.MigrateAsync();
        }
    }
}
