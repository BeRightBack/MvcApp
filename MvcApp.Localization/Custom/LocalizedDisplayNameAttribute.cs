using System.ComponentModel;

namespace MvcApp.Localization.Custom
{
    public class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        private readonly string _name;

        public LocalizedDisplayNameAttribute(string name)
        {
            _name = name;
        }

        public override string DisplayName
        {
            get
            {
                // Resolves the localizer from the current request, and falls back to the
                // untranslated name when there is none (background work, or a test).
                return LocalizationContext.Translate(_name);
            }
        }
    }
}