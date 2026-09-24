using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public class InterestTag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public InterestCategory Category { get; set; } = InterestCategory.Interest;

    public bool IsCurated { get; set; }

    public ICollection<UserInterestTag> UserTags { get; set; } = [];
}
