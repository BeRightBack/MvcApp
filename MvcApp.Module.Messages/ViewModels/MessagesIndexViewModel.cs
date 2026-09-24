using MvcApp.Core.Models;

namespace MvcApp.Module.Messages.ViewModels;

public class MessagesIndexViewModel
{
    public List<LikedMemberModel> Suggestions { get; set; } = new();

    public List<ConversationSummaryModel> Conversations { get; set; } = new();
}
