namespace Void.Platform.Web;

using System.Security.Claims;

[LoadCurrent]
[Authorize(Policy.CookieAuthenticated)]
public abstract class PageController : Controller
{
  //-----------------------------------------------------------------------------------------------

  public new ClaimsPrincipal User
  {
    get
    {
      throw new NotImplementedException("use Current.Principal instead - you'll get our wrapper class");
    }
  }

  //-----------------------------------------------------------------------------------------------

  private UrlGenerator? _url;
  protected new UrlGenerator Url
  {
    get
    {
      return _url ??= GetService<UrlGenerator>();
    }
  }

  //-----------------------------------------------------------------------------------------------

  protected T GetService<T>() where T : class
  {
    return HttpContext.RequestServices.GetRequiredService<T>();
  }

  //-----------------------------------------------------------------------------------------------
}