namespace MvcApp.Module.Messages.ViewModels;

/// <summary>
/// One row of the admin message oversight list. Deliberately not the <c>Message</c> entity: the
/// admin list needs the derived flags (is this sender currently banned) and must not hand the
/// view a change-tracked entity it could accidentally mutate.
/// </summary>
public sealed record AdminMessageRow(
    int Id,
    string? SenderUsername,
    string? RecipientUsername,
    string? Content,
    DateTime MessageSent,
    DateTime? DateRead,
    bool SenderDeleted,
    bool RecipientDeleted,
    bool SenderBanned);

public sealed class AdminMessagesViewModel
{
    public string Search { get; set; } = "";
    public string Flagged { get; set; } = "";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int Total { get; set; }
    public IReadOnlyList<AdminMessageRow> Messages { get; set; } = [];
}
