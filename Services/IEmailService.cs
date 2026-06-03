namespace UrbanDrive.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string toName, string subject, string body);
        Task SendEmailWithTemplateAsync(string toEmail, string toName, string templateType, Dictionary<string, string> placeholders);
    }
}