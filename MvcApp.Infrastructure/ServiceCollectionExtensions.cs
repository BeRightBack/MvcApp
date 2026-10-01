using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure.Seeding;
using MvcApp.Infrastructure.Seeding.Packs;

using MySql.EntityFrameworkCore.Extensions;

namespace MvcApp.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("IdentityDbConnection");

            services.AddDbContext<UserDbContext>(options => options.UseMySQL( conn!));

            services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
            services.Configure<IpGeolocationOptions>(configuration.GetSection("IpGeolocation"));
            services.Configure<DeepLOptions>(configuration.GetSection("DeepLConfig"));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<SeedPackService>();
            services.AddScoped<SeedPackPlanner>();
            services.AddScoped<ISeedPack, CommunityPack>();
            services.AddScoped<ISeedPack, BlogPack>();
            services.AddScoped<ISeedPack, DatingPack>();
            services.AddScoped<ISeedPack, PlansPack>();
            services.AddScoped<ISeedPack, ChatPack>();
            services.AddScoped<ISeedPack, EventsPack>();
            services.AddScoped<ISeedPack, GamificationPack>();
            services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IAdminRepository, AdminRepository>();
            services.AddScoped<IMemberRepository, MemberRepository>();
            services.AddScoped<ILikesRepository, LikesRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IAppreciationRepository, AppreciationRepository>();
            services.AddScoped<IUserBlockRepository, UserBlockRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<INotificationSettingsRepository, NotificationSettingsRepository>();

            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddSingleton<SettingsCache>();
            services.AddScoped<IBanService, BanService>();
            services.AddScoped<IGamificationService, GamificationService>();
            services.AddScoped<ISuperLikeService, SuperLikeService>();
            services.AddScoped<ISuperLikeRepository, SuperLikeRepository>();
            services.AddHttpContextAccessor();

            return services;
        }
    }
}
