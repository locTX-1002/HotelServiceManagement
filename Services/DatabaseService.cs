using Microsoft.Extensions.Configuration;
using Repositories;

namespace Services
{
    /// <summary>
    /// Cho tang WPF goi chuan bi database luc khoi dong ma khong phai tham chieu truc tiep
    /// xuong DataAccessObjects (giu dung chieu phu thuoc WPF -> Services cua kien truc 3 lop).
    /// </summary>
    public static class DatabaseService
    {
        public static async Task EnsureMigratedAsync()
        {
            await new DatabaseRepository().EnsureMigratedAsync();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .Build();
            var email = configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
            var fullName = configuration["BootstrapAdmin:FullName"]?.Trim();
            var password = configuration["BootstrapAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName)
                || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Thieu cau hinh BootstrapAdmin trong appsettings.Local.json.");
            var passwordError = PasswordPolicy.Validate(password);
            if (passwordError != null) throw new InvalidOperationException(passwordError);
            await new UserRepository().EnsureBootstrapAdminAsync(fullName, email, BCrypt.Net.BCrypt.HashPassword(password));
        }
    }
}
