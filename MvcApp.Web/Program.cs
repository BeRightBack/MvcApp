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
using MvcApp.Infrastructure.Seeding.Packs;
using MvcApp.Infrastructure.Seeding;
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
// ConnectionStrings__SerilogLogs environment variable) so that credentials never live in code.
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

var logsConnectionString = logConfig["ConnectionStrings:SerilogLogs"];
if (string.IsNullOrWhiteSpace(logsConnectionString))
{
    throw new InvalidOperationException("ConnectionStrings:SerilogLogs is not configured. Add it to appsettings.json or set the environment variable.");
}

// The MySQL sink connects eagerly when the logger is built and is then dropped for the
// lifetime of the process if its database is missing, so create the database first.
if (await SystemLogDatabaseInitializer.EnsureExistsAsync(logsConnectionString))
{
    Console.WriteLine("Created the Serilog log database named in ConnectionStrings:SerilogLogs.");
}

var isDevelopment =
    string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

// MySQL is the system of record for logs; the file sink only exists to make local
// tailing easy. Logging:FileSink forces it on or off, unset = on in Development only.
var writeFileSink = logConfig.GetValue<bool?>("Logging:FileSink") ?? isDevelopment;

var loggerConfig = new LoggerConfiguration()
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
    );

if (writeFileSink)
{
    loggerConfig = loggerConfig.WriteTo.File(path: "logs/log-.txt",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Information
    );
}

Log.Logger = loggerConfig.CreateLogger();

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

    // Seed in the background while the server starts. The bootstrap tier (languages,
    // settings, module flags, roles, the administrator, reference data) runs in EVERY
    // environment: without it a production deployment has no admin to log in with and
    // every module reads as disabled. The demo tier (test users, likes, messages, page
    // snippets) is opt-in via Seeding:IncludeDemoData, which defaults to on in
    // Development and off everywhere else.
    var includeDemoData = app.Configuration.GetValue("Seeding:IncludeDemoData", app.Environment.IsDevelopment());

    _ = Task.Run(async () =>
    {
        try
        {
            Log.Information("Starting background seeding... (demo data: {DemoData})",
                includeDemoData ? "on" : "off");

            await SeedLanguageAsync(app);
            await SeedSettingsAsync(app);
            await SeedChatRoomsAsync(app);
            await ApplySeedPacksAsync(app);
            await SeedEventCategoriesAsync(app);
            await SeedGamificationAsync(app);
            RegisterBlazorPageWidgetAssemblies(app);
            app.SeedData(includeDemoData);

            if (includeDemoData)
            {
                await SeedPageSnippetsAsync(app);
            }

            Log.Information("Background seeding completed.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error during background seeding");
        }
    });

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


// Migrate only when there are pending migrations. On MariaDB the Oracle provider
// takes GET_LOCK(-1) unconditionally inside MigrateAsync (MariaDB returns NULL, which
// crashes as InvalidCastException), so when the schema is already applied (remote
// deploy path uses scripted migrations) we must skip the call entirely.
static async Task EnsureMigratedAsync(DbContext db)
{
    if ((await db.Database.GetPendingMigrationsAsync()).Any())
    {
        await db.Database.MigrateAsync();
    }
}

static async Task SeedLanguageAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LocalizationDbContext>();

    // Apply migrations to create/update the database
    await EnsureMigratedAsync(context);

    var service = scope.ServiceProvider.GetRequiredService<SeedLanguage>();
    await service.EnsureSeedLanguageAsync();

    // Languages must exist first: the translation seed only writes rows for cultures that
    // already have a Language row.
    var translations = scope.ServiceProvider.GetRequiredService<TranslationSeedSeeder>();
    await translations.SeedAsync();
}

static async Task SeedSettingsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await EnsureMigratedAsync(db);
    await SettingsSeeder.SeedAsync(db);

    // The seeder may have inserted or upserted rows; drop any snapshot cached earlier.
    SettingsSeeder.InvalidateCache(scope.ServiceProvider.GetRequiredService<SettingsCache>());
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

static async Task ApplySeedPacksAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await EnsureMigratedAsync(db);

    var packs = scope.ServiceProvider.GetRequiredService<SeedPackService>();
    var applied = scope.ServiceProvider.GetServices<ISeedPack>();

    foreach (var pack in applied)
    {
        try
        {
            await pack.SeedAsync(packs, db, "startup", CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Seed pack {Pack} failed; continuing with the remaining packs", pack.Name);
        }
    }
}

static async Task SeedPageSnippetsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await EnsureMigratedAsync(db);

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
    await EnsureMigratedAsync(db);

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

static async Task SeedGamificationAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var gamification = scope.ServiceProvider.GetRequiredService<IGamificationService>();
    await gamification.SeedBadgesAsync();
}


