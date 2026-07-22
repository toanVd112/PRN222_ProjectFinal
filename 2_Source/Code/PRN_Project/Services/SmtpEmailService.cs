using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PRN_Project.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var host = emailSettings["SmtpServer"];
            var port = int.Parse(emailSettings["SmtpPort"] ?? "587");
            var userName = emailSettings["SenderEmail"];
            var password = emailSettings["SenderPassword"];
            var senderName = emailSettings["SenderName"];

            using (var client = new SmtpClient(host, port))
            {
                client.Credentials = new NetworkCredential(userName, password);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(userName, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                var overrideEmail = emailSettings["OverrideToEmail"];
                if (!string.IsNullOrEmpty(overrideEmail))
                {
                    mailMessage.To.Add(overrideEmail);
                    body = $"<div style='background: #fff3cd; padding: 10px; border-radius: 5px; margin-bottom: 20px; color: #856404;'><b>[TEST MODE]</b> Email này đáng lẽ được gửi đến <b>{toEmail}</b> nhưng đã được chuyển hướng về đây.</div>" + body;
                }
                else
                {
                    mailMessage.To.Add(toEmail);
                }

                // For testing purposes, if credentials are empty, we just simulate the send
                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                {
                    System.Diagnostics.Debug.WriteLine("=== SIMULATED EMAIL ===");
                    System.Diagnostics.Debug.WriteLine($"To: {toEmail}");
                    System.Diagnostics.Debug.WriteLine($"Subject: {subject}");
                    System.Diagnostics.Debug.WriteLine($"Body: {body}");
                    System.Diagnostics.Debug.WriteLine("=======================");
                    return;
                }

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
