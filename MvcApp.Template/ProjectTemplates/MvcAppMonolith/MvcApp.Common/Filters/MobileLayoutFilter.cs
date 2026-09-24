using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MvcApp.Common.Filters
{
    public class MobileLayoutFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();
            if (userAgent.Contains("Mobi"))
            {
                // Pass the mobile layout name in ViewBag
                if (context.Controller is Controller controller)
                {
                    controller.ViewBag.Layout = "_MobileLayout";
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
