using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core.Models;

public class MemberModel
{
    public string Id { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string MainPhotoFilename { get; set; } = string.Empty;

    public string MainPhotoUrl
    {
        get
        {
            if (string.IsNullOrEmpty(MainPhotoFilename))
                return "/images/user.svg";

            if (MainPhotoFilename.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                MainPhotoFilename.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return MainPhotoFilename;

            return $"/Photos/{Username}/{MainPhotoFilename}";
        }
    }

    public int Age { get; set; }

    [MaxLength(50)]
    public string KnownAs { get; set; } = string.Empty;

    public DateTime Created { get; set; }

    public DateTime LastActive { get; set; } 

    [MaxLength(25)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Introduction { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string LookingFor { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Interests { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    public IList<PhotoModel> Photos { get; set; } = [];

    public IList<InterestTag> InterestTags { get; set; } = [];

    public bool IsAvailable { get; set; }

    public bool IsVip { get; set; }

    public bool IsVerified { get; set; }

    public MessagingPermission MessagingPermission { get; set; }

    public DateTime CacheTime { get; set; }
}
