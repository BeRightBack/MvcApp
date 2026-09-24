namespace MvcApp.Core.Abstractions;

public interface INavService
{
    Task<List<NavItem>> GetNavItemsAsync();
    Task<List<NavItem>> GetFooterItemsAsync();
    Task<List<NavItem>> GetSocialDropdownAsync();
    Task<List<NavItem>> GetProfileDropdownAsync();
    Task<List<NavItem>> GetTemplateNavbarAsync(string template);
    Task<List<NavItem>> GetTemplateFooterAsync(string template);
    Task<List<NavItem>> GetTemplateSocialDropdownAsync(string template);
    Task<List<NavItem>> GetTemplateProfileDropdownAsync(string template);
}
