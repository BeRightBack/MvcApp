namespace MvcApp.Core.Models;

public class ConversationSummaryModel
{
    public string MemberId { get; set; } = string.Empty;

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

    public string KnownAs { get; set; } = string.Empty;

    public string LastMessage { get; set; } = string.Empty;

    public DateTime LastMessageAt { get; set; }

    public int UnreadCount { get; set; }

    public DateTime LastActive { get; set; }
}
