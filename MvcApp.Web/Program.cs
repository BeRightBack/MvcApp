using System.Data;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.DataProtection;
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

// Do not advertise the server implementation.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);



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

    // Operational endpoints. Liveness must not depend on downstream services; readiness is what a
    // load balancer or deploy gate should use, and it reflects the SCHEMA and the SEEDING outcome,
    // not merely that a TCP connection to MySQL succeeded.
    builder.Services.AddSingleton<StartupState>();
    builder.Services.AddSingleton<MvcApp.Web.Storage.VerificationDocumentStore>();
    // User-authored rich text is allow-list sanitised before it is stored or rendered (audit 2.1).
    builder.Services.AddSingleton<MvcApp.Common.Html.IHtmlContentSanitizer, MvcApp.Common.Html.HtmlContentSanitizer>();
    // Seals the quoted plan/detail/amount into the payment return URL so the grant cannot be pointed
    // at a different plan than the one that was paid for (audit 3.12).
    builder.Services.AddSingleton<MvcApp.Common.Payments.PaymentIntentProtector>();
    // The payment gate: rails are resolved through IPaymentGateway so a template owner can enable
    // whichever ones they want (offline/manual first; card and entity rails register alongside).
    MvcApp.Common.Payments.PaymentGatewayServiceCollectionExtensions.AddPaymentGateways(builder.Services, builder.Configuration);
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database")
        .AddCheck<PendingMigrationsHealthCheck>("migrations")
        .AddCheck<StartupReadinessHealthCheck>("startup");

    // Data Protection keys must OUTLIVE a deployment and be shared between instances, or auth
    // cookies, session state and antiforgery tokens are invalidated on every restart and every
    // additional instance. The default per-user ring is machine-local and not deploy-stable, and
    // without a stable application name the ring is shared with any other app on the box.
    var dataProtectionPath = builder.Configuration.GetValue<string>("DataProtection:KeyPath");
    if (string.IsNullOrWhiteSpace(dataProtectionPath))
    {
        dataProtectionPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MvcApp",
            "keys");
    }

    dataProtectionPath = Path.GetFullPath(dataProtectionPath);
    Directory.CreateDirectory(dataProtectionPath);

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
        .SetApplicationName("MvcApp");

    Log.Information("Data Protection key ring: {KeyPath}", dataProtectionPath);

    // Rate limiting. A NAMED policy is optional metadata that any endpoint can simply lack —
    // which is exactly how the "auth" policy ended up throttling nothing. A global limiter
    // cannot be forgotten, so it is the baseline; static files are served before this middleware,
    // so only dynamic requests are counted.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Limits are read from configuration when a partition is first created (not at startup),
        // so they can be overridden per environment — and per test — without touching this code.
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ =>
                {
                    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

                    return new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("RateLimiting:PermitLimit", 120),
                        Window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:WindowSeconds", 60)),
                        QueueLimit = 0
                    };
                }));

        // A tighter policy, still available for endpoints that opt in explicitly.
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

        // Phase 2 default-deny: every action now requires an authenticated user unless it (or its
        // controller) is explicitly marked [AllowAnonymous]. Deliberately a global AuthorizeFilter
        // rather than a FallbackPolicy: the fallback also applies when NO endpoint matched, which
        // turned every unknown URL from a 404 into a redirect to the login page (observed on
        // /does-not-exist), so it was replaced with this.
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(
            new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build()));
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

        // Same default-deny for pages (Phase 2); the public Identity pages carry [AllowAnonymous].
        options.Conventions.AuthorizeFolder("/");
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

    // Lockout policy (audit 2.6). SignInManager owns lockout; its attempt count and window come from
    // the admin-editable SystemSettings so the app does not carry two disagreeing policies. Read
    // ONCE here, after the container is built: doing it from an IConfigureOptions<IdentityOptions>
    // deadlocked startup, because options construction is synchronous and would have to make a
    // blocking database call. Applied to the resolved options singleton before the first request;
    // a change made in the admin UI takes effect on the next restart.
    {
        using var startupScope = app.Services.CreateScope();
        var startupLogger = startupScope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("MvcApp.Startup.Lockout");

        var lockoutOptions = app.Services
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.IdentityOptions>>()
            .Value;

        try
        {
            var lockoutSettings = startupScope.ServiceProvider
                .GetRequiredService<MvcApp.Core.Abstractions.ISettingsService>();

            lockoutOptions.Lockout.MaxFailedAccessAttempts =
                await lockoutSettings.GetAsync<int>("MaxLoginAttempts")
                ?? lockoutOptions.Lockout.MaxFailedAccessAttempts;

            var lockoutMinutes = await lockoutSettings.GetAsync<int>("LockoutDurationMinutes")
                                 ?? (int)lockoutOptions.Lockout.DefaultLockoutTimeSpan.TotalMinutes;

            lockoutOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutMinutes);
            lockoutOptions.Lockout.AllowedForNewUsers = true;

            startupLogger.LogInformation(
                "Lockout policy from settings: {Attempts} attempts, {Minutes} minute window.",
                lockoutOptions.Lockout.MaxFailedAccessAttempts, lockoutMinutes);
        }
        catch (Exception ex)
        {
            // A settings read must never prevent the app from starting; keep Identity's defaults.
            startupLogger.LogWarning(ex,
                "Could not read lockout settings; keeping Identity defaults: {Attempts} attempts / {Minutes} minutes.",
                lockoutOptions.Lockout.MaxFailedAccessAttempts,
                (int)lockoutOptions.Lockout.DefaultLockoutTimeSpan.TotalMinutes);
        }
    }

    // Structured request logging via Serilog (duration, status, client IP).
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.

    // Forwarded headers must run BEFORE anything that inspects the scheme (HSTS, HTTPS redirect).
    // Behind the TLS-terminating reverse proxy the request otherwise still looks like http, and
    // HSTS silently declines to emit.
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
        RequireHeaderSymmetry = false,
        ForwardLimit = null,
        KnownProxies = { IPAddress.Parse("127.0.0.1") } // or your Apache proxy IP
    });

    // Baseline security headers on every response.
    app.UseMiddleware<SecurityHeadersMiddleware>();

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
    // Liveness: the process is up. No dependency checks — a database outage must trigger readiness
    // failures, not restarts.
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    }).AllowAnonymous();

    // Readiness: safe to receive traffic — schema current AND seeding finished. "/health" remains
    // the readiness alias so existing probes keep working unchanged.
    // AllowAnonymous on all three: under the Phase 2 fallback policy an endpoint with no
    // authorization metadata would otherwise require a login, which would take the probes down.
    app.MapHealthChecks("/health/ready").AllowAnonymous();
    app.MapHealthChecks("/health").AllowAnonymous();

    // Seed in the background while the server starts. The bootstrap tier (languages,
    // settings, module flags, roles, the administrator, reference data) runs in EVERY
    // environment: without it a production deployment has no admin to log in with and
    // every module reads as disabled. The demo tier (test users, likes, messages, page
    // snippets) is opt-in via Seeding:IncludeDemoData, which defaults to on in
    // Development and off everywhere else.
    var includeDemoData = app.Configuration.GetValue("Seeding:IncludeDemoData", app.Environment.IsDevelopment());

    // Seeding runs off the startup path, so its outcome has to be recorded for readiness to see.
    // Previously a failure here was a Fatal log line and nothing else, while the app served 500s.
    var startupState = app.Services.GetRequiredService<StartupState>();

    _ = Task.Run(async () =>
    {
        try
        {
            Log.Information("Starting background seeding... (demo data: {DemoData})",
                includeDemoData ? "on" : "off");

            await SeedLanguageAsync(app);
            await SeedSettingsAsync(app);

            // Chat rooms, event categories and badges are owned by the Chat / Events /
            // Gamification packs, so they apply only when the active template needs them and
            // they are recorded in SeedManifest. The old inline seeders created those rows
            // outside the manifest, which made them impossible to remove.
            await ApplySeedPacksAsync(app);

            RegisterBlazorPageWidgetAssemblies(app);
            app.SeedData(includeDemoData);

            if (includeDemoData)
            {
                await SeedPageSnippetsAsync(app);
            }

            startupState.MarkCompleted();
            Log.Information("Background seeding completed.");
        }
        catch (Exception ex)
        {
            startupState.MarkFailed(ex.Message);
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


// Migrate only when there are pending migrations AND the server can actually be migrated
// in-process. On MariaDB the Oracle provider takes GET_LOCK(-1) unconditionally inside
// MigrateAsync (MariaDB returns NULL, which crashes as InvalidCastException) BEFORE it
// looks at the pending list, so a pending migration on MariaDB cannot be applied here at
// all. MariaDB deployments apply migrations with `dotnet ef migrations script` piped to
// mysql instead.
static async Task EnsureMigratedAsync(DbContext db)
{
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

    if (pending.Count == 0)
    {
        return;
    }

    if (await IsMariaDbAsync(db))
    {
        throw new InvalidOperationException(
            $"{pending.Count} pending migration(s) cannot be applied at startup because this "
            + "server is MariaDB: MySql.EntityFrameworkCore's history repository runs "
            + "SELECT GET_LOCK('__EFMigrationsLock', -1) and MariaDB returns NULL for a negative "
            + "timeout, which the provider casts to Int64 and fails on. Generate and apply a "
            + "script instead: dotnet ef migrations script "
            + $"{pending[0]} {pending[^1]} --project MvcApp.Infrastructure "
            + "--startup-project MvcApp.Web -c UserDbContext --output migrate.sql, then pipe it "
            + "to the mysql client. Pending migrations: "
            + string.Join(", ", pending));
    }

    await db.Database.MigrateAsync();
}

static async Task<bool> IsMariaDbAsync(DbContext db)
{
    var connection = db.Database.GetDbConnection();

    if (connection.State != ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    return connection.ServerVersion.Contains("MariaDB", StringComparison.OrdinalIgnoreCase);
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

static async Task ApplySeedPacksAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await EnsureMigratedAsync(db);

    var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
    var template = await templateService.GetActiveTemplateAsync();

    // Only the packs this template's profile asks for. Before this, every registered pack ran
    // on every boot, so a business portal shipped dating content it never wanted.
    var planner = scope.ServiceProvider.GetRequiredService<SeedPackPlanner>();
    var plan = await planner.ApplyForTemplateAsync(template, "startup", CancellationToken.None);

    foreach (var item in plan.Where(i => i.Applied))
    {
        Log.Information("Seed pack {Pack}: {Rows} row(s) on disk (required by {Template}: {Needed})",
            item.Pack, item.Rows, template, item.Needed ? "yes" : "no");
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

// Event categories are seeded by EventsPack and badges by GamificationPack; see
// ApplySeedPacksAsync. Neither has an inline seeder here on purpose: rows created outside
// SeedManifest cannot be removed by a pack.

// Exposed so the host-level test harness can reference this entry-point assembly
// (WebApplicationFactory<Program>). Required because Program.cs uses top-level statements.
public partial class Program { }


