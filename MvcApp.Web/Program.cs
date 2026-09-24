using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using MvcApp.Common.Filters;
using MvcApp.Common.Hubs;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Identity;
using MvcApp.Infrastructure;
using MvcApp.Web.Middlewares;
using MvcApp.Web.Health;
using MvcApp.Localization;
using MvcApp.Services;
using MvcApp.Module.Forum;
using MvcApp.Module.Blog;
using MvcApp.Module.Chat;
using MvcApp.Module.Messages;
using MvcApp.Module.Store;
using MvcApp.Module.IPTV;
using MvcApp.Module.Pages;
using MvcApp.Module.Ads;
using MvcApp.Module.Utility;
using MvcApp.Module.Video;
using MvcApp.Module.Video.Hubs;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Threading.RateLimiting;

// Serilog log-store connection string is read from configuration (appsettings.json overridable by the
// MvcApp__Serilog__ConnectionStrings__Logs environment variable) so that credentials never live in code.
// Find appsettings.json using the same content-root discovery as the rest of the app.
var serilogBase = Directory.GetCurrentDirectory();
if (!Directory.Exists(Path.Combine(serilogBase, "wwwroot")) || !File.Exists(Path.Combine(serilogBase, "appsettings.json")))
{
    var found = AppContext.BaseDirectory;
    foreach (var start in new[] { serilogBase, found })
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "wwwroot")) &&
                File.Exists(Path.Combine(dir.FullName, "appsettings.json")))
            {
                serilogBase = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }
        if (Directory.Exists(Path.Combine(serilogBase, "wwwroot")) &&
            File.Exists(Path.Combine(serilogBase, "appsettings.json"))) break;
    }
}

var logConfig = new ConfigurationBuilder()
    .SetBasePath(serilogBase)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var logsConnectionString = logConfig["Serilog:ConnectionStrings:Logs"];
if (string.IsNullOrWhiteSpace(logsConnectionString))
{
    throw new InvalidOperationException("Serilog:ConnectionStrings:Logs is not configured. Add it to appsettings.json or set the environment variable.");
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.MySQL(
        connectionString: logsConnectionString,
        tableName: "Logs",
        restrictedToMinimumLevel: LogEventLevel.Information
    )
    .WriteTo.File(path: "logs/log-.txt",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Information
    ).CreateLogger();

// When launched from bin\Debug\<tfm> (or from a different working directory) the content
// root may point at a folder without wwwroot/appsettings.json. Fall back to a directory
// that contains both, mirroring what `dotnet run` resolves.
static bool IsProjectRoot(string path) =>
    Directory.Exists(Path.Combine(path, "wwwroot")) &&
    File.Exists(Path.Combine(path, "appsettings.json"));

var contentRoot = Directory.GetCurrentDirectory();
if (!IsProjectRoot(contentRoot))
{
    string? found = null;
    foreach (var start in new[] { contentRoot, AppContext.BaseDirectory })
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (IsProjectRoot(dir.FullName))
            {
                found = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }
        if (found != null)
        {
            break;
        }
    }

    if (found != null)
    {
        contentRoot = found;
    }
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot
});



