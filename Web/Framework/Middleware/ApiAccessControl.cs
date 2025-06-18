namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class ApiAccessControl : ActionFilterAttribute
{
  public override void OnActionExecuting(ActionExecutingContext ctx)
  {
    ctx.HttpContext.Response.Headers.Append(Http.Header.AccessControlAllowMethods, "GET,HEAD,PUT,PATCH,POST,DELETE");
    ctx.HttpContext.Response.Headers.Append(Http.Header.AccessControlAllowOrigin, "*");
    ctx.HttpContext.Response.Headers.Append(Http.Header.AccessControlAllowHeaders, "Authorization");
  }
}