using Microsoft.Extensions.Localization;

namespace MvcApp.Localization.Custom
{
    public class DbStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DbStringLocalizerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            var localizerType = typeof(DbStringLocalizer<>).MakeGenericType(resourceSource);
            var localizer = (IStringLocalizer?)_serviceProvider.GetService(localizerType);
            if (localizer == null)
            {
                throw new InvalidOperationException($"Unable to resolve service for type '{localizerType}'.");
            }
            return localizer;
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return Create(typeof(object));
        }
    }
}
