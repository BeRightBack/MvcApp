using System.Threading.Channels;
using DeepL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MvcApp.Core;

namespace MvcApp.Localization;

/// <summary>
/// Translates string keys that are not yet in the database.
///
/// The localizer used to do this inline: on a miss it called DeepL for every supported
/// culture with <c>.Result</c> and stored the results before returning. That blocked the
/// request thread for the whole translation (and up to ~31s of backoff per key on rate
/// limits), which is fine for a handful of keys and catastrophic once every view is
/// localized.
///
/// This keeps the self-translate behaviour - a missing key is still translated
/// automatically and stored, with no human step - but does it off the request path, so
/// the page renders immediately with the key (exactly what the localizer already
/// returned) and the translation lands in the database moments later.
/// </summary>
public sealed class BackgroundTranslationService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<BackgroundTranslationService> logger) : BackgroundService
{
    private const int MaxRetries = 5;

    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan BetweenTranslations = TimeSpan.FromMilliseconds(250);

    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true });

    private readonly HashSet<string> _queued = new(StringComparer.Ordinal);
    private readonly Lock _queuedLock = new();

    public string SourceLang => configuration["DeepLConfig:SourceLang"] ?? "EN";

    /// <summary>Queues a missing key. Safe to call on every request; duplicates collapse.</summary>
    public void Enqueue(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        lock (_queuedLock)
        {
            if (!_queued.Add(key)) return;
        }

        _queue.Writer.TryWrite(key);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background translation service started (source language {Source})", SourceLang);

        await foreach (var key in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await TranslateAsync(key, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Background translation failed for '{Key}'", key);
            }
            finally
            {
                lock (_queuedLock)
                {
                    _queued.Remove(key);
                }
            }
        }
    }

    private async Task TranslateAsync(string key, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        var languageService = scope.ServiceProvider.GetRequiredService<ILanguageService>();

        var translator = scope.ServiceProvider.GetRequiredService<Translator>();

        foreach (var language in languageService.GetLanguages().ToList())
        {
            var existing = localizationService.GetStringResource(key, language.Id);
            if (existing != null && !string.IsNullOrEmpty(existing.Value)) continue;

            var target = TargetFor(language.Id);
            if (string.IsNullOrWhiteSpace(target)) continue;

            var translated = await TranslateWithRetryAsync(translator, key, target, cancellationToken);
            if (string.IsNullOrWhiteSpace(translated) || translated == key) continue;

            localizationService.AddOrUpdateStringResource(new StringResource
            {
                LanguageId = language.Id,
                Name = key,
                Value = translated
            });

            logger.LogInformation("Translated '{Key}' for {Culture}", key, language.Culture);
        }
    }

    /// <summary>
    /// Language id -> DeepL target code. Mirrors the mapping the inline localizer used
    /// (1=en, 2=fr, 3=es, 4=it, 5=pt, 6=de) so behaviour is unchanged.
    /// </summary>
    private string? TargetFor(int languageId) => languageId switch
    {
        1 => configuration["DeepLConfig:TargetLangEn"],
        2 => configuration["DeepLConfig:TargetLangFr"],
        3 => configuration["DeepLConfig:TargetLangEs"],
        4 => configuration["DeepLConfig:TargetLangIt"],
        5 => configuration["DeepLConfig:TargetLangPt"],
        6 => configuration["DeepLConfig:TargetLangDe"],
        _ => null
    };

    private async Task<string> TranslateWithRetryAsync(Translator translator, string text, string target, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var result = await translator.TranslateTextAsync(text, SourceLang, target);
                return result.Text;
            }
            catch (TooManyRequestsException)
            {
                if (attempt > MaxRetries)
                {
                    logger.LogWarning("DeepL rate limit retries exhausted for '{Text}'; leaving it untranslated", text);
                    return string.Empty;
                }

                var delay = TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                logger.LogInformation("DeepL rate limited; retrying '{Text}' in {Delay}", text, delay);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "DeepL translation failed for '{Text}'; leaving it untranslated", text);
                return string.Empty;
            }
        }
    }
}
