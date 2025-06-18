namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LoadCurrentGame : Attribute, IAsyncActionFilter, IAsyncPageFilter
{
  //===============================================================================================
  // MVC ACTION FILTER
  //===============================================================================================

  public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
  {
    var isPage = ctx.Controller is PageController;
    var isApi = ctx.Controller is ApiController;

    if (await LoadCurrentGame.From(ctx.HttpContext))
    {
      if (isPage && RedirectToGameSettingsPage(ctx.HttpContext, out var redirect))
      {
        ctx.Result = redirect;
        return;
      }
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
    if (await LoadCurrentGame.From(ctx.HttpContext))
    {
      if (RedirectToGameSettingsPage(ctx.HttpContext, out var redirect))
      {
        ctx.Result = redirect;
        return;
      }
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

  private static async Task<bool> From(HttpContext ctx)
  {
    var current = GetCurrent(ctx);
    var authz = GetAuthz(ctx);
    var app = GetApplication(ctx);

    if (!await LoadCurrentOrganization.From(ctx))
      return false;

    if (!ctx.Request.RouteValues.TryGetValue("game", out var slugValue) || (slugValue is not string slug))
      return false;

    var game = app.Account.GetGame(current.Organization, slug);
    if (game is null)
      return false;

    if (current.MustAuthenticate)
    {
      var auth = await authz.AuthorizeAsync(current.Principal, game, Policy.GameMember);
      if (!auth.Succeeded)
        return false;
    }

    current.Game = game;
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

  private static UrlGenerator GetUrlGenerator(HttpContext ctx)
  {
    return ctx.RequestServices.GetRequiredService<UrlGenerator>();
  }

  //-----------------------------------------------------------------------------------------------

  private bool RedirectToGameSettingsPage(HttpContext ctx, out IActionResult redirect)
  {
    var current = GetCurrent(ctx);
    var url = GetUrlGenerator(ctx);
    if (current.Game.IsArchived && !current.Page.Matches("/Game/Settings"))
    {
      redirect = new RedirectResult(url.SettingsPage(current.Organization, current.Game));
      return true;
    }
    current.Page.HeaderPartial = "Layout/Default/Header/Game";
    redirect = new OkResult();
    return false;
  }

  //-----------------------------------------------------------------------------------------------
}