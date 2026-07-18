using System.Net;
using System.Net.Mail;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HotelServiceManagement.Infrastructure.Services
{
    // Dung System.Net.Mail co san trong .NET (khong them goi ngoai) - du dung cho 1 email don gian
    // qua Gmail SMTP + App Password. Config doc tu Smtp:* - FromPassword luon nam trong
    // dotnet user-secrets (moi truong dev) hoac bien moi truong that (production), KHONG BAO GIO
    // ghi vao appsettings.json de tranh commit nham secret that.
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
        {
            var smtp = _configuration.GetSection("Smtp");
            var host = smtp["Host"] ?? "smtp.gmail.com";
            var port = int.Parse(smtp["Port"] ?? "587");
            var fromEmail = smtp["FromEmail"];
            var fromPassword = smtp["FromPassword"];
            var fromName = smtp["FromName"] ?? "HSMS - Hotel Service Management";

            if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(fromPassword))
            {
                // Chua cau hinh SMTP that (dev chua chay dotnet user-secrets) - log ra console thay vi
                // nem loi, de cac luong khac cua app khong bi vo vi thieu credential email.
                _logger.LogWarning(
                    "SMTP chua duoc cau hinh (Smtp:FromEmail/Smtp:FromPassword rong) - khong gui duoc email toi {ToEmail}. Link dat lai: {ResetLink}",
                    toEmail, resetLink);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "HSMS - Yêu cầu đặt lại mật khẩu",
                Body = $"Chào {toName},\n\n" +
                       $"Có yêu cầu đặt lại mật khẩu cho tài khoản {toEmail}. Bấm vào liên kết dưới đây để đặt mật khẩu mới (hết hạn sau 30 phút):\n\n" +
                       $"{resetLink}\n\n" +
                       "Nếu bạn không yêu cầu việc này, hãy bỏ qua email này.",
                IsBodyHtml = false,
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
            };

            await client.SendMailAsync(message);
        }
    }
}
