using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure;

public class UnitOfWork(
    ILikesRepository likesRepository,
    IMemberRepository memberRepository,
    IMessageRepository messageRepository,
    IAdminRepository adminRepository,
    IAccountRepository accountRepository,
    IAppreciationRepository appreciationRepository,
    IUserBlockRepository userBlockRepository,
    INotificationRepository notificationRepository,
    INotificationSettingsRepository notificationSettingsRepository,
    UserDbContext db) : IUnitOfWork
{
    public ILikesRepository LikesRepository => likesRepository;
    public IMemberRepository MemberRepository => memberRepository;
    public IMessageRepository MessageRepository => messageRepository;
    public IAdminRepository AdminRepository => adminRepository;
    public IAccountRepository AccountRepository => accountRepository;
    public IAppreciationRepository AppreciationRepository => appreciationRepository;
    public IUserBlockRepository UserBlockRepository => userBlockRepository;
    public INotificationRepository NotificationRepository => notificationRepository;
    public INotificationSettingsRepository NotificationSettingsRepository => notificationSettingsRepository;

    public async Task<bool> CompleteAsync()
    {
        return await db.SaveChangesAsync() > 0;
    }

    public bool HasChanges()
    {
        return db.ChangeTracker.HasChanges();
    }
}
