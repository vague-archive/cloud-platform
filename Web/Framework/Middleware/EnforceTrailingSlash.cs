namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class EnforceTrailingSlash : ActionFilterAttribute
{
  public override void OnActionExecuting(ActionExecutingContext ctx)
  {
    var path = ctx.HttpContext.Request.Path.Value;
    if (path is not null && !path.EndsWith("/"))
    {
      ctx.Result = new RedirectResult(path + "/");
    }
  }
}