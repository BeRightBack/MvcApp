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
                var localizer = LocalizationContext.Localizer;
                return localizer?[_name] ?? _name;
            }
        }
    }
}