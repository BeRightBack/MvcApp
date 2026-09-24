namespace MvcApp.Core
{
    public class UserLike
    {
        public UserDetails? SourceUser { get; set; }
        public required string SourceUserId { get; set; }
        public UserDetails? LikedUser { get; set; }
        public required string LikedUserId { get; set; }
    }
}
