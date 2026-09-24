namespace MvcApp.Core.Pagination;

public class MessageParameters : PaginationParameters
{
    public required string Username { get; set; }
    public string Container { get; set; } = string.Empty;
}
