using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using MvcApp.Core;

namespace MvcApp.Infrastructure.Seeding;

public sealed class SeedPackService(UserDbContext db, ILogger<SeedPackService> logger)
{
    public async Task<SeedPackResult> ApplyAsync<TEntity>(
        string packName,
        Action<UserDbContext> add,
        Func<UserDbContext, IQueryable<TEntity>>? existing = null,
        string? appliedBy = null,
        CancellationToken ct = default)
        where TEntity : class
    {
        var typeName = SeedKey.TypeName<TEntity>();
        var alreadyRecorded = await db.SeedManifest
            .CountAsync(m => m.PackName == packName && m.EntityType == typeName, ct);

        if (alreadyRecorded > 0)
            return new SeedPackResult(packName, 0, alreadyRecorded);

        var keyProperties = SeedKey.KeyProperties<TEntity>(db);

        add(db);

        var added = db.ChangeTracker.Entries<TEntity>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        if (added.Count == 0)
        {
            // Every pack guards its own add() with "if (d.X.Any()) return;", so a database that
            // already holds the rows (a previous install, a restored backup, rows seeded before
            // the manifest existed) produces zero additions. Without this branch the pack reported
            // nothing, the manifest stayed empty, the admin page showed "Not applied", and
            // clicking Apply answered "already applied" — status and action contradicting each
            // other, which is exactly what was reported.
            //
            // When the caller supplied an `existing` selector, adopt what is already there so the
            // manifest becomes the truthful record. It is optional because a pack whose rows are
            // not individually addressable (no stable key) cannot adopt them; those keep the old
            // behaviour of recording nothing.
            if (existing is not null)
            {
                var adopted = await AdoptAsync(packName, existing, appliedBy, ct);
                if (adopted > 0)
                    return new SeedPackResult(packName, 0, adopted);
            }

            return new SeedPackResult(packName, 0, 0);
        }

        await db.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var entity in added)
        {
            db.SeedManifest.Add(new SeedManifest
            {
                PackName = packName,
                EntityType = typeName,
                EntityKey = SeedKey.Write(entity, keyProperties),
                AppliedAt = now,
                AppliedBy = appliedBy,
            });
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seed pack {Pack} recorded {Count} {Entity} row(s).",
            packName, added.Count, typeof(TEntity).Name);