try
{

    // Register separated modules
    // Identity must be registered first because UnitOfWork depends on UserManager

    

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMvcAppIdentity(builder.Configuration);

    // Register optional modules — comment out to remove the feature
    builder.Services.AddForum();
    builder.Services.AddBlog();
    builder.Services.AddChat();
    builder.Services.AddVideo();
    builder.Services.AddMessages();
    builder.Services.AddStore();
    builder.Services.AddIptv();
    builder.Services.AddPages();
    builder.Services.AddAdsModule();
    builder.Services.AddUtility();

    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddHttpClient<MvcApp.Web.Services.VipPayPalService>();
    builder.Services.AddMvcAppLocalization(builder.Configuration);

    builder.Services.AddHttpContextAccessor();

    // Operational endpoints: /health reports database reachability for load balancers / uptime probes.
    builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

    // Global auth rate limiting (keyed per client IP) to blunt brute-force/spam against credential endpoints.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    });

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml" });
    });

    // Add session services
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    var mvcBuilder = builder.Services.AddControllersWithViews(options =>
    {
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
        options.Filters.Add(new MobileLayoutFilter());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

    // Discover controllers and pages from optional modules
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.Forum.ServiceRegistration).Assembly);
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.Blog.ServiceRegistration).Assembly);
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.Chat.ServiceRegistration).Assembly);
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.Video.ServiceRegistration).Assembly);
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.Messages.ServiceRegistration).Assembly);
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.Store.ServiceRegistration).Assembly);
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.IPTV.ServiceRegistration).Assembly);
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.Pages.ServiceRegistration).Assembly);
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.Ads.ServiceRegistration).Assembly);
    mvcBuilder.AddApplicationPart(typeof(MvcApp.Module.Utility.ServiceRegistration).Assembly);

    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.ConfigureFilter(new MobileLayoutPageFilter());
    })
    .AddRazorRuntimeCompilation() // Enable runtime compilation for debugging
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

    // Configure application parts to include Razor Pages from referenced projects
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(MvcApp.Identity.Pages.Account.LoginModel).Assembly);

    builder.Services.AddSignalR(o => o.EnableDetailedErrors = true);
    builder.Services.AddServerSideBlazor();


    builder.Services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
    });

    // Registers Serilog's DiagnosticContext (required by UseSerilogRequestLogging).
    builder.Services.AddSerilog();

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 104857600; // 100MB
    });



    builder.Services.AddScoped<SecureVerificationService>();

    var app = builder.Build();

    // Structured request logging via Serilog (duration, status, client IP).
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
        RequireHeaderSymmetry = false,
        ForwardLimit = null,
        KnownProxies = { IPAddress.Parse("127.0.0.1") } // or your Apache proxy IP
    });

    app.UseHttpsRedirection();

    var locOptions = ((IApplicationBuilder)app).ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>();
    app.UseRequestLocalization(locOptions.Value);

    // Response compression is disabled in Development: Visual Studio's Browser Link middleware
    // injects its script into the response body and corrupts the compressed stream, which makes
    // the browser fail with ERR_CONTENT_DECODING_FAILED (blank page).
    if (!app.Environment.IsDevelopment())
    {
        app.UseResponseCompression();
    }

    app.UseStaticFiles();

    // Serve the IPTV module's wwwroot under its _content path so views referencing
    // ~/_content/MvcApp.Module.IPTV/... work regardless of how the app is launched.
    var iptvWwwRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "MvcApp.Module.IPTV", "wwwroot"));
    if (Directory.Exists(iptvWwwRoot))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(iptvWwwRoot),
            RequestPath = "/_content/MvcApp.Module.IPTV"
        });
    }

    app.UseSession();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<LocalizationMiddleware>();
    app.UseMiddleware<SiteUnderMaintenanceMiddleware>();
    app.UseMiddleware<BannedUserMiddleware>();
    app.UseMiddleware<VisitorLoggingMiddleware>();

    app.MapAreaControllerRoute(
        name: "admin",
        areaName: "Admin",
        pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapRazorPages();
    app.MapBlazorHub();
    app.MapHub<ChatHub>("/chathub");
    app.MapHub<VideoChatHub>("/videohub");
    app.MapHub<NotificationHub>("/notificationhub");
    app.MapHealthChecks("/health");

    // Seed data in background while server starts
    if (app.Environment.IsDevelopment())
    {
        _ = Task.Run(async () =>
        {
            try
            {
                Log.Information("Starting background seeding...");
                await SeedLanguageAsync(app);
                await SeedSettingsAsync(app);
                await SeedChatRoomsAsync(app);
                await SeedForumAsync(app);
                await SeedBlogAsync(app);
                await SeedPageSnippetsAsync(app);
                await SeedEventCategoriesAsync(app);
                await SeedInterestTagsAsync(app);
                await SeedGamificationAsync(app);
                await SeedVipPlansAsync(app);
                RegisterBlazorPageWidgetAssemblies(app);
                app.SeedData();
                Log.Information("Background seeding completed.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error during background seeding");
            }
        });
    }

    try
    {
        Log.Information("Starting web host...");
        Console.WriteLine("Now listening on: http://localhost:9001");
        app.Run();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application start-up failed 1");
        throw;
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed 2");
    throw;
}
finally
{
    Log.CloseAndFlush();

}


static async Task SeedLanguageAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LocalizationDbContext>();

    // Apply migrations to create/update the database
    await context.Database.MigrateAsync();

    var service = scope.ServiceProvider.GetRequiredService<SeedLanguage>();
    await service.EnsureSeedLanguageAsync();
}

static async Task SeedSettingsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();
    await SettingsSeeder.SeedAsync(db);
}

static async Task SeedChatRoomsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();

    if (await db.ChatRooms.AnyAsync()) return;

    db.ChatRooms.AddRange(
        new ChatRoom { Name = "General Discussion", Description = "Talk about anything and everything" },
        new ChatRoom { Name = "Tech Support", Description = "Get help with technical issues" },
        new ChatRoom { Name = "Announcements", Description = "Official announcements and updates" }
    );
    await db.SaveChangesAsync();
}

