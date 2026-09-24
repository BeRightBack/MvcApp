namespace MvcApp.Core.Models;

public class PhotoModel
{
    public int Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public bool IsMain { get; set; }

    public bool IsApproved { get; set; }

    public PrivacyLevel PrivacyLevel { get; set; }

    public string Username { get; set; }= string.Empty;

    public string PhotoUrl
    {
        get
        {
            if (string.IsNullOrEmpty(Filename))
                return "/images/user.svg";

            if (Filename.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                Filename.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return Filename;

            return $"/Photos/{Username}/{Filename}";
        }
    }
}
