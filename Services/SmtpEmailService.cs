using System.Net;
using System.Net.Mail;

namespace SecureSistem.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var host = _configuration["Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("SMTP is not configured (Smtp:Host is missing).");

            var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");
            var fromAddress = _configuration["Smtp:FromAddress"] ?? username ?? "no-reply@localhost";
            var fromName = _configuration["Smtp:FromName"] ?? "SecureSistem";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrEmpty(username) ? null : new NetworkCredential(username, password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
        }
    }
}
