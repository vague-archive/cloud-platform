namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadCurrentOrganization : Attribute, IAsyncActionFilter, IAsyncPageFilter
{
  //===============================================================================================
  // MVC ACTION FILTER
  //===============================================================================================

  public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
  {
    if (await LoadCurrentOrganization.From(ctx.HttpContext))
    {
      await next();
    }
    else
    {
      ctx.Result = new NotFoundResult();
    }
  }

  //===============================================================================================
  // RAZOR PAGE FILTER
  //===============================================================================================

  public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext ctx)
  {
    return Task.CompletedTask;
  }

  public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext ctx, PageHandlerExecutionDelegate next)
  {
    if (await LoadCurrentOrganization.From(ctx.HttpContext))
    {
      var current = GetCurrent(ctx.HttpContext);
      current.Page.HeaderPartial = "Layout/Default/Header/Organization";
      await next();
    }
    else
    {
      ctx.Result = new NotFoundResult();
    }
  }

  //===============================================================================================
  // SHARED IMPLEMENTATION
  //===============================================================================================

  public static async Task<bool> From(HttpContext ctx)
  {
    var current = GetCurrent(ctx);
    var authz = GetAuthz(ctx);
    var app = GetApplication(ctx);

    if (!ctx.Request.RouteValues.TryGetValue("org", out var slugValue) || (slugValue is not string slug))
      return false;

    var org = app.Account.GetOrganization(slug);
    if (org is null)
      return false;

    if (current.MustAuthenticate)
    {
      var auth = await authz.AuthorizeAsync(current.Principal, org, Policy.OrgMember);
      if (!auth.Succeeded)
        return false;
    }

    current.Organization = org;
    return true;
  }

  private static Current GetCurrent(HttpContext ctx)
  {
    return ctx.RequestServices.GetRequiredService<Current>();
  }

  private static Application GetApplication(HttpContext ctx)
  {
    return ctx.RequestServices.GetRequiredService<Application>();
  }

  private static IAuthorizationService GetAuthz(HttpContext ctx)
  {
    return ctx.RequestServices.GetRequiredService<IAuthorizationService>();
  }

  //-----------------------------------------------------------------------------------------------
}