using System;
using Microsoft.Extensions.Configuration;

namespace Services
{
    /// <summary>
    /// Thong tin gioi thieu khach san hien tren trang chu. Doc tu muc "Hotel" trong
    /// appsettings.json de doi ten hay nam thanh lap khong phai sua code.
    /// Thieu muc do van chay binh thuong bang gia tri mac dinh ben duoi.
    /// </summary>
    public static class HotelInfo
    {
        public static string Name { get; }
        public static string Tagline { get; }
        public static string About { get; }
        public static int EstablishedYear { get; }

        static HotelInfo()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .Build()
                .GetSection("Hotel");

            Name = config["Name"] ?? "FU Hotel & Resort";
            Tagline = config["Tagline"] ?? "Nghỉ dưỡng giữa lòng thành phố";
            About = config["About"]
                ?? "Chúng tôi mang tới không gian nghỉ dưỡng giữa lòng thành phố với đội ngũ "
                 + "phục vụ tận tâm, phòng nghỉ đầy đủ tiện nghi và dịch vụ ẩm thực trọn ngày.";
            EstablishedYear = int.TryParse(config["EstablishedYear"], out var year) ? year : 2015;
        }
    }
}
