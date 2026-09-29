using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MvcApp.Core;

namespace MvcApp.Localization;

/// <summary>
/// Loads the translation set that ships inside this assembly and writes it to
/// <c>StringResources</c>, so a freshly generated app renders translated straight away instead
/// of English-then-machine-translated.
/// <para>
/// Seeding only when the table is empty makes this idempotent and non-destructive: an app that
/// has already been running keeps whatever it has, including admin edits and any strings the
/// background translator produced.
/// </para>
/// <para>
/// English rows are deliberately not included. The localizer falls back to returning the key,
/// and the key IS the English text, so an <c>en</c> row would be redundant.
/// </para>
/// </summary>
public sealed class TranslationSeedSeeder(LocalizationDbContext context, ILogger<TranslationSeedSeeder> logger)
{
    private const string ResourceName = "MvcApp.Localization.Translations.json.gz";

    // Inserted in batches: one SaveChanges for several thousand rows holds a long transaction
    // against a remote MySQL, and a failure then loses the whole run.
    private const int BatchSize = 400;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.StringResources.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Translations already present; skipping the translation seed.");
            return;
        }

        var payload = Load();

        if (payload?.Keys is not { Count: > 0 })
        {
            logger.LogWarning("Translation seed resource is missing or empty; the app will machine-translate on demand.");
            return;
        }

        // Only seed cultures this app actually has a Language row for. The payload carries a
        // fixed set, and a generated app may be configured with fewer supported cultures.
        var cultureByName = await context.Languages
            .Where(l => l.Culture != null)
            .ToDictionaryAsync(l => l.Culture!.ToLowerInvariant(), l => l.Id, cancellationToken);

        if (cultureByName.Count == 0)
        {
            logger.LogWarning("No languages are seeded yet; skipping the translation seed.");
            return;
        }

        var added = 0;
        var skippedCultures = new List<string>();

        foreach (var key in payload.Keys)
        {
            if (string.IsNullOrWhiteSpace(key.Name)) continue;

            var batch = new List<StringResource>();

            foreach (var translation in key.Translations)
            {
                if (translation.Value is null) continue;

                var culture = translation.Key.ToLowerInvariant();
                if (!cultureByName.TryGetValue(culture, out var languageId))
                {
                    if (!skippedCultures.Contains(culture)) skippedCultures.Add(culture);
                    continue;
                }

                batch.Add(new StringResource
                {
                    LanguageId = languageId,
                    Name = key.Name.Trim(),
                    Value = translation.Value
                });
            }

            if (batch.Count == 0) continue;

            context.StringResources.AddRange(batch);
            added += batch.Count;

            if (added % BatchSize < batch.Count)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Translation seed complete: {Keys} keys, {Rows} rows, {Cultures}.",
            payload.Keys.Count, added,
            string.Join(", ", cultureByName.Keys));

        if (skippedCultures.Count > 0)
        {
            logger.LogInformation(
                "Translation seed skipped cultures with no Language row: {Cultures}.",
                string.Join(", ", skippedCultures));
        }
    }

    private static TranslationSeedPayload? Load()
    {
        var assembly = typeof(TranslationSeedSeeder).Assembly;

        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null) return null;

        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        return JsonSerializer.Deserialize<TranslationSeedPayload>(reader.ReadToEnd(), JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class TranslationSeedPayload
    {
        [JsonPropertyName("cultures")] public List<string> Cultures { get; set; } = new();
        [JsonPropertyName("keyCount")] public int KeyCount { get; set; }
        [JsonPropertyName("keys")] public List<TranslationSeedKey> Keys { get; set; } = new();
    }

    private sealed class TranslationSeedKey
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("translations")] public Dictionary<string, string?> Translations { get; set; } = new();
    }
}
