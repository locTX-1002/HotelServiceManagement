using DataAccessObjects;

namespace Services
{
    /// <summary>
    /// Cho tang WPF goi chuan bi database luc khoi dong ma khong phai tham chieu truc tiep
    /// xuong DataAccessObjects (giu dung chieu phu thuoc WPF -> Services cua kien truc 3 lop).
    /// </summary>
    public static class DatabaseService
    {
        public static Task EnsureMigratedAsync() => HotelDbContextFactory.EnsureMigratedAsync();
    }
}
