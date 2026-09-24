namespace MvcApp.Core.Abstractions;

public interface IUnitOfWork
{
    ILikesRepository LikesRepository { get; }
    IMemberRepository MemberRepository { get; }
    IMessageRepository MessageRepository { get; }
    IAdminRepository AdminRepository { get; }
    IAccountRepository AccountRepository { get; }
    IAppreciationRepository AppreciationRepository { get; }
    IUserBlockRepository UserBlockRepository { get; }
    INotificationRepository NotificationRepository { get; }
    INotificationSettingsRepository NotificationSettingsRepository { get; }

    Task<bool> CompleteAsync();
    bool HasChanges();
}
