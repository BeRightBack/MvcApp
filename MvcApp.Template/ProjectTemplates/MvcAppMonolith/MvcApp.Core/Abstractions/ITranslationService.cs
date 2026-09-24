namespace MvcApp.Core.Abstractions;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string target);
}
