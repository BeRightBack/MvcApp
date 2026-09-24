using System.Security.Claims;
using MvcApp.Core;

namespace MvcApp.Module.Pages.Services;

public interface IPageRenderer
{
    Task<string> RenderAsync(string razorBody, ContentPage model, ClaimsPrincipal? user = null);
}
