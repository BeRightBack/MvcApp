using MvcApp.Services;
using Xunit;

namespace MvcApp.Tests;

public class EmailQueueTests
{
    [Fact]
    public async Task Enqueue_ThenDequeue_RoundTripsItem()
    {
        var queue = new EmailQueue();
        var item = new EmailQueueItem { To = "a@b.c", Subject = "S", Message = "M" };

        await queue.EnqueueAsync(item);
        var dequeued = await queue.DequeueAsync();

        Assert.Same(item, dequeued);
    }

    [Fact]
    public async Task Enqueue_PreservesContactFields()
    {
        var queue = new EmailQueue();
        var item = new EmailQueueItem { IsContact = true, ContactName = "N", ContactEmail = "n@b.c", Subject = "S", Message = "M" };

        await queue.EnqueueAsync(item);
        var dequeued = await queue.DequeueAsync();

        Assert.True(dequeued.IsContact);
        Assert.Equal("N", dequeued.ContactName);
        Assert.Equal("n@b.c", dequeued.ContactEmail);
    }
}
