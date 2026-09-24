using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MvcApp.Core.Abstractions;

namespace MvcApp.Common.Hubs;

[Authorize]
public class NotificationHub(INotificationRepository notificationRepo) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<int> GetUnreadCount()
    {
        var userId = Context.UserIdentifier;
        if (userId == null)
        {
            return 0;
        }
        return await notificationRepo.GetUnreadCountAsync(userId);
    }
}
