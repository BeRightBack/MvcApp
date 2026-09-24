using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Module.Pages.Services;

public static class SnippetSeeder
{
    public static async Task SeedAsync(UserDbContext db)
    {
        if (await db.ContentPageSnippets.AnyAsync())
            return;

        foreach (var snippet in SnippetCatalog.Defaults)
            db.ContentPageSnippets.Add(snippet);
        await db.SaveChangesAsync();
    }
}
