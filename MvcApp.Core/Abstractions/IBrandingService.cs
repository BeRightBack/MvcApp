namespace MvcApp.Core.Abstractions;

public interface IBrandingService
{
    Task<string> GetSiteNameAsync();
    Task<string?> GetLogoUrlAsync();
    Task<string?> GetTaglineAsync();
}
