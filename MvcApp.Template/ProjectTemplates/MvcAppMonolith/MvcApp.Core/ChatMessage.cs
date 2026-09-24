namespace MvcApp.Core
{
    public class ChatMessage(string message, Guid userId, Guid chatGroupId)
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = message;
        public Guid SenderId { get; set; } = userId;
        public Guid ChatGroupId { get; set; } = chatGroupId;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
