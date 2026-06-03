using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using UrbanDrive.Models;

namespace UrbanDrive.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly bool _isDevelopment = false; 

        public EmailService(IOptions<MailSettings> mailSettings, ILogger<EmailService> logger)
        {
            _mailSettings = mailSettings.Value;
            _logger = logger;
        }


        public async Task SendEmailAsync(string toEmail, string toName, string subject, string body)
        {
            // DEVELOPMENT MODE: Skip all real email sending
            if (_isDevelopment)
            {
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("📧 [DEV MODE] Email DISABLED - Would have been sent");
                Console.WriteLine($"   To: {toEmail}");
                Console.WriteLine($"   Name: {toName ?? "Not provided"}");
                Console.WriteLine($"   Subject: {subject}");
                Console.WriteLine($"   Body Preview: {(body?.Length > 200 ? body.Substring(0, 200) + "..." : body ?? "Empty")}");
                Console.WriteLine("═══════════════════════════════════════════════════════════");

                await Task.CompletedTask;
                return;
            }

            // PRODUCTION MODE: Actually send email
            try
            {
                if (string.IsNullOrEmpty(_mailSettings.SenderEmail) || string.IsNullOrEmpty(_mailSettings.Server))
                {
                    Console.WriteLine("Email not sent: SMTP not configured");
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_mailSettings.SenderName, _mailSettings.SenderEmail));
                message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(_mailSettings.Server, _mailSettings.Port, SecureSocketOptions.StartTls);

                if (!string.IsNullOrEmpty(_mailSettings.Username) && !string.IsNullOrEmpty(_mailSettings.Password))
                {
                    await client.AuthenticateAsync(_mailSettings.Username, _mailSettings.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {toEmail}");
                Console.WriteLine($"✅ Email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                Console.WriteLine($"❌ Email error: {ex.Message}");
            }
        }

        public async Task SendEmailWithTemplateAsync(string toEmail, string toName, string templateType, Dictionary<string, string> placeholders)
        {
            try
            {
                var template = EmailTemplates.GetTemplate(templateType, placeholders);
                await SendEmailAsync(toEmail, toName, template.Subject, template.Body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Template error: {ex.Message}");
            }
        }
    }
}