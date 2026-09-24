using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core.Pagination;

public class MemberParameters : PaginationParameters
{
    public string CurrentUsername { get; set; } = string.Empty;

    [MaxLength(25, ErrorMessage = "Invalid selection")]
    public string Gender { get; set; } = string.Empty;

    [Range(18, 85, ErrorMessage ="Must be at least 18 years or older")]
    public int MinAge { get; set; } = 18;

    [Range(18, 85, ErrorMessage = "Sorry pops, you must be 85 or younger")]
    public int MaxAge { get; set; } = 45;

    public string OrderBy { get; set; } = "LastActive";

    public bool? HasPhoto { get; set; }

    public bool? IsOnline { get; set; }

    public bool? AvailableNow { get; set; }

    public string? City { get; set; }

    public List<int>? InterestTagIds { get; set; }

    public string Values
    {
        get 
        { 
            return $"MinAge({MinAge})-MaxAge({MaxAge})-Gender({Gender})-OrderBy({OrderBy})-HasPhoto({HasPhoto})-IsOnline({IsOnline})-AvailableNow({AvailableNow})-City({City})-PageSize({PageSize})-PageNumber({PageNumber})";
        }
    }
}
