using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class Photo
    {
        public int Id { get; set; }
        public string Filename { get; set; } = "";

        [Required]
        public bool IsMain { get; set; }

        public bool IsApproved { get; set; } = true;

        public PrivacyLevel PrivacyLevel { get; set; } = PrivacyLevel.Public;

        public required UserDetails UserDetails { get; set; }

        public required string UserDetailsId { get; set; }
    }
}
