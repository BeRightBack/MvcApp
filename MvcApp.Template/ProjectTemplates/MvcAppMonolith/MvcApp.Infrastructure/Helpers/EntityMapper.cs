using MvcApp.Core;
using MvcApp.Core.Models;
using MvcApp.Infrastructure.Extentions;
using MvcApp.Infrastructure.Models;

namespace MvcApp.Infrastructure.Helpers;

public static class EntityMapper
{
    public static MemberModel? MapToMemberModel(UserDetails? user)
    {
        if (user == null) return null;

        return new MemberModel
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            MainPhotoFilename = user.Photos?.FirstOrDefault(x => x.IsMain && x.IsApproved)?.Filename ?? string.Empty,
            Age = user.DateOfBirth.CalculateAge(),
            KnownAs = user.KnownAs,
            Created = user.Created,
            LastActive = user.LastActive,
            Gender = user.Gender,
            Introduction = user.Introduction,
            LookingFor = user.LookingFor,
            Interests = string.Join(", ", user.Interests ?? []),
            City = user.City,
            State = user.State,
            IsAvailable = user.IsAvailable,
            IsVip = user.IsVip,
            IsVerified = user.IsVerified,
            MessagingPermission = user.MessagingPermission,
            Photos = user.Photos?.Where(p => p.IsApproved).Select(p => MapToPhotoModel(p, user.UserName ?? string.Empty))
                .OfType<PhotoModel>()
                .ToList() ?? [],
            InterestTags = user.UserInterestTags?.Select(ut => ut.Tag!).OfType<InterestTag>().ToList() ?? []
        };
    }

    public static PhotoModel? MapToPhotoModel(Photo? photo, string? username = null)
    {
        if (photo == null) return null;

        return new PhotoModel
        {
            Id = photo.Id,
            Filename = photo.Filename,
            IsMain = photo.IsMain,
            IsApproved = photo.IsApproved,
            PrivacyLevel = photo.PrivacyLevel,
            Username = username ?? string.Empty
        };
    }

    public static LikedMemberModel MapToLikedMemberModel(MemberModel member, bool isLikedByYou, bool isMutual, bool isSuperLikedByYou = false, bool isBoosted = false, bool isPhotoBlurred = false)
    {
        if (member == null) return new LikedMemberModel();

        return new LikedMemberModel
        {
            Id = member.Id,
            Username = member.Username,
            MainPhotoFilename = member.MainPhotoFilename,
            Age = member.Age,
            KnownAs = member.KnownAs,
            Created = member.Created,
            LastActive = member.LastActive,
            Gender = member.Gender,
            Introduction = member.Introduction,
            LookingFor = member.LookingFor,
            Interests = member.Interests,
            City = member.City,
            State = member.State,
            Photos = member.Photos,
            InterestTags = member.InterestTags,
            IsAvailable = member.IsAvailable,
            IsVip = member.IsVip,
            IsVerified = member.IsVerified,
            CacheTime = member.CacheTime,
            IsLikedByYou = isLikedByYou,
            IsMutual = isMutual,
            IsSuperLikedByYou = isSuperLikedByYou,
            IsBoosted = isBoosted,
            IsPhotoBlurred = isPhotoBlurred
        };
    }

    public static MessageModel? MapToMessageModel(Message? message)
    {
        if (message == null) return null;

        return new MessageModel
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderUsername = message.SenderUsername ?? string.Empty,
            SenderPhotoUrl = message.Sender?.Photos?.FirstOrDefault(x => x.IsMain)?.Filename ?? string.Empty,
            RecipientId = message.RecipientId,
            RecipientUsername = message.RecipientUsername ?? string.Empty,
            RecipientPhotoUrl = message.Recipient?.Photos?.FirstOrDefault(x => x.IsMain)?.Filename ?? string.Empty,
            Content = message.Content ?? string.Empty,
            DateRead = message.DateRead,
            MessageSent = message.MessageSent,
            SenderDeleted = message.SenderDeleted,
            RecipientDeleted = message.RecipientDeleted
        };
    }

    public static void MapMemberUpdateToAppUser(MemberUpdateModel source, UserDetails destination)
    {
        if (source == null || destination == null) return;

        destination.Introduction = source.Introduction;
        destination.LookingFor = source.LookingFor;
        destination.Interests = string.IsNullOrWhiteSpace(source.Interests) 
            ? [] 
            : source.Interests.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        destination.City = source.City;
        destination.State = source.State;
    }
}
