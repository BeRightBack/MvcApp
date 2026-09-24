using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class VerificationCodeRecord
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public required string UserId { get; set; }

        [Required]
        public required string EncryptedCode { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }

        public required string Email { get; set; }

        // Additional security fields
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
