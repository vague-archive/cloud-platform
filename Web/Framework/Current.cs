namespace Void.Platform.Web;

using Microsoft.AspNetCore.Antiforgery;

public class Current
{
  //===============================================================================================
  // CONSTRUCTOR and DI SERVICES
  //===============================================================================================

  private IWebHostEnvironment Env { get; init; }
  private IHttpContextAccessor HttpContextAccessor { get; init; }
  private IAntiforgery Antiforgery { get; init; }
  private Application App { get; init; }

  public Current(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor, IAntiforgery antiforgery, Application app)
  {
    Env = env;
    HttpContextAccessor = httpContextAccessor;
    Antiforgery = antiforgery;
    App = app;
  }

  //===============================================================================================
  // INITIALIZE RAZOR PAGE SPECIFIC CONTEXT
  //===============================================================================================

  public void For(BasePage ctx)
  {
    principal = ctx.HttpContext.User.Wrap();
    page = new PageView(ctx.HttpContext, ctx.ViewData, ctx.ModelState);
    flash = new PageFlash(ctx.TempData);
  }

  public void For(PageController ctx)
  {
    principal = ctx.HttpContext.User.Wrap();
    page = new PageView(ctx.HttpContext, ctx.ViewData, ctx.ModelState);
    flash = new PageFlash(ctx.TempData);
  }

  public void For(ApiController ctx)
  {
    principal = ctx.HttpContext.User.Wrap();
    page = null;
    flash = null;
  }

  //===============================================================================================
  // Current.Environment
  //===============================================================================================

  public string EnvironmentName
  {
    get
    {
      return Env.EnvironmentName;
    }
  }

  public bool IsDevelopment
  {
    get
    {
      return Env.IsDevelopment();
    }
  }

  public bool IsProduction
  {
    get
    {
      return Env.IsProduction();
    }
  }

  public bool IsTest
  {
    get
    {
      return Env.IsEnvironment("test");
    }
  }

  //===============================================================================================
  // Current.Principal
  //===============================================================================================

  private UserPrincipal? principal;
  public bool HasPrincipal { get { return principal is not null; } }
  public UserPrincipal Principal
  {
    get
    {
      RuntimeAssert.Present(principal, "Current.Principal is missing");
      return principal;
    }
    set
    {
      principal = value;
    }
  }

  public Account.User? user;
  public bool HasUser { get { return HasPrincipal; } }
  public Account.User User
  {
    get
    {
      return user ??= LoadCurrentUser();
    }
  }

  private Account.User LoadCurrentUser()
  {
    var user = App.Account.GetAuthenticatedUser(Principal.Id);
    RuntimeAssert.Present(user, "Current.User could not be found");
    return user;
  }

  //===============================================================================================
  // Current.Organization
  //===============================================================================================

  private Account.Organization? organization;
  public bool HasOrganization { get { return organization is not null; } }
  public Account.Organization Organization
  {
    get
    {
      RuntimeAssert.Present(organization, "Current.Organization is missing");
      return organization;
    }
    set
    {
      organization = value;
    }
  }

  //===============================================================================================
  // Current.Game
  //===============================================================================================

  private Account.Game? game;
  public bool HasGame { get { return game is not null; } }
  public Account.Game Game
  {
    get
    {
      RuntimeAssert.Present(game, "Current.Game is missing");
      return game;
    }
    set
    {
      game = value;
    }
  }

  //===============================================================================================
  // Current.Page
  //===============================================================================================

  private PageView? page;
  public PageView Page
  {
    get
    {
      RuntimeAssert.Present(page, "Current.Page is missing");
      return page;
    }
  }

  private PageFlash? flash;
  public PageFlash Flash
  {
    get
    {
      RuntimeAssert.Present(flash, "Current.Flash is missing");
      return flash;
    }
  }

  //===============================================================================================
  // Current .MustAuthenticate and .HasAllowAnonymous
  //===============================================================================================

  public bool MustAuthenticate
  {
    get
    {
      return !HasAllowAnonymous;
    }
  }

  private bool? hasAllowAnonymous;
  public bool HasAllowAnonymous
  {
    get
    {
      return hasAllowAnonymous ??= HasAttribute<AllowAnonymousAttribute>();
    }
  }

  private bool HasAttribute<T>() where T : Attribute
  {
    return HttpContext.GetEndpoint()?.Metadata.GetMetadata<T>() is not null;
  }

  //===============================================================================================
  // Current.CsrfToken
  //===============================================================================================

  private string? csrfToken;
  public string CsrfToken
  {
    get
    {
      return csrfToken ??= GetCsrfToken();
    }
  }

  private string GetCsrfToken()
  {
    var token = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken;
    RuntimeAssert.Present(token);
    return token;
  }

  //===============================================================================================
  // PRIVATE HELPERS
  //===============================================================================================

  private HttpContext HttpContext
  {
    get
    {
      RuntimeAssert.Present(HttpContextAccessor.HttpContext, "Current.HttpContext is missing");
      return HttpContextAccessor.HttpContext;
    }
  }

  //-----------------------------------------------------------------------------------------------
}