namespace MvcApp.Core.Abstractions;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
    Task SendEmailAsync(string email, string subject, string heading, string message, string? ctaText = null, string? ctaUrl = null);
    Task SendFromContactAsync(string name, string email, string subject, string message);
}
