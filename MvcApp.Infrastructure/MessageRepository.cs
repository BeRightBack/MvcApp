using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Models;
using MvcApp.Core.Pagination;
using MvcApp.Infrastructure.Helpers;
using MvcApp.Infrastructure.Extentions;

namespace MvcApp.Infrastructure;

public class MessageRepository(UserDbContext db, IUserBlockRepository blockRepo) : IMessageRepository
{
    public async Task CreateMessageAsync(Message message)
    {
        await db.Messages!.AddAsync(message);
    }

    public async Task<Message?> GetMessageAsync(int id)
    {
        return await db.Messages!.FindAsync(id);
    }

    public async Task<PaginationList<MessageModel>> GetMessagesForMemberAsync(MessageParameters messageParameters)
    {
        IQueryable<Message> messages = db.Messages!
            .Include(m => m.Sender)
            .ThenInclude(u => u!.Photos)
            .Include(m => m.Recipient)
            .ThenInclude(u => u!.Photos)
            .OrderByDescending(m => m.MessageSent)
            .AsQueryable();

        messages = messageParameters.Container switch
        {
            "Inbox" => messages.Where(u => u.RecipientUsername == messageParameters.Username && u.RecipientDeleted == false),
            "Sent" => messages.Where(u => u.SenderUsername == messageParameters.Username && u.SenderDeleted == false),
            _ => messages.Where(u => u.RecipientUsername == messageParameters.Username && u.DateRead == null && u.RecipientDeleted == false)
        };

        var messagesList = await messages
            .Skip((messageParameters.PageNumber - 1) * messageParameters.PageSize)
            .Take(messageParameters.PageSize)
            .ToListAsync();

        var messageModels = messagesList.Select(EntityMapper.MapToMessageModel).OfType<MessageModel>().ToList();

        var count = await messages.CountAsync();

        return new PaginationList<MessageModel>(
            messageModels,
            count,
            messageParameters.PageNumber,
            messageParameters.PageSize);
    }

    public async Task<IEnumerable<MessageModel>> GetMessageThreadAsync(string currentUsername, string recipientUsername)
    {
        List<Message> messages = await db.Messages!
            .Include(m => m.Sender)
            .ThenInclude(u => u!.Photos)
            .Include(m => m.Recipient)
            .ThenInclude(u => u!.Photos)
            .Where(m => m.Recipient!.UserName == currentUsername
                && m.Sender!.UserName == recipientUsername
                || m.Recipient.UserName == recipientUsername
                && m.Sender!.UserName == currentUsername)
            .OrderByDescending(m => m.MessageSent)
            .AsSplitQuery()
            .Take(10)
            .ToListAsync();

        messages.Reverse();

        List<MessageModel> messageModels = [.. messages.Select(EntityMapper.MapToMessageModel).OfType<MessageModel>()];

        List<MessageModel> unreadMessages = [.. messageModels
            .Where(m => m.DateRead == null && m.RecipientUsername == currentUsername)];

        if (unreadMessages.Count > 0)
        {
            foreach (MessageModel message in unreadMessages)
            {
                message.DateRead = DateTime.UtcNow;
            }
        }

        return messageModels;
    }

    public async Task<List<ConversationSummaryModel>> GetConversationSummariesAsync(string userId)
    {
        var blocked = await blockRepo.GetBlockedUserIdsAsync(userId);
        var blockers = await blockRepo.GetBlockersOfAsync(userId);

        var messages = await db.Messages!
            .Include(m => m.Sender)
            .ThenInclude(u => u!.Photos)
            .Include(m => m.Recipient)
            .ThenInclude(u => u!.Photos)
            .Where(m => (m.RecipientId == userId && !m.RecipientDeleted)
                || (m.SenderId == userId && !m.SenderDeleted))
            .OrderByDescending(m => m.MessageSent)
            .AsSplitQuery()
            .ToListAsync();

        var summaries = messages
            .Where(m => !blocked.Contains(m.SenderId == userId ? m.RecipientId : m.SenderId)
                && !blockers.Contains(m.SenderId == userId ? m.RecipientId : m.SenderId))
            .GroupBy(m => m.SenderId == userId ? m.RecipientId : m.SenderId)
            .Select(g =>
            {
                var latest = g.First();
                var other = latest.SenderId == userId ? latest.Recipient : latest.Sender;
                return new ConversationSummaryModel
                {
                    MemberId = other?.Id ?? g.Key,
                    Username = other?.UserName ?? string.Empty,
                    KnownAs = other?.KnownAs ?? string.Empty,
                    Age = other?.DateOfBirth.CalculateAge() ?? 0,
                    MainPhotoFilename = other?.Photos?.FirstOrDefault(p => p.IsMain)?.Filename ?? string.Empty,
                    LastMessage = latest.Content ?? string.Empty,
                    LastMessageAt = latest.MessageSent,
                    UnreadCount = g.Count(x => x.RecipientId == userId && x.DateRead == null),
                    LastActive = other?.LastActive ?? DateTime.MinValue
                };
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();

        return summaries;
    }

    public Tuple<string, string> DeleteMessageAsync(string requestUser, int id)
    {
        Message? message = db.Messages!.FirstOrDefault(m => m.Id == id);

        if (message is null)
        {
            throw new ArgumentException($"Message with id={id} was not found");
        }

        if (message.SenderUsername != requestUser 
            && message.RecipientUsername != requestUser)
        {
            throw new UnauthorizedAccessException($"Request user [{requestUser}] is not authorized to delete this message");
        }

        if (message.SenderUsername == requestUser)
        {
            message.SenderDeleted = true;
        }

        if (message.RecipientUsername == requestUser)
        {
            message.RecipientDeleted = true;
        }

        if (message.SenderDeleted && message.RecipientDeleted)
        {
            db.Messages!.Remove(message);
        }

        return new Tuple<string, string>(message.SenderUsername ?? string.Empty, message.RecipientUsername ?? string.Empty);
    }
}