static async Task SeedForumAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();

    if (await db.ForumCategories.AnyAsync()) return;

    var general = new ForumCategory { Name = "General", Description = "General discussions", SortOrder = 0 };
    var support = new ForumCategory { Name = "Support", Description = "Get help and support", SortOrder = 1 };
    var offTopic = new ForumCategory { Name = "Off-Topic", Description = "Anything not covered elsewhere", SortOrder = 2 };

    db.ForumCategories.AddRange(general, support, offTopic);
    await db.SaveChangesAsync();

    db.Forums.AddRange(
        new Forum { CategoryId = general.Id, Name = "Introductions", Description = "Introduce yourself to the community", SortOrder = 0 },
        new Forum { CategoryId = general.Id, Name = "General Discussion", Description = "Talk about anything", SortOrder = 1 },
        new Forum { CategoryId = support.Id, Name = "Technical Support", Description = "Get help with technical issues", SortOrder = 0 },
        new Forum { CategoryId = support.Id, Name = "Feature Requests", Description = "Suggest new features", SortOrder = 1 },
        new Forum { CategoryId = offTopic.Id, Name = "Random Chat", Description = "Casual conversation", SortOrder = 0 },
        new Forum { CategoryId = offTopic.Id, Name = "Games & Fun", Description = "Gaming discussions and fun threads", SortOrder = 1 }
    );
    await db.SaveChangesAsync();
}

static async Task SeedBlogAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();

    if (await db.BlogCategories.AnyAsync()) return;

    var tech = new BlogCategory { Name = "Technology", Slug = "technology", Description = "Tech news and tutorials", SortOrder = 0 };
    var news = new BlogCategory { Name = "News", Slug = "news", Description = "Company and product announcements", SortOrder = 1 };
    var guides = new BlogCategory { Name = "Guides", Slug = "guides", Description = "How-to guides and best practices", SortOrder = 2 };

    db.BlogCategories.AddRange(tech, news, guides);

    var gettingStarted = new BlogTag { Name = "Getting Started", Slug = "getting-started" };
    var tips = new BlogTag { Name = "Tips & Tricks", Slug = "tips-tricks" };
    var updates = new BlogTag { Name = "Updates", Slug = "updates" };
    var tutorial = new BlogTag { Name = "Tutorial", Slug = "tutorial" };

    db.BlogTags.AddRange(gettingStarted, tips, updates, tutorial);
    await db.SaveChangesAsync();
}

static async Task SeedPageSnippetsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();

    if (await db.ContentPageSnippets.AnyAsync()) return;

    foreach (var snippet in MvcApp.Module.Pages.Services.SnippetCatalog.Defaults)
        db.ContentPageSnippets.Add(snippet);
    await db.SaveChangesAsync();
}

static void RegisterBlazorPageWidgetAssemblies(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var registry = scope.ServiceProvider.GetRequiredService<MvcApp.Module.Pages.Services.BlazorComponentRegistry>();
    registry.RegisterAssembly(typeof(MvcApp.Razor.Components.Pages.Counter).Assembly);
}

static async Task SeedEventCategoriesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();

    if (await db.EventCategories.AnyAsync()) return;

    db.EventCategories.AddRange(
        new EventCategory { Name = "Party", Icon = "bx-party" },
        new EventCategory { Name = "Networking", Icon = "bx-group" },
        new EventCategory { Name = "Sports", Icon = "bx-dumbbell" },
        new EventCategory { Name = "Music", Icon = "bx-music" },
        new EventCategory { Name = "Food & Drink", Icon = "bx-food" },
        new EventCategory { Name = "Nightlife", Icon = "bx-moon" },
        new EventCategory { Name = "Outdoors", Icon = "bx-sun" },
        new EventCategory { Name = "Arts & Culture", Icon = "bx-palette" },
        new EventCategory { Name = "Tech", Icon = "bx-chip" },
        new EventCategory { Name = "Other", Icon = "bx-calendar" }
    );
    await db.SaveChangesAsync();
}

