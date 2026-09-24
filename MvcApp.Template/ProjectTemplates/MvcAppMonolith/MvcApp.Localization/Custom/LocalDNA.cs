using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace MvcApp.Localization.Custom
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class LocalDNA : DisplayNameAttribute
    {
        private readonly string _resourceKey;
        private readonly Type _resourceType;

        public LocalDNA(string resourceKey, Type resourceType)
        {
            _resourceKey = resourceKey;
            _resourceType = resourceType;
        }

        public override string DisplayName
        {
            get
            {
                var localizer = GetLocalizedString();
                return localizer ?? _resourceKey;
            }
        }

        private string? GetLocalizedString()
        {
            var httpContextAccessor = ServiceLocator.Instance.GetService<IHttpContextAccessor>();
            var httpContext = httpContextAccessor?.HttpContext;

            if (httpContext == null)
            {
                return null;
            }

            // Get the IStringLocalizer<T> where T is the resource type
            var localizerType = typeof(IStringLocalizer<>).MakeGenericType(_resourceType);
            var localizer = httpContext.RequestServices.GetService(localizerType) as IStringLocalizer;

            if (localizer == null)
            {
                return null;
            }

            var localizedString = localizer[_resourceKey];

            return localizedString.Value;
        }
    }

    // ServiceLocator to access IServiceProvider
    public class ServiceLocator
    {
        private static IServiceProvider? _instance;
        public static IServiceProvider Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("ServiceLocator is not initialized");
                return _instance;
            }
            set => _instance = value;
        }
    }
}
