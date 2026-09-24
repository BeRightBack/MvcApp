namespace MvcApp.Core
{
    public class Feedback(string? content, Guid? senderId)
    {
        public Guid Id = Guid.NewGuid();
        public string? Content { get; set; } = content;
        public Guid? SenderId { get; set; } = senderId;
    }
}
