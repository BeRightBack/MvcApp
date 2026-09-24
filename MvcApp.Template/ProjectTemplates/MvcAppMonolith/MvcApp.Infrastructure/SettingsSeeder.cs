using Microsoft.EntityFrameworkCore;
using MvcApp.Core;

namespace MvcApp.Infrastructure;

public static class SettingsSeeder
{
    private static readonly List<SystemSetting> _moduleDefaults =
    [
        new() { Key = "Module.Forum.Enabled", Value = "true", Description = "Enable/disable the Forum feature",          Group = "Modules" },
        new() { Key = "Module.Blog.Enabled",  Value = "true", Description = "Enable/disable the Blog feature",           Group = "Modules" },
        new() { Key = "Module.Chat.Enabled",  Value = "true", Description = "Enable/disable the Chat feature",           Group = "Modules" },
        new() { Key = "Module.Messages.Enabled", Value = "true", Description = "Enable/disable the Private Messages feature", Group = "Modules" },
        new() { Key = "Module.Store.Enabled",    Value = "true", Description = "Enable/disable the Store feature",              Group = "Modules" },
        new() { Key = "Module.Iptv.Enabled",     Value = "true", Description = "Enable/disable the IPTV feature",               Group = "Modules" },
        new() { Key = "Module.Pages.Enabled",    Value = "true", Description = "Enable/disable the Pages (CMS) feature",         Group = "Modules" },
        new() { Key = "Module.Utility.Enabled",  Value = "true", Description = "Enable/disable the Utility (ToDos) feature",       Group = "Modules" },
        new() { Key = "Module.Ads.Enabled",      Value = "true", Description = "Enable/disable the Ads feature",                   Group = "Modules" },
        new() { Key = "Module.Video.Enabled",    Value = "true", Description = "Enable/disable the Video Chat feature",            Group = "Modules" },
        new() { Key = "Module.Events.Enabled",   Value = "true", Description = "Enable/disable the Events feature",               Group = "Modules" },
        new() { Key = "Module.Gamification.Enabled", Value = "true", Description = "Enable/disable Gamification (points, badges, leaderboards)", Group = "Modules" },
    ];

    private static readonly List<SystemSetting> _brandingDefaults =
    [
        new() { Key = "Branding.SiteName", Value = "MvcApp.Web", Description = "Site name shown in navbar, titles and footer",  Group = "Branding" },
        new() { Key = "Branding.LogoUrl",  Value = "",           Description = "Optional logo URL shown instead of the site name", Group = "Branding" },
        new() { Key = "Branding.Tagline",  Value = "",           Description = "Optional short tagline for the site",             Group = "Branding" },
    ];

    public static async Task SeedAsync(UserDbContext db)
    {
        if (!await db.SystemSettings.AnyAsync())
        {
            var defaults = new List<SystemSetting>
            {
                new() { Key = "SiteUnderMaintenance",   Value = "false", Description = "Show maintenance page to visitors",              Group = "General" },
                new() { Key = "DefaultLanguage",        Value = "en",    Description = "Default UI culture for new users",              Group = "General" },
                new() { Key = "MaxLoginAttempts",       Value = "5",     Description = "Failed attempts before lockout",                Group = "Security" },
                new() { Key = "LockoutDurationMinutes", Value = "15",    Description = "How long a user is locked out (minutes)",       Group = "Security" },
                new() { Key = "SessionTimeoutMinutes",  Value = "20",    Description = "Inactive session timeout",                      Group = "Security" },
                new() { Key = "EnableRegistration",     Value = "true",  Description = "Allow new user self-registration",              Group = "Registration" },
                new() { Key = "MaxUserRegistration",    Value = "1000",  Description = "Maximum registered accounts allowed",           Group = "Registration" },
                new() { Key = "PendingDeletionDays",    Value = "30",    Description = "Days before soft-deleted accounts are purged",  Group = "Registration" },
                new() { Key = "SmtpEnabled",            Value = "true",  Description = "Enable email sending",                          Group = "Features" },
                new() { Key = "VisitorTrackingEnabled", Value = "true",  Description = "Log visitor IP/location data",                  Group = "Features" },
                new() { Key = "SiteTemplate",           Value = "Default", Description = "Active UI template (Default, Dating, etc.)",  Group = "Appearance" },
            };
            defaults.AddRange(_moduleDefaults);
            defaults.AddRange(_brandingDefaults);
            db.SystemSettings.AddRange(defaults);
        }
        else
        {
            // Always ensure module and branding settings exist (upsert)
            foreach (var ms in _moduleDefaults.Concat(_brandingDefaults))
            {
                var existing = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == ms.Key);
                if (existing == null)
                    db.SystemSettings.Add(ms);
            }
        }

        await db.SaveChangesAsync();
    }
}
