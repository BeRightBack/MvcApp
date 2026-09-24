namespace MvcApp.Core
{
    public class BlogPostTag
    {
        public int PostId { get; set; }
        public BlogPost? Post { get; set; }

        public int TagId { get; set; }
        public BlogTag? Tag { get; set; }
    }
}
