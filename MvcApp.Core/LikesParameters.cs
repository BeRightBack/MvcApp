namespace MvcApp.Core.Pagination;

public class LikesParameters : PaginationParameters
{
    public string UserId { get; set; } = string.Empty;
    public string Predicate { get; set; } = string.Empty;
    public string Gender { get; set; } = "All";
    public string OrderBy { get; set; } = "LastActive";

    public string Values
    {
        get
        {
            return $"User({UserId})-Predicate({Predicate})-Gender({Gender})-OrderBy({OrderBy})-PageSize({PageSize})-PageNumber({PageNumber})";
        }
    }
}
