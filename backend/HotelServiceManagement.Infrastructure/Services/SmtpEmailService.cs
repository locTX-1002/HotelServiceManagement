using System.Net;
using System.Net.Mail;
using System.Net.Mime;
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

            var plainBody =
                $"Chào {toName},\n\n" +
                $"Có yêu cầu đặt lại mật khẩu cho tài khoản {toEmail}. Mở liên kết dưới đây để đặt mật khẩu mới (hết hạn sau 30 phút):\n\n" +
                $"{resetLink}\n\n" +
                "Nếu bạn không yêu cầu việc này, hãy bỏ qua email này - mật khẩu hiện tại vẫn giữ nguyên.";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "HSMS - Đặt lại mật khẩu",
                Body = plainBody,
                IsBodyHtml = false,
            };
            message.To.Add(toEmail);

            // Gui kem ban HTML: mail client nao doc duoc HTML thi hien ban co thuong hieu, con lai
            // (hoac che do doc van ban) tu rot ve plainBody o tren - dung chuan multipart/alternative.
            var htmlView = AlternateView.CreateAlternateViewFromString(
                BuildPasswordResetHtml(toName, toEmail, resetLink), null, "text/html");
            message.AlternateViews.Add(htmlView);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
            };

            await client.SendMailAsync(message);
        }

        // Email HTML phai viet bang <table> + style inline: Gmail/Outlook cat bo <style> trong <head>
        // va khong ho tro flex/grid, nen bo cuc hien dai se vo o mot so may khach.
        private static string BuildPasswordResetHtml(string toName, string toEmail, string resetLink)
        {
            var name = WebUtility.HtmlEncode(toName);
            var mail = WebUtility.HtmlEncode(toEmail);
            var link = WebUtility.HtmlEncode(resetLink);

            return $@"<!DOCTYPE html>
<html lang=""vi"">
<body style=""margin:0;padding:0;background:#F2EFE9;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#F2EFE9;padding:32px 12px;"">
    <tr><td align=""center"">
      <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""
             style=""max-width:520px;background:#FDFBF7;border:1px solid #E4DFD5;border-radius:14px;overflow:hidden;"">

        <!-- Thuong hieu -->
        <tr><td align=""center"" style=""padding:32px 32px 8px;"">
          <div style=""width:38px;height:46px;background:#A9481F;border-radius:19px 19px 6px 6px;
                      color:#ffffff;font:700 18px Georgia,serif;line-height:66px;text-align:center;"">H</div>
          <div style=""margin-top:10px;font:600 22px Georgia,serif;color:#1A1815;letter-spacing:.5px;"">HSMS</div>
          <div style=""margin-top:2px;font:500 10px Arial,sans-serif;color:#8A857C;letter-spacing:2px;text-transform:uppercase;"">
            Hotel &amp; Service Management
          </div>
        </td></tr>

        <tr><td style=""padding:16px 32px 0;"">
          <hr style=""border:none;border-top:1px solid #E4DFD5;margin:0;"" />
        </td></tr>

        <!-- Noi dung -->
        <tr><td style=""padding:26px 32px 0;"">
          <h1 style=""margin:0;font:600 26px Georgia,serif;color:#1A1815;"">Đặt lại mật khẩu</h1>
          <p style=""margin:14px 0 0;font:400 15px/1.6 Arial,sans-serif;color:#4A463F;"">
            Chào {name}, chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản
            <strong style=""color:#1A1815;"">{mail}</strong>.
          </p>
        </td></tr>

        <!-- Nut hanh dong -->
        <tr><td align=""center"" style=""padding:24px 32px 0;"">
          <a href=""{link}""
             style=""display:inline-block;background:#A9481F;color:#ffffff;text-decoration:none;
                    font:700 14px Arial,sans-serif;letter-spacing:.5px;padding:15px 38px;border-radius:999px;"">
            Đặt mật khẩu mới
          </a>
          <p style=""margin:14px 0 0;font:400 13px Arial,sans-serif;color:#8A857C;"">
            Liên kết có hiệu lực trong <strong style=""color:#4A463F;"">30 phút</strong>.
          </p>
        </td></tr>

        <!-- Link du phong khi nut khong bam duoc -->
        <tr><td style=""padding:24px 32px 0;"">
          <p style=""margin:0;font:400 12px Arial,sans-serif;color:#8A857C;"">Nút không bấm được? Dán liên kết này vào trình duyệt:</p>
          <p style=""margin:6px 0 0;font:400 12px/1.5 'Courier New',monospace;color:#A9481F;word-break:break-all;"">{link}</p>
        </td></tr>

        <!-- Ghi chu an toan -->
        <tr><td style=""padding:22px 32px 0;"">
          <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""
                 style=""background:#F6F1E8;border-radius:10px;"">
            <tr><td style=""padding:14px 16px;font:400 13px/1.55 Arial,sans-serif;color:#6B6459;"">
              Nếu bạn không yêu cầu việc này, hãy bỏ qua email — mật khẩu hiện tại vẫn giữ nguyên.
            </td></tr>
          </table>
        </td></tr>

        <!-- Chan -->
        <tr><td align=""center"" style=""padding:26px 32px 30px;"">
          <hr style=""border:none;border-top:1px solid #E4DFD5;margin:0 0 16px;"" />
          <p style=""margin:0;font:400 11px Arial,sans-serif;color:#A8A29A;"">
            Email tự động từ hệ thống HSMS — vui lòng không trả lời.<br />
            Group 2 · SE1919 · FPT University
          </p>
        </td></tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";
        }
    }
}
