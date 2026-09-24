namespace MvcApp.Core
{
    public class GroupMessage(string senderId, string message)
    {
        public string SenderId { get; set; } = senderId;
        public string Message { get; set; } = message;
    }
}
