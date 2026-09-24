using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using MvcApp.Core;
using MvcApp.Core.Abstractions;


namespace MvcApp.Common.Hubs;

[Authorize]
public class ChatHub(
    UserManager<UserDetails> userManager,
    IMessageRepository messageRepo,
    IUnitOfWork unitOfWork,
    IRepository<ChatRoomMessage> roomMessageRepo,
    IUserBlockRepository blockRepo,
    ILikesRepository likesRepo,
    IBanService banService,
    IGamificationService gamification) : Hub
{
    private async Task EnsureNotBannedAsync(string userId)
    {
        if (await banService.IsBannedAsync(userId))
            throw new HubException("Your account is suspended.");
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendPrivateMessage(string recipientId, string content)
    {
        var sender = await userManager.GetUserAsync(Context.User);
        if (sender == null || string.IsNullOrWhiteSpace(content))
            return;

        await EnsureNotBannedAsync(sender.Id);

        if (await blockRepo.IsBlockedAsync(sender.Id, recipientId))
            throw new HubException("You cannot message this member.");

        var matches = await likesRepo.GetMatchesAsync(sender.Id);
        if (matches.All(m => m.Id != recipientId))
            throw new HubException("You can only message your matches.");

        var recipient = await userManager.FindByIdAsync(recipientId);
        if (recipient != null && recipient.MessagingPermission == MessagingPermission.VipOnly && !sender.IsVip)
            throw new HubException("This member only accepts messages from VIP users.");

        var message = new Message
        {
            SenderId = sender.Id,
            SenderUsername = sender.UserName,
            RecipientId = recipientId,
            RecipientUsername = (await userManager.FindByIdAsync(recipientId))?.UserName ?? "unknown",
            Content = content.Trim(),
            MessageSent = DateTime.UtcNow
        };

        await messageRepo.CreateMessageAsync(message);
        await unitOfWork.CompleteAsync();

        await gamification.AwardPointsAsync(sender.Id, 5, "Sent a message", "Message", message.Id);

        var timestamp = message.MessageSent.ToString("o");
        await Clients.Group(recipientId).SendAsync("ReceiveMessage", sender.Id, sender.UserName, content, timestamp);
        await Clients.Caller.SendAsync("ReceiveMessage", sender.Id, sender.UserName, content, timestamp);
    }

    public async Task JoinRoom(int roomId)
    {
        var user = await userManager.GetUserAsync(Context.User);
        if (user == null) return;
        await EnsureNotBannedAsync(user.Id);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");
    }

    public async Task SendRoomMessage(int roomId, string content)
    {
        var sender = await userManager.GetUserAsync(Context.User);
        if (sender == null || string.IsNullOrWhiteSpace(content))
            return;

        await EnsureNotBannedAsync(sender.Id);

        var message = new ChatRoomMessage
        {
            ChatRoomId = roomId,
            SenderId = sender.Id,
            SenderUsername = sender.UserName,
            Content = content.Trim(),
            MessageSent = DateTime.UtcNow
        };

        await roomMessageRepo.AddAsync(message);
        await unitOfWork.CompleteAsync();

        var timestamp = message.MessageSent.ToString("o");
        await Clients.Group($"room_{roomId}").SendAsync("ReceiveRoomMessage", sender.Id, sender.UserName, content, timestamp, roomId);
    }

    public async Task MarkAsRead(string senderId)
    {
        var user = await userManager.GetUserAsync(Context.User);
        if (user == null) return;
    }
}
