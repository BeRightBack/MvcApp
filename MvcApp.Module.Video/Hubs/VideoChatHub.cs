using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Module.Video.Hubs;

public record VideoSlotOccupant(string ConnectionId, string UserId, string Username);

public class VideoRoomState
{
    public VideoRoomState(string roomId, int maxSpots)
    {
        RoomId = roomId;
        Slots = new VideoSlotOccupant?[maxSpots];
    }

    public string RoomId { get; }

    public int MaxSpots => Slots.Length;

    public VideoSlotOccupant?[] Slots { get; }

    // connectionId -> viewer identity (joined but not broadcasting). Anyone can join as a viewer.
    public Dictionary<string, VideoSlotOccupant> Viewers { get; } = new();

    public int ParticipantCount
    {
        get
        {
            var count = Viewers.Count;
            for (var i = 0; i < Slots.Length; i++)
                if (Slots[i] is not null) count++;
            return count;
        }
    }
}

/// <summary>
/// WebRTC signaling + slot-state hub. Media (audio/video) never flows through
/// this server â€” it is exchanged peer-to-peer between browsers. The hub only
/// relays SDP offers/answers and ICE candidates and tracks which of the (max 10)
/// broadcast spots are occupied, plus online presence for 1-on-1 calls.
/// </summary>
[Authorize]
public class VideoChatHub(
    UserManager<UserDetails> userManager,
    IRepository<VideoRoom> roomRepo,
    IRepository<VideoRoomMessage> messageRepo,
    IUnitOfWork unitOfWork,
    IBanService banService) : Hub
{
    private async Task EnsureNotBannedAsync(string userId)
    {
        if (await banService.IsBannedAsync(userId))
            throw new HubException("Your account is suspended.");
    }

    // userId -> connectionId (latest wins; used for 1-on-1 call invitations)
    private static readonly ConcurrentDictionary<string, string> _connections = new();

    // roomId -> live slot/viewer state. roomId is the numeric VideoRoom id for
    // group rooms ("3") or a synthetic id like "dm:userA:userB" for private calls.
    private static readonly ConcurrentDictionary<string, VideoRoomState> _rooms = new();

    public override Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
            _connections[userId] = Context.ConnectionId;
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null && _connections.TryGetValue(userId, out var conn) && conn == Context.ConnectionId)
            _connections.TryRemove(userId, out _);

        foreach (var pair in _rooms)
        {
            var state = pair.Value;
            var wasViewer = false;
            var hadSlot = false;
            var freedSlot = -1;
            lock (state)
            {
                wasViewer = state.Viewers.Remove(Context.ConnectionId);
                for (var i = 0; i < state.Slots.Length; i++)
                {
                    if (state.Slots[i]?.ConnectionId == Context.ConnectionId)
                    {
                        state.Slots[i] = null;
                        freedSlot = i;
                        hadSlot = true;
                    }
                }
            }

            if (hadSlot)
                await Clients.Group(state.RoomId).SendAsync("SlotVacated", freedSlot);
            if (wasViewer)
                await Clients.Group(state.RoomId).SendAsync("ParticipantLeft", Context.ConnectionId);

            if (hadSlot || wasViewer)
            {
                await Clients.Group(state.RoomId).SendAsync("ViewerCountChanged", GetViewerCount(state));
            }
            else if (state.ParticipantCount == 0)
            {
                _rooms.TryRemove(pair.Key, out _);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task<UserDetails?> GetUserAsyncSafe()
    {
        if (Context.User is null) return null;
        return await userManager.GetUserAsync(Context.User);
    }

    // ----- Room lifecycle -----

    public async Task JoinRoom(string roomId)
    {
        var user = await GetUserAsyncSafe();
        if (user == null) return;
        await EnsureNotBannedAsync(user.Id);

        var maxSpots = 10;
        if (roomId.StartsWith("dm:", StringComparison.Ordinal))
        {
            maxSpots = 2;
        }
        else if (int.TryParse(roomId, out var id))
        {
            var room = await roomRepo.GetFirstOrDefaultAsync(r => r.Id == id && r.IsActive);
            if (room == null) return;
            maxSpots = Math.Clamp(room.MaxSpots, 1, 10);
        }
        else
        {
            return;
        }

        if (userId is not null) _connections[userId] = Context.ConnectionId;

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        var state = _rooms.GetOrAdd(roomId, r => new VideoRoomState(roomId, maxSpots));
        var viewer = new VideoSlotOccupant(Context.ConnectionId, user.Id, user.UserName ?? user.Id);
        lock (state)
        {
            state.Viewers[Context.ConnectionId] = viewer;
        }

        var slots = state.Slots.Select(s => s is null ? null : new { s.ConnectionId, s.UserId, s.Username }).ToArray();
        var viewers = state.Viewers.Values.Select(v => new { v.ConnectionId, v.UserId, v.Username }).ToArray();
        await Clients.Caller.SendAsync("RoomState", new { roomId, maxSpots = state.MaxSpots, slots, viewers });
        await Clients.Group(roomId).SendAsync("ViewerCountChanged", GetViewerCount(state));
        await Clients.OthersInGroup(roomId).SendAsync("ParticipantJoined", new { viewer.ConnectionId, viewer.UserId, viewer.Username });
    }

    private string? userId => Context.UserIdentifier;

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        if (!_rooms.TryGetValue(roomId, out var state)) return;

        var wasViewer = false;
        var freedSlot = -1;
        lock (state)
        {
            wasViewer = state.Viewers.Remove(Context.ConnectionId);
            for (var i = 0; i < state.Slots.Length; i++)
            {
                if (state.Slots[i]?.ConnectionId == Context.ConnectionId)
                {
                    state.Slots[i] = null;
                    freedSlot = i;
                }
            }
        }

        if (freedSlot >= 0)
            await Clients.Group(roomId).SendAsync("SlotVacated", freedSlot);
        if (wasViewer)
            await Clients.Group(roomId).SendAsync("ParticipantLeft", Context.ConnectionId);
        await Clients.Group(roomId).SendAsync("ViewerCountChanged", GetViewerCount(state));
    }

    public async Task TakeSlot(string roomId, int slotIndex)
    {
        var user = await GetUserAsyncSafe();
        if (user == null) return;
        await EnsureNotBannedAsync(user.Id);
        if (!_rooms.TryGetValue(roomId, out var state)) return;

        var viewerRemoved = false;
        lock (state)
        {
            if (slotIndex < 0 || slotIndex >= state.Slots.Length) return;
            if (state.Slots[slotIndex] != null) return;

            for (var i = 0; i < state.Slots.Length; i++)
            {
                if (state.Slots[i]?.ConnectionId == Context.ConnectionId)
                    state.Slots[i] = null;
            }

            state.Slots[slotIndex] = new VideoSlotOccupant(Context.ConnectionId, user.Id, user.UserName ?? user.Id);
            viewerRemoved = state.Viewers.Remove(Context.ConnectionId);
        }

        var occupant = state.Slots[slotIndex];
        await Clients.Group(roomId).SendAsync("SlotOccupied", slotIndex, occupant);
        if (viewerRemoved)
            await Clients.Group(roomId).SendAsync("ParticipantLeft", Context.ConnectionId);
        await Clients.Group(roomId).SendAsync("ViewerCountChanged", GetViewerCount(state));
    }

    public async Task LeaveSlot(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var state)) return;

        var user = await GetUserAsyncSafe();
        var viewer = user is null ? null : new VideoSlotOccupant(Context.ConnectionId, user.Id, user.UserName ?? user.Id);
        var freedSlot = -1;
        var addedViewer = false;
        lock (state)
        {
            for (var i = 0; i < state.Slots.Length; i++)
            {
                if (state.Slots[i]?.ConnectionId == Context.ConnectionId)
                {
                    state.Slots[i] = null;
                    freedSlot = i;
                }
            }

            if (viewer is not null)
            {
                state.Viewers[Context.ConnectionId] = viewer;
                addedViewer = true;
            }
        }

        if (freedSlot >= 0)
            await Clients.Group(roomId).SendAsync("SlotVacated", freedSlot);
        if (addedViewer)
            await Clients.Group(roomId).SendAsync("ParticipantJoined", new { viewer!.ConnectionId, viewer.UserId, viewer.Username });
        await Clients.Group(roomId).SendAsync("ViewerCountChanged", GetViewerCount(state));
    }

    // ----- WebRTC signaling relay (group rooms + private calls) -----

    public Task SendOffer(string targetConnectionId, string sdp) =>
        Clients.Client(targetConnectionId).SendAsync("ReceiveOffer", Context.ConnectionId, sdp);

    public Task SendAnswer(string targetConnectionId, string sdp) =>
        Clients.Client(targetConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, sdp);

    public Task SendIce(string targetConnectionId, string candidate) =>
        Clients.Client(targetConnectionId).SendAsync("ReceiveIce", Context.ConnectionId, candidate);

    // ----- 1-on-1 call invitations -----

    public async Task CallUser(string targetUserId, string roomId)
    {
        var caller = await GetUserAsyncSafe();
        if (caller == null) return;
        await EnsureNotBannedAsync(caller.Id);

        if (!_connections.TryGetValue(targetUserId, out var targetConnection))
        {
            await Clients.Caller.SendAsync("UserOffline", targetUserId);
            return;
        }

        await Clients.Client(targetConnection).SendAsync("IncomingCall", new
        {
            callerId = caller.Id,
            callerName = caller.UserName,
            callerConnectionId = Context.ConnectionId,
            roomId
        });
        await Clients.Caller.SendAsync("CallRinging", targetUserId);
    }

    public async Task AcceptCall(string targetUserId, string roomId)
    {
        var callee = await GetUserAsyncSafe();
        if (callee == null) return;
        await EnsureNotBannedAsync(callee.Id);

        if (_connections.TryGetValue(targetUserId, out var callerConnection))
        {
            await Clients.Client(callerConnection).SendAsync("CallAccepted", new
            {
                calleeId = callee.Id,
                calleeName = callee.UserName,
                calleeConnectionId = Context.ConnectionId,
                roomId
            });
        }
    }

    public async Task DeclineCall(string targetUserId)
    {
        var callee = await GetUserAsyncSafe();
        if (callee == null) return;

        if (_connections.TryGetValue(targetUserId, out var callerConnection))
        {
            await Clients.Client(callerConnection).SendAsync("CallDeclined", new
            {
                calleeId = callee.Id,
                calleeName = callee.UserName
            });
        }
    }

    public async Task HangUp(string targetUserId)
    {
        var me = await GetUserAsyncSafe();
        if (me == null) return;

        if (_connections.TryGetValue(targetUserId, out var targetConnection))
        {
            await Clients.Client(targetConnection).SendAsync("CallEnded", new { senderId = me.Id });
        }
    }

    // ----- Room text chat (group rooms only) -----

    public async Task SendVideoRoomMessage(string roomId, string content)
    {
        var sender = await GetUserAsyncSafe();
        if (sender == null || string.IsNullOrWhiteSpace(content)) return;
        await EnsureNotBannedAsync(sender.Id);
        if (roomId.StartsWith("dm:", StringComparison.Ordinal)) return;
        if (!int.TryParse(roomId, out var id)) return;

        var message = new VideoRoomMessage
        {
            VideoRoomId = id,
            SenderId = sender.Id,
            SenderUsername = sender.UserName,
            Content = content.Trim(),
            MessageSent = DateTime.UtcNow
        };

        await messageRepo.AddAsync(message);
        await unitOfWork.CompleteAsync();

        await Clients.Group(roomId).SendAsync("ReceiveVideoRoomMessage", sender.Id, sender.UserName, message.Content, message.MessageSent.ToString("o"));
    }

    private static int GetViewerCount(VideoRoomState state)
    {
        lock (state)
        {
            return state.Viewers.Count;
        }
    }
}
