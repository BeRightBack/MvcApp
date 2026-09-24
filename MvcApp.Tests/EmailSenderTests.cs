using MvcApp.Core.Abstractions;
using MvcApp.Services;
using Moq;
using Xunit;

namespace MvcApp.Tests;

public class EmailSenderTests
{
    private static Mock<IEmailQueue> CreateQueue()
    {
        var queue = new Mock<IEmailQueue>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<EmailQueueItem>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return queue;
    }

    [Fact]
    public async Task SendEmailAsync_EnqueuesBasicMessage()
    {
        var queue = CreateQueue();
        var sender = new EmailSender(queue.Object);

        await sender.SendEmailAsync("u@example.com", "Subject", "Body");

        queue.Verify(q => q.EnqueueAsync(It.Is<EmailQueueItem>(i =>
            i.To == "u@example.com" &&
            i.Subject == "Subject" &&
            i.Message == "Body" &&
            !i.IsContact), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_EnqueuesHeadingAndCta()
    {
        var queue = CreateQueue();
        var sender = new EmailSender(queue.Object);

        await sender.SendEmailAsync("u@example.com", "Subject", "Heading", "Body", "Click", "https://example.com");

        queue.Verify(q => q.EnqueueAsync(It.Is<EmailQueueItem>(i =>
            i.Heading == "Heading" &&
            i.CtaText == "Click" &&
            i.CtaUrl == "https://example.com"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendFromContactAsync_EnqueuesContactMessage()
    {
        var queue = CreateQueue();
        var sender = new EmailSender(queue.Object);

        await sender.SendFromContactAsync("Jane", "jane@example.com", "Subject", "Body");

        queue.Verify(q => q.EnqueueAsync(It.Is<EmailQueueItem>(i =>
            i.IsContact &&
            i.ContactName == "Jane" &&
            i.ContactEmail == "jane@example.com"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
