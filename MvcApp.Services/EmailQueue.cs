using System.Threading.Channels;

namespace MvcApp.Services;

public sealed class EmailQueueItem
{
    public string To { get; init; } = "";
    public string Subject { get; init; } = "";
    public string? Heading { get; init; }
    public string Message { get; init; } = "";
    public string? CtaText { get; init; }
    public string? CtaUrl { get; init; }
    public bool IsContact { get; init; }
    public string? ContactName { get; init; }
    public string? ContactEmail { get; init; }
    public string? RawHtmlBody { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
}

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailQueueItem item, CancellationToken cancellationToken = default);
    ValueTask<EmailQueueItem> DequeueAsync(CancellationToken cancellationToken = default);
}

public sealed class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailQueueItem> _channel = Channel.CreateBounded<EmailQueueItem>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.Wait });

    public ValueTask EnqueueAsync(EmailQueueItem item, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(item, cancellationToken);

    public ValueTask<EmailQueueItem> DequeueAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAsync(cancellationToken);
}
