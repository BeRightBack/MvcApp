using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MvcApp.Common.Filters
{
    public class MobileLayoutPageFilter : IPageFilter
    {
        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();
            if (userAgent.Contains("Mobi"))
            {
                if (context.HandlerInstance is PageModel pageModel)
                {
                    pageModel.PageContext.ViewData["Layout"] = "_MobileLayout";
                }
            }
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }
    }
}

