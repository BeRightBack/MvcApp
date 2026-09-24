using MvcApp.Core;

namespace MvcApp.Localization
{
    public class LanguageViewModel
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Culture { get; set; }

        public LanguageViewModel(Language language)
        {
            Id = language.Id;
            Name = language.Name;
            Culture = language.Culture;
        }

        public LanguageViewModel()
        {
        }
    }
}