static async Task SeedInterestTagsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();

    if (await db.InterestTags.AnyAsync()) return;

    db.InterestTags.AddRange(
        new InterestTag { Name = "Music", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Movies", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Travel", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Foodie", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Fitness", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Reading", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Gaming", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Dancing", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Art", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Photography", Category = InterestCategory.Interest, IsCurated = true },
        new InterestTag { Name = "Romance", Category = InterestCategory.Fantasy, IsCurated = true },
        new InterestTag { Name = "Adventure", Category = InterestCategory.Fantasy, IsCurated = true },
        new InterestTag { Name = "Roleplay", Category = InterestCategory.Fantasy, IsCurated = true },
        new InterestTag { Name = "Threesome", Category = InterestCategory.Fantasy, IsCurated = true },
        new InterestTag { Name = "BDSM", Category = InterestCategory.Fantasy, IsCurated = true },
        new InterestTag { Name = "Polyamory", Category = InterestCategory.Fantasy, IsCurated = true },
        new InterestTag { Name = "Erotic Massage", Category = InterestCategory.Fantasy, IsCurated = true },
        new InterestTag { Name = "Open Relationship", Category = InterestCategory.Fantasy, IsCurated = true },
        new InterestTag { Name = "Hiking", Category = InterestCategory.Hobby, IsCurated = true },
        new InterestTag { Name = "Cooking", Category = InterestCategory.Hobby, IsCurated = true },
        new InterestTag { Name = "Yoga", Category = InterestCategory.Hobby, IsCurated = true },
        new InterestTag { Name = "Surfing", Category = InterestCategory.Hobby, IsCurated = true },
        new InterestTag { Name = "Camping", Category = InterestCategory.Hobby, IsCurated = true },
        new InterestTag { Name = "DIY", Category = InterestCategory.Hobby, IsCurated = true },
        new InterestTag { Name = "Gardening", Category = InterestCategory.Hobby, IsCurated = true },
        new InterestTag { Name = "Vegetarian", Category = InterestCategory.Lifestyle, IsCurated = true },
        new InterestTag { Name = "Vegan", Category = InterestCategory.Lifestyle, IsCurated = true },
        new InterestTag { Name = "420 Friendly", Category = InterestCategory.Lifestyle, IsCurated = true },
        new InterestTag { Name = "Social Drinker", Category = InterestCategory.Lifestyle, IsCurated = true },
        new InterestTag { Name = "Non-Smoker", Category = InterestCategory.Lifestyle, IsCurated = true },
        new InterestTag { Name = "Dog Lover", Category = InterestCategory.Lifestyle, IsCurated = true },
        new InterestTag { Name = "Cat Lover", Category = InterestCategory.Lifestyle, IsCurated = true },
        new InterestTag { Name = "Night Owl", Category = InterestCategory.Lifestyle, IsCurated = true },
        new InterestTag { Name = "Early Bird", Category = InterestCategory.Lifestyle, IsCurated = true },
        new InterestTag { Name = "Submissive", Category = InterestCategory.Kink, IsCurated = true },
        new InterestTag { Name = "Dominant", Category = InterestCategory.Kink, IsCurated = true },
        new InterestTag { Name = "Switch", Category = InterestCategory.Kink, IsCurated = true },
        new InterestTag { Name = "Voyeur", Category = InterestCategory.Kink, IsCurated = true },
        new InterestTag { Name = "Exhibitionist", Category = InterestCategory.Kink, IsCurated = true },
        new InterestTag { Name = "Pet Play", Category = InterestCategory.Kink, IsCurated = true },
        new InterestTag { Name = "Bondage", Category = InterestCategory.Kink, IsCurated = true }
    );
    await db.SaveChangesAsync();
}

static async Task SeedGamificationAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var gamification = scope.ServiceProvider.GetRequiredService<IGamificationService>();
    await gamification.SeedBadgesAsync();
}

static async Task SeedVipPlansAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    if (await db.SubscriptionPlans.AnyAsync()) return;

    var basic = new SubscriptionPlan
    {
        Name = "Basic",
        DescriptionShort = "1 Month VIP",
        Description = "Unlock messaging permissions and VIP badge for 1 month",
        SubscriptionDetails =
        [
            new() { Price = 9.99m, Description = "1 Month VIP Access", DurationInMonths = 1 }
        ]
    };

    var premium = new SubscriptionPlan
    {
        Name = "Premium",
        DescriptionShort = "3 Months VIP",
        Description = "Best value — 3 months of full VIP access with priority support",
        SubscriptionDetails =
        [
            new() { Price = 24.99m, Description = "3 Month VIP Access", DurationInMonths = 3 }
        ]
    };

    var platinum = new SubscriptionPlan
    {
        Name = "Platinum",
        DescriptionShort = "6 Months VIP",
        Description = "Ultimate plan — 6 months of VIP with all premium features",
        SubscriptionDetails =
        [
            new() { Price = 39.99m, Description = "6 Month VIP Access", DurationInMonths = 6 }
        ]
    };

    db.SubscriptionPlans.AddRange(basic, premium, platinum);
    await db.SaveChangesAsync();
}
