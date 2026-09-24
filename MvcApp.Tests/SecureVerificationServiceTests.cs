using Microsoft.Extensions.Configuration;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Services;
using Moq;
using Xunit;

namespace MvcApp.Tests;

public class SecureVerificationServiceTests
{
    private static (SecureVerificationService svc, Mock<IEmailQueue> queue, Mock<IVerificationCodeRepository> repo) Create()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Encryption:Key"]).Returns("0123456789abcdef0123456789abcdef");

        var repo = new Mock<IVerificationCodeRepository>();

        var branding = new Mock<IBrandingService>();
        branding.Setup(b => b.GetSiteNameAsync()).ReturnsAsync("TestSite");
        branding.Setup(b => b.GetLogoUrlAsync()).ReturnsAsync((string?)null);
        branding.Setup(b => b.GetTaglineAsync()).ReturnsAsync((string?)null);

        var queue = new Mock<IEmailQueue>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<EmailQueueItem>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var svc = new SecureVerificationService(config.Object, repo.Object, branding.Object, queue.Object);
        return (svc, queue, repo);
    }

    [Fact]
    public async Task SendVerificationCodeAsync_CreatesRecordAndQueuesSecureEmail()
    {
        var (svc, queue, repo) = Create();

        var result = await svc.SendVerificationCodeAsync("u@example.com", "user-1", "127.0.0.1", "Mozilla");

        Assert.Equal(6, result.Code.Length);
        Assert.False(string.IsNullOrEmpty(result.EncryptedCode));
        repo.Verify(r => r.CreateAsync(It.Is<VerificationCodeRecord>(rec =>
            rec.UserId == "user-1" &&
            rec.Email == "u@example.com")), Times.Once);
        queue.Verify(q => q.EnqueueAsync(It.Is<EmailQueueItem>(i =>
            i.To == "u@example.com" &&
            i.Subject == "Secure Verification Code" &&
            i.RawHtmlBody != null &&
            i.Headers != null &&
            i.Headers.ContainsKey("X-Content-Type-Options") &&
            i.Headers.ContainsKey("X-Frame-Options")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyCodeAsync_ValidCode_SucceedsAndMarksUsed()
    {
        var (svc, _, repo) = Create();
        var sent = await svc.SendVerificationCodeAsync("u@example.com", "user-1");

        repo.Setup(r => r.GetActiveByUserIdAsync("user-1")).ReturnsAsync(new VerificationCodeRecord
        {
            UserId = "user-1",
            Email = "u@example.com",
            EncryptedCode = sent.EncryptedCode,
            ExpiresAt = sent.Expiry
        });

        var ok = await svc.VerifyCodeAsync("user-1", sent.Code);

        Assert.True(ok);
        repo.Verify(r => r.MarkAsUsedAsync(It.IsAny<string>(), null, null), Times.Once);
    }

    [Fact]
    public async Task VerifyCodeAsync_WrongCode_FailsWithoutMarkingUsed()
    {
        var (svc, _, repo) = Create();
        var sent = await svc.SendVerificationCodeAsync("u@example.com", "user-1");

        repo.Setup(r => r.GetActiveByUserIdAsync("user-1")).ReturnsAsync(new VerificationCodeRecord
        {
            UserId = "user-1",
            Email = "u@example.com",
            EncryptedCode = sent.EncryptedCode,
            ExpiresAt = sent.Expiry
        });

        var ok = await svc.VerifyCodeAsync("user-1", "000000");

        Assert.False(ok);
        repo.Verify(r => r.MarkAsUsedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task VerifyCodeAsync_ExpiredCode_Fails()
    {
        var (svc, _, repo) = Create();
        var sent = await svc.SendVerificationCodeAsync("u@example.com", "user-1");

        repo.Setup(r => r.GetActiveByUserIdAsync("user-1")).ReturnsAsync(new VerificationCodeRecord
        {
            UserId = "user-1",
            Email = "u@example.com",
            EncryptedCode = sent.EncryptedCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        });

        var ok = await svc.VerifyCodeAsync("user-1", sent.Code);

        Assert.False(ok);
    }
}
