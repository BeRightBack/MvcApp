using MvcApp.Core;

namespace MvcApp.Localization;

public class LocalizationService : ILocalizationService
{
    private readonly LocalizationDbContext _context;

    public LocalizationService(LocalizationDbContext context)
    {
        _context = context;
    }

    public StringResource GetStringResource(string resourceKey, int languageId)
    {

        var key = resourceKey.Trim();
        return _context.StringResources.FirstOrDefault(x =>
            x.Name == key && x.LanguageId == languageId)!;

        //return _context.StringResources.FirstOrDefault(x =>
        //        x.Name!.Trim().ToLower() == resourceKey.Trim().ToLower()
        //        && x.LanguageId == languageId)!;
    }

    public void AddOrUpdateStringResource(StringResource resource)
    {
        lock (_context)
        {
            var existing = _context.StringResources
                .FirstOrDefault(x => x.Name == resource.Name && x.LanguageId == resource.LanguageId);

            if (existing != null)
            {
                existing.Value = resource.Value;
                _context.Update(existing);
            }
            else
            {
                _context.Add(resource);
            }

            _context.SaveChanges();
        }
    }
}
