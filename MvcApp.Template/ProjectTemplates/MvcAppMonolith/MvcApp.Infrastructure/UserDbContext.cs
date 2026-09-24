using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MvcApp.Core;
using System.Reflection.Emit;
using System.Text.Json;
using System.Linq;

namespace MvcApp.Infrastructure
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : IdentityDbContext<UserDetails, UserRole, string>(options)
    {
        public virtual DbSet<UserLike> Likes { get; set; }
        public virtual DbSet<UserBlock> UserBlocks { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<UserNotificationSettings> UserNotificationSettings { get; set; }
        public virtual DbSet<UserBan> UserBans { get; set; }
        public virtual DbSet<Message> Messages { get; set; }
        public virtual DbSet<Photo> Photos { get; set; }

        public virtual DbSet<VisitorLog> VisitorLogs { get; set; }
        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<SystemSetting> SystemSettings { get; set; }
        public virtual DbSet<VerificationCodeRecord> VerificationCodes { get; set; }
        // Module DbSets — kept for seed functions and direct controller access; entity configs come from module assemblies via scanning
        public virtual DbSet<Event> Events { get; set; }
        public virtual DbSet<EventCategory> EventCategories { get; set; }
        public virtual DbSet<EventRSVP> EventRSVPs { get; set; }
        public virtual DbSet<InterestTag> InterestTags { get; set; }
        public virtual DbSet<UserInterestTag> UserInterestTags { get; set; }
        public virtual DbSet<ChatRoom> ChatRooms { get; set; }
        public virtual DbSet<ChatRoomMessage> ChatRoomMessages { get; set; }
        public virtual DbSet<ForumCategory> ForumCategories { get; set; }
        public virtual DbSet<Forum> Forums { get; set; }
        public virtual DbSet<ForumThread> ForumThreads { get; set; }
        public virtual DbSet<ForumPost> ForumPosts { get; set; }
        public virtual DbSet<BlogCategory> BlogCategories { get; set; }
        public virtual DbSet<BlogTag> BlogTags { get; set; }
        public virtual DbSet<BlogPost> BlogPosts { get; set; }
        public virtual DbSet<BlogPostTag> BlogPostTags { get; set; }
        public virtual DbSet<BlogComment> BlogComments { get; set; }
        public virtual DbSet<ContentPage> ContentPages { get; set; }
        public virtual DbSet<ContentPageSnippet> ContentPageSnippets { get; set; }
        public virtual DbSet<ProfileAppreciation> ProfileAppreciations { get; set; }
        public virtual DbSet<ProductCategory> ProductCategories { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        // IPTV module entities
        public virtual DbSet<Subscription> Subscriptions { get; set; }
        public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public virtual DbSet<SubscriptionDetail> SubscriptionDetails { get; set; }
        public virtual DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        public virtual DbSet<IptvOrder> IptvOrders { get; set; }
        public virtual DbSet<Payment> Payments { get; set; }
        public virtual DbSet<VideoUpload> VideoUploads { get; set; }
        public virtual DbSet<VerificationRequest> VerificationRequests { get; set; }
        public virtual DbSet<Report> Reports { get; set; }
        public virtual DbSet<Badge> Badges { get; set; }
        public virtual DbSet<UserBadge> UserBadges { get; set; }
        public virtual DbSet<PointTransaction> PointTransactions { get; set; }
        public virtual DbSet<SuperLike> SuperLikes { get; set; }
        public virtual DbSet<UserBoost> UserBoosts { get; set; }

        //private static readonly int[] convertFromProviderExpression = [18, 100];

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //builder.Entity<UserDetails>(entity =>
            //{
            //    entity.ToTable(name: "User");

            //    // Configure ReportedUsers to be stored as JSON
            //    entity.Property(e => e.ReportedUsers)
            //        .HasConversion(
            //            v => JsonSerializer.Serialize(v),
            //            v => JsonSerializer.Deserialize<List<Guid>>(v) ?? new List<Guid>()
            //        )
            //        .HasColumnType("json");

            //    // Configure Interests to be stored as JSON
            //    entity.Property(e => e.Interests)
            //        .HasConversion(
            //            v => JsonSerializer.Serialize(v),
            //            v => JsonSerializer.Deserialize<string[]>(v) ?? Array.Empty<string>()
            //        )
            //        .HasColumnType("json");

            //    // Configure AgeRange to be stored as JSON
            //    entity.Property(e => e.AgeRange)
            //        .HasConversion(
            //            v => JsonSerializer.Serialize(v),
            //            v => JsonSerializer.Deserialize<int[]>(v) ?? convertFromProviderExpression)
            //        .HasColumnType("json");
            //});

            // 1. Create a reusable value comparer for your string collections
            //var stringListComparer = new ValueComparer<string[]>(
            //    (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
            //    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
            //    c => c.ToArray()
            //);

            // 2. Map your UserDetails collection properties completely
            builder.Entity<UserDetails>(entity =>
            {
                entity.ToTable("User");
                entity.Property(e => e.Interests)
                      .HasConversion(
                          v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                          v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? new string[0]
                      )
                      .Metadata.SetValueComparer(new ValueComparer<string[]>(
                          (c1, c2) => c1!.SequenceEqual(c2!),
                          c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                          c => c.ToArray()
                      ));

                entity.Property(e => e.ReportedUsers)
                        .HasConversion(
                            // Converts List<Guid> to a JSON string for the DB
                            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                            // Converts JSON string back to List<Guid> for C#
                            v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
                        )
                        .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                            // 1. Check equality element-by-element
                            (c1, c2) => c1!.SequenceEqual(c2!),
                            // 2. Generate a collective hash code
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            // 3. Create a snapshot copy for change tracking
                            c => c.ToList()
                        ));

                entity.Property(e => e.AgeRange)
                      .HasConversion(
                          v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                          v => JsonSerializer.Deserialize<int[]>(v, (JsonSerializerOptions?)null) ?? new int[0]
                      )
                      .Metadata.SetValueComparer(new ValueComparer<int[]>(
                          (c1, c2) => c1!.SequenceEqual(c2!),
                          c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                          c => c.ToArray()
                      ));
            });


            builder.Entity<UserRole>(entity =>
            {
                entity.ToTable(name: "Role");
            });
            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
            });
            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
            });
            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
            });
            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");
            });
            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
            });


            // 3. Fix the Photo relationship with matching string data types
            builder.Entity<Photo>()
                .HasOne(p => p.UserDetails)
                .WithMany(u => u.Photos)
                .HasForeignKey(p => p.UserDetailsId) // Points to your newly updated string property
                .IsRequired();


            builder.Entity<UserLike>()
            .HasKey(k => new { k.SourceUserId, k.LikedUserId });

            builder.Entity<UserLike>()
                .HasOne(s => s.SourceUser)
                .WithMany(l => l.LikedUsers)
                .HasForeignKey(s => s.SourceUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<UserLike>()
                .HasOne(s => s.LikedUser)
                .WithMany(l => l.LikedByUsers)
                .HasForeignKey(s => s.LikedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserBlock>()
                .HasKey(k => new { k.SourceUserId, k.BlockedUserId });

            builder.Entity<UserBlock>()
                .HasOne(s => s.SourceUser)
                .WithMany()
                .HasForeignKey(s => s.SourceUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<UserBlock>()
                .HasOne(s => s.BlockedUser)
                .WithMany()
                .HasForeignKey(s => s.BlockedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.IsRead });
                entity.Property(e => e.Type).HasMaxLength(20);
                entity.Property(e => e.ActorUsername).HasMaxLength(50);
                entity.Property(e => e.Message).HasMaxLength(500);
                entity.Property(e => e.Url).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserNotificationSettings>(entity =>
            {
                entity.ToTable("UserNotificationSettings");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.EmailOnLike).HasDefaultValue(true);
                entity.Property(e => e.EmailOnMatch).HasDefaultValue(true);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserBan>(entity =>
            {
                entity.ToTable("UserBans");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.BannedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.BannedBy)
                    .WithMany()
                    .HasForeignKey(e => e.BannedById)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.RevokedBy)
                    .WithMany()
                    .HasForeignKey(e => e.RevokedById)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<Message>()
                .HasOne(m => m.Recipient)
                .WithMany(m => m.MessagesReceived)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(m => m.MessagesSent)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<VisitorLog>(entity =>
            {
                entity.Property(e => e.Country).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.City).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.Region).HasMaxLength(100).IsRequired(false);
            });

            builder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.Property(e => e.UserId).HasMaxLength(255);
                entity.Property(e => e.UserName).HasMaxLength(255);
                entity.Property(e => e.Action).HasMaxLength(100);
                entity.Property(e => e.Entity).HasMaxLength(100);
                entity.Property(e => e.EntityId).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.Action);
            });

            builder.Entity<ProfileAppreciation>(entity =>
            {
                entity.ToTable("ProfileAppreciations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Value).IsRequired();

                entity.HasOne(e => e.SourceUser)
                    .WithMany()
                    .HasForeignKey(e => e.SourceUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.TargetUser)
                    .WithMany()
                    .HasForeignKey(e => e.TargetUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SystemSetting>(entity =>
            {
                entity.ToTable("SystemSettings");
                entity.Property(e => e.Key).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Value).HasColumnType("TEXT");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Group).HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.HasIndex(e => e.Key).IsUnique();
            });

            builder.Entity<VerificationCodeRecord>(entity =>
            {
                entity.ToTable("VerificationCodes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => new { e.UserId, e.IsUsed, e.ExpiresAt });
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.ExpiresAt).IsRequired();
            });

            builder.Entity<UserInterestTag>()
                .ToTable("UserInterestTags")
                .HasKey(e => new { e.UserId, e.TagId });

            builder.Entity<UserInterestTag>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserInterestTag>()
                .HasOne(e => e.Tag)
                .WithMany(t => t.UserTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InterestTag>()
                .ToTable("InterestTags")
                .Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Entity<InterestTag>()
                .HasIndex(e => new { e.Name, e.Category })
                .IsUnique();

            // Gamification
            builder.Entity<Badge>()
                .ToTable("Badges")
                .HasKey(e => e.Id);

            builder.Entity<Badge>()
                .Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Entity<Badge>()
                .HasIndex(e => e.Name)
                .IsUnique();

            builder.Entity<UserBadge>()
                .ToTable("UserBadges")
                .HasKey(e => e.Id);

            builder.Entity<UserBadge>()
                .HasIndex(e => new { e.UserId, e.BadgeId })
                .IsUnique();

            builder.Entity<UserBadge>()
                .HasOne(e => e.User)
                .WithMany(u => u.UserBadges)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserBadge>()
                .HasOne(e => e.Badge)
                .WithMany()
                .HasForeignKey(e => e.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PointTransaction>()
                .ToTable("PointTransactions")
                .HasKey(e => e.Id);

            builder.Entity<PointTransaction>()
                .HasOne(e => e.User)
                .WithMany(u => u.PointTransactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PointTransaction>()
                .HasIndex(e => new { e.UserId, e.CreatedAt });

            builder.Entity<SuperLike>()
                .ToTable("SuperLikes")
                .HasKey(e => e.Id);

            builder.Entity<SuperLike>()
                .HasOne(e => e.SourceUser)
                .WithMany(u => u.SuperLikesSent)
                .HasForeignKey(e => e.SourceUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SuperLike>()
                .HasOne(e => e.TargetUser)
                .WithMany(u => u.SuperLikesReceived)
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SuperLike>()
                .HasIndex(e => new { e.SourceUserId, e.TargetUserId })
                .IsUnique();

            builder.Entity<SuperLike>()
                .HasIndex(e => e.TargetUserId);

            builder.Entity<UserBoost>()
                .ToTable("UserBoosts")
                .HasKey(e => e.Id);

            builder.Entity<UserBoost>()
                .HasOne(e => e.User)
                .WithMany(u => u.Boosts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserBoost>()
                .HasIndex(e => new { e.UserId, e.EndDate });

            // Apply module entity configurations via dynamic assembly scanning
            foreach (var assembly in ModuleConfigurationRegistry.Assemblies)
            {
                builder.ApplyConfigurationsFromAssembly(assembly);
            }
        }
    }
}
