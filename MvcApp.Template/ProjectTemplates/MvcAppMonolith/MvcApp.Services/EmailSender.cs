using MvcApp.Core.Abstractions;

namespace MvcApp.Services
{
    public class EmailSender(IEmailQueue queue) : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
            => queue.EnqueueAsync(new EmailQueueItem { To = email, Subject = subject, Message = message }).AsTask();

        public Task SendEmailAsync(string email, string subject, string heading, string message, string? ctaText = null, string? ctaUrl = null)
            => queue.EnqueueAsync(new EmailQueueItem
            {
                To = email,
                Subject = subject,
                Heading = heading,
                Message = message,
                CtaText = ctaText,
                CtaUrl = ctaUrl
            }).AsTask();

        public Task SendFromContactAsync(string name, string email, string subject, string message)
            => queue.EnqueueAsync(new EmailQueueItem
            {
                IsContact = true,
                ContactName = name,
                ContactEmail = email,
                Subject = subject,
                Message = message
            }).AsTask();
    }
}
