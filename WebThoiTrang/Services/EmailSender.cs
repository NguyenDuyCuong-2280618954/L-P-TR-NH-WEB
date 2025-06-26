// Trong Services/EmailSender.cs
using MailKit.Net.Smtp; // Chỉ giữ lại dòng này MỘT LẦN
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
// XÓA DÒNG NÀY: using System.Net.Mail; // Không cần thiết khi dùng MailKit
using System.Threading.Tasks;

// XÓA CẢ HAI DÒNG LẶP LẠI NÀY:
// using MailKit.Net.Smtp;
// using System.Net.Mail;

namespace WebThoiTrang.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var senderName = _configuration["EmailSettings:SenderName"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:Password"];
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = message };

            using (var smtp = new SmtpClient()) // Bây giờ trình biên dịch sẽ hiểu đây là MailKit.Net.Smtp.SmtpClient
            {
                // Để debug:
                // smtp.Connect(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                // Hoặc nếu bạn dùng cổng 465 (SSL):
                // smtp.Connect(smtpServer, smtpPort, SecureSocketOptions.SslOnConnect);

                // Quan trọng: Sử dụng SecureSocketOptions.StartTls nếu bạn dùng cổng 587
                // Hoặc SecureSocketOptions.SslOnConnect nếu bạn dùng cổng 465
                await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(senderEmail, senderPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
        }
    }
}