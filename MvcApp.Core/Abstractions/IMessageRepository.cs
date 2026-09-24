using MvcApp.Core;
using MvcApp.Core.Models;
using MvcApp.Core.Pagination;

namespace MvcApp.Core.Abstractions;

public interface IMessageRepository
{
    Task CreateMessageAsync(Message message);
    Task<Message?> GetMessageAsync(int id);
    Task<PaginationList<MessageModel>> GetMessagesForMemberAsync(MessageParameters messageParameters);
    Task<IEnumerable<MessageModel>> GetMessageThreadAsync(string currentUsername, string recipientUsername);
    Task<List<ConversationSummaryModel>> GetConversationSummariesAsync(string userId);
    Tuple<string, string> DeleteMessageAsync(string requestUser, int id);
}
