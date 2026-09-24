using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MvcApp.Core.Abstractions;

namespace MvcApp.Services;

public sealed class SmtpEmailSender(
    IConfiguration configuration,
    ISettingsService settings,
    IBrandingService branding,
    ILogger<SmtpEmailSender> logger) : IDisposable
{
    private readonly SmtpClient _client = new();

    public async Task SendAsync(EmailQueueItem item, CancellationToken cancellationToken = default)
    {
        var smtpEnabled = await settings.GetAsync<bool>("SmtpEnabled") ?? true;
        if (!smtpEnabled)
        {
            logger.LogInformation("SMTP disabled by system setting. Skipped email to {To}: {Subject}", item.To, item.Subject);
            return;
        }

        var emailFrom = configuration["SmtpSettings:From"];
        var smtpHost = configuration["SmtpSettings:Host"];
        var smtpPortString = configuration["SmtpSettings:Port"];

        if (string.IsNullOrWhiteSpace(emailFrom) || string.IsNullOrWhiteSpace(smtpHost) || smtpPortString == null)
        {
            logger.LogError("SMTP settings incomplete (From/Host/Port). Cannot send email to {To}: {Subject}", item.To, item.Subject);
            return;
        }

        var smtpPort = int.Parse(smtpPortString);
        var smtpUser = configuration["SmtpSettings:Username"];
        var smtpPass = configuration["SmtpSettings:Password"];

        var siteName = await branding.GetSiteNameAsync();
        var logoUrl = await branding.GetLogoUrlAsync();
        var tagline = await branding.GetTaglineAsync();

        var message = BuildMessage(item, siteName, logoUrl, tagline, emailFrom);

        // Certificates are validated by default. Opt-in bypass only when explicitly configured
        // (insecure, intended for development against self-signed local SMTP servers).
        var allowInvalidCertificates = configuration.GetValue<bool>("SmtpSettings:AllowInvalidCertificates");
        _client.ServerCertificateValidationCallback = allowInvalidCertificates
            ? (s, c, h, e) => true
            : null;
        _client.Timeout = 30000; // 30 seconds

        try
        {
            await _client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls, cancellationToken);
            if (!string.IsNullOrWhiteSpace(smtpUser))
            {
                await _client.AuthenticateAsync(smtpUser, smtpPass ?? "", cancellationToken);
            }
            await _client.SendAsync(message, cancellationToken);
            logger.LogInformation("Email sent to {To}: {Subject}", item.To, item.Subject);
        }
        finally
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync(true, cancellationToken);
            }
        }
    }

    public void Dispose() => _client.Dispose();

    private static MimeMessage BuildMessage(EmailQueueItem item, string siteName, string? logoUrl, string? tagline, string emailFrom)
    {
        var message = new MimeMessage();

        if (item.IsContact)
        {
            var fromAddress = item.ContactEmail ?? "noreply@example.com";
            message.From.Add(new MailboxAddress(item.ContactName ?? "", fromAddress));
            message.To.Add(new MailboxAddress(siteName, emailFrom));
            message.Subject = item.Subject;
            message.Body = new TextPart("plain") { Text = item.Message };
        }
        else
        {
            message.From.Add(new MailboxAddress(siteName, emailFrom));
            message.To.Add(new MailboxAddress("", item.To));
            message.Subject = item.Subject;
            var html = item.RawHtmlBody
                ?? EmailTemplates.Build(siteName, item.Subject, item.Message, logoUrl, tagline, item.Heading, item.CtaText, item.CtaUrl);
            message.Body = new TextPart("html") { Text = html };
        }

        if (item.Headers != null)
        {
            foreach (var (key, value) in item.Headers)
            {
                message.Headers.Add(key, value);
            }
        }

        return message;
    }
}
