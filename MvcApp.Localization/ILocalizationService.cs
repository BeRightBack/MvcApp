using MvcApp.Core;

namespace MvcApp.Localization;

public interface ILocalizationService
{
    StringResource GetStringResource(string resourceKey, int languageId);
    void AddOrUpdateStringResource(StringResource resource);
}