        return new SeedPackResult(packName, added.Count, 0);
    }

    public async Task<int> AdoptAsync<TEntity>(
        string packName,
        Func<UserDbContext, IQueryable<TEntity>> select,
        string? adoptedBy = null,
        CancellationToken ct = default)
        where TEntity : class
    {
        var typeName = SeedKey.TypeName<TEntity>();

        var alreadyRecorded = await db.SeedManifest
            .CountAsync(m => m.PackName == packName && m.EntityType == typeName, ct);

        if (alreadyRecorded > 0)
            return 0;

        var keyProperties = SeedKey.KeyProperties<TEntity>(db);
        var rows = await select(db).ToListAsync(ct);

        if (rows.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        foreach (var entity in rows)
        {
            db.SeedManifest.Add(new SeedManifest
            {
                PackName = packName,
                EntityType = typeName,
                EntityKey = SeedKey.Write(entity, keyProperties),
                AppliedAt = now,
                AppliedBy = adoptedBy,
            });
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seed pack {Pack} adopted {Count} existing {Entity} row(s).",
            packName, rows.Count, typeof(TEntity).Name);

        return rows.Count;
    }

    public async Task<SeedPackRemoval> RemoveAsync<TEntity>(
        string packName,
        CancellationToken ct = default)
        where TEntity : class
    {
        var typeName = SeedKey.TypeName<TEntity>();
        var manifest = await db.SeedManifest
            .Where(m => m.PackName == packName && m.EntityType == typeName)
            .ToListAsync(ct);

        return await RemoveRowsAsync(packName, typeName, manifest, ct);
    }

    public async Task<SeedPackRemoval> RemoveAsync(string packName, CancellationToken ct = default)
    {
        var byType = await db.SeedManifest
            .Where(m => m.PackName == packName)
            .GroupBy(m => m.EntityType)
            .Select(g => new { EntityType = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totals = new Dictionary<string, int>(StringComparer.Ordinal);
        var total = 0;

        foreach (var group in byType)
        {
            var clrType = ResolveClrType(group.EntityType);

            var method = typeof(SeedPackService)
                .GetMethod(nameof(RemoveTypedAsync), System.Reflection.BindingFlags.NonPublic |
                                                     System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(clrType);

            var task = (Task<SeedPackRemoval>)method.Invoke(this, new object?[] { packName, ct })!;
            var removal = await task;

            total += removal.RowsDeleted;
            foreach (var pair in removal.ByEntityType)
                totals[pair.Key] = pair.Value;
        }

        return new SeedPackRemoval(packName, total, totals);
    }

    public async Task<bool> IsAppliedAsync(string packName, CancellationToken ct = default) =>
        await db.SeedManifest.AnyAsync(m => m.PackName == packName, ct);

    /// <summary>
    /// Whether SeedManifest exists yet. It is created by the AddSeedManifest migration, so on
    /// any database that has not been migrated past that point every method on this class
    /// throws ER_NO_SUCH_TABLE (1146) — including the admin page that is supposed to explain
    /// the situation. Callers check this first and say so in words.
    ///
    /// This is a real state, not a hypothetical one: MariaDB deployments cannot migrate
    /// in-process, because the Oracle provider takes GET_LOCK with a negative timeout that
    /// MariaDB answers with NULL. They apply migrations from a generated script, so until
    /// somebody runs it the site is fully migrated except for this table.
    /// </summary>
    public async Task<bool> IsManifestAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.SeedManifest.AsNoTracking().AnyAsync(ct);
        }
        catch (MySqlException ex) when (ex.Number == 1146)
        {
            logger.LogWarning("SeedManifest is not present; seed packs are unavailable until the pending migrations are applied.");
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetAppliedPacksAsync(CancellationToken ct = default) =>
        await db.SeedManifest
            .Select(m => m.PackName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct);

    /// <remarks>
    /// The grouping is done in SQL and the Ordinal sort is applied in memory. Ordering inside
    /// the query with a <see cref="StringComparer"/> cannot be translated — the comparer is a
    /// .NET object with no SQL equivalent, so EF throws InvalidOperationException naming the
    /// whole expression rather than just the offending clause, which reads as if the GroupBy
    /// were at fault. It was not: the same group/count without a comparer translates fine
    /// (see SeedPackPlanner.RowsByPackAsync). The result set is one row per entity type for a
    /// single pack, so a client-side sort is not a material cost.
    /// </remarks>
    public async Task<IReadOnlyList<SeedPackEntityCount>> DescribeAsync(
        string packName,
        CancellationToken ct = default)
    {
        var groups = await db.SeedManifest
            .AsNoTracking()
            .Where(m => m.PackName == packName)
            .GroupBy(m => m.EntityType)
            .Select(g => new SeedPackEntityCount(g.Key, g.Count()))
            .ToListAsync(ct);

        return groups
            .OrderBy(g => g.EntityType, StringComparer.Ordinal)
            .ToList();
    }

    private Type ResolveClrType(string entityTypeName)
    {
        var fromModel = db.Model.FindEntityType(entityTypeName)?.ClrType;
        if (fromModel is not null)
            return fromModel;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var candidate = assembly.GetType(entityTypeName, throwOnError: false);
            if (candidate is not null)
                return candidate;
        }

        throw new InvalidOperationException(
            $"Seed manifest references unknown entity type '{entityTypeName}'. "
            + "The type was renamed or removed; the pack cannot be removed automatically.");
    }

    private async Task<SeedPackRemoval> RemoveTypedAsync<TEntity>(string packName, CancellationToken ct)
        where TEntity : class
    {
        var typeName = SeedKey.TypeName<TEntity>();
        var manifest = await db.SeedManifest
            .Where(m => m.PackName == packName && m.EntityType == typeName)
            .ToListAsync(ct);

        return await RemoveRowsAsync(packName, typeName, manifest, ct);
    }

    private async Task<SeedPackRemoval> RemoveRowsAsync(
        string packName,
        string typeName,
        List<SeedManifest> manifest,
        CancellationToken ct)
    {
        if (manifest.Count == 0)
            return new SeedPackRemoval(packName, 0, new Dictionary<string, int>(StringComparer.Ordinal));

        var clrType = ResolveClrType(typeName);
        var deleteMethod = typeof(SeedPackService)
            .GetMethod(nameof(DeleteEntitiesAsync), System.Reflection.BindingFlags.NonPublic |
                                                    System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(clrType);

        var task = (Task<int>)deleteMethod.Invoke(this, new object?[] { manifest, ct })!;
        var deleted = await task;

        db.SeedManifest.RemoveRange(manifest);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seed pack {Pack} removed {Count} {Entity} row(s).",
            packName, deleted, clrType.Name);

        return new SeedPackRemoval(
            packName, deleted, new Dictionary<string, int>(StringComparer.Ordinal) { [typeName] = deleted });
    }

    private async Task<int> DeleteEntitiesAsync<TEntity>(List<SeedManifest> manifest, CancellationToken ct)
        where TEntity : class
    {
        var keyProperties = SeedKey.KeyProperties<TEntity>(db);
        var keys = manifest.Select(m => SeedKey.Split(m.EntityKey)).ToList();
        var set = db.Set<TEntity>();
        var deleted = 0;

        foreach (var key in keys)
        {
            var tracked = await set.FirstOrDefaultAsync(SeedKey.Predicate<TEntity>(keyProperties, key), ct);
            if (tracked is null)
                continue;

            set.Remove(tracked);
            deleted++;
        }

        await db.SaveChangesAsync(ct);
        return deleted;
    }
}
