namespace MvcApp.Core
{
    public class SuperLike
    {
        public int Id { get; set; }
        public required string SourceUserId { get; set; }
        public UserDetails? SourceUser { get; set; }
        public required string TargetUserId { get; set; }
        public UserDetails? TargetUser { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
