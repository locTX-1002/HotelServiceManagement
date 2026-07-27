using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccessObjects
{
    /// <summary>
    /// Diem tao DbContext DUY NHAT luc runtime - moi DAO goi qua day de dam bao ca app dung chung
    /// 1 connection string doc tu appsettings.json (nam canh file .exe). Khong hardcode chuoi ket
    /// noi rai rac trong tung DAO.
    /// </summary>
    public static class HotelDbContextFactory
    {
        private sealed record AmbientTransactionState(DbConnection Connection, DbTransaction Transaction);

        private static readonly AsyncLocal<AmbientTransactionState?> AmbientTransaction = new();

        public static HotelDbContext Create()
        {
            var ambient = AmbientTransaction.Value;
            if (ambient == null)
            {
                var options = new DbContextOptionsBuilder<HotelDbContext>().Options;
                return new HotelDbContext(options);
            }

            // Khi workflow Approval dang mo transaction, moi DAO van duoc phep tao DbContext
            // ngan han nhu cu, nhung tat ca DbContext do dung CHUNG connection + DbTransaction.
            // context con khong so huu connection nen Dispose() khong dong transaction cua workflow.
            var sharedOptions = new DbContextOptionsBuilder<HotelDbContext>()
                .UseSqlServer(ambient.Connection)
                .Options;
            var context = new HotelDbContext(sharedOptions);
            context.Database.UseTransaction(ambient.Transaction);
            return context;
        }

        /// <summary>
        /// Chay mot workflow qua nhieu DAO/service trong cung mot transaction SQL Server.
        /// Nested call se tai su dung transaction hien tai thay vi mo transaction moi.
        /// </summary>
        public static async Task<T> ExecuteInTransactionAsync<T>(
            IsolationLevel isolationLevel,
            Func<Task<T>> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);

            if (AmbientTransaction.Value != null)
            {
                return await operation();
            }

            await using var owner = Create();
            await owner.Database.OpenConnectionAsync();
            await using var transaction = await owner.Database.BeginTransactionAsync(isolationLevel);

            AmbientTransaction.Value = new AmbientTransactionState(
                owner.Database.GetDbConnection(),
                transaction.GetDbTransaction());
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                AmbientTransaction.Value = null;
            }
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
