namespace MvcApp.Core
{
    public class ChatGroup(Guid[] users)
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid[] Users { get; set; } = users;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
