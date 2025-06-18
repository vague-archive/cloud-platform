namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc.Routing;

public class UrlGenerator
{
  //-----------------------------------------------------------------------------------------------

  private IUrlProvider provider;

  //-----------------------------------------------------------------------------------------------

  public UrlGenerator(IUrlProvider provider)
  {
    this.provider = provider;
  }

  //-----------------------------------------------------------------------------------------------

  public string HomePage(bool full = false)
  {
    return Page("/Home", full: full);
  }

  public string DownloadsPage(bool full = false)
  {
    return Page("/Downloads", values: new { repo = "", assetId = "" }, full: full);
  }

  public string ProfilePage(bool full = false)
  {
    return Page("/Profile", full: full);
  }

  public string LoginPage(bool full = false)
  {
    return Page("/Login", values: new { provider = "" }, full: full);
  }

  public string LoginPage(string? origin, bool cli = false, bool full = false)
  {
    return Page("/Login", values: new { provider = "", origin, cli = cli ? "true" : null }, full: full);
  }

  public string GitHubLoginPage(string? origin, bool cli = false, bool full = false)
  {
    return Page("/Login", values: new { provider = "github", origin, cli = cli ? "true" : null }, full: full);
  }

  public string DiscordLoginPage(string? origin, bool cli = false, bool full = false)
  {
    return Page("/Login", values: new { provider = "discord", origin, cli = cli ? "true" : null }, full: full);
  }

  public string PasswordLoginPage(string? origin, bool cli, bool full = false)
  {
    return Page("/Login", values: new { provider = "password", origin, cli = cli ? "true" : null }, full: full);
  }

  public string LoginCallbackUrl(Account.IdentityProvider provider)
  {
    return Page("/Login", pageHandler: "callback", values: new { provider = provider.ToString().ToLower() }, full: true);
  }

  public string JoinCallbackUrl(Account.IdentityProvider provider)
  {
    return Page("/Join/Provider", pageHandler: "callback", values: new { provider = provider.ToString().ToLower() }, full: true);
  }

  public string OrganizationPage(Account.Organization org, bool full = false)
  {
    return Page("/Organization", values: new { org = org }, full: full);
  }

  public string GamesPage(Account.Organization org, bool full = false)
  {
    return Page("/Organization/Games", values: new { org = org }, full: full);
  }

  public string SettingsPage(Account.Organization org, bool full = false)
  {
    return Page("/Organization/Settings", values: new { org = org }, full: full);
  }

  public string SharePage(Account.Organization org, Account.Game game, bool full = false)
  {
    return Page("/Game/Share", values: new { org = org, game = game }, full: full);
  }

  public string ServeGame(Account.Organization org, Account.Game game, Share.Branch branch, bool full = false)
  {
    return EnforceTrailingSlash(Action("Index", "ServeGame", values: new { org = org, game = game, slug = branch.Slug }, full: full));
  }

  public string ServeGamePassword(Account.Organization org, Account.Game game, Share.Branch branch, bool full = false)
  {
    return Action("Password", "ServeGame", values: new { org = org, game = game, slug = branch.Slug });
  }

  public string SettingsPage(Account.Organization org, Account.Game game, bool full = false)
  {
    return Page("/Game/Settings", values: new { org = org, game = game }, full: full);
  }

  public string JoinPage(string token = Account.SendInviteCommand.TokenPlaceholder, bool full = false)
  {
    return Page("/Join", values: new { token }, full: full);
  }

  public string SysAdminPage(bool full = false)
  {
    return Action("Index", "SysAdmin", full: full);
  }

  public string SysAdminFileStoreDiff(bool full = false)
  {
    return Action("Diff", "SysAdmin", full: full);
  }

  public string SysAdminDownloadMissingFile(string file, bool full = false)
  {
    return Action("DownloadMissingFile", "SysAdmin", values: new { file }, full: full);
  }

  public string SysAdminUploadMissingFile(string file, bool full = false)
  {
    return Action("UploadMissingFile", "SysAdmin", values: new { file }, full: full);
  }

  public string SysAdminCacheDelete(string key, bool full = false)
  {
    return Action("CacheDelete", "SysAdmin", values: new { key }, full: full);
  }

  public string SysAdminTrashDelete(string key, bool full = false)
  {
    return Action("TrashDelete", "SysAdmin", values: new { key }, full: full);
  }

  public string SysAdminDeleteExpiredBranch(Share.Branch branch, bool full = false)
  {
    return Action("DeleteExpiredBranch", "SysAdmin", values: new { branchId = branch.Id }, full: full);
  }

  //-----------------------------------------------------------------------------------------------

  public string Route(string routeName, object? values = null, bool full = false)
  {
    return provider.Route(
      name: routeName,
      values: Slugify(values),
      scheme: full ? provider.Scheme : null,
      host: full ? provider.HostName : null
    );
  }

  public string Page(string pageName, string? pageHandler = null, object? values = null, bool full = false)
  {
    return provider.Page(
      pageName: pageName,
      pageHandler: pageHandler,
      values: Slugify(values),
      scheme: full ? provider.Scheme : null,
      host: full ? provider.HostName : null
    );
  }

  public string Action(string action, string? controller = null, object? values = null, bool full = false)
  {
    return provider.Action(
      action: action,
      controller: controller,
      values: Slugify(values),
      scheme: full ? provider.Scheme : null,
      host: full ? provider.HostName : null
    );
  }

  public string Content(string fileName)
  {
    return provider.Content(fileName);
  }

  //-----------------------------------------------------------------------------------------------

  private string EnforceTrailingSlash(string path)
  {
    return path.EndsWith("/") ? path : path + "/";
  }

  //-----------------------------------------------------------------------------------------------

  private RouteValueDictionary? Slugify(object? values = null)
  {
    if (values is null)
      return null;
    RouteValueDictionary? routeValues = new RouteValueDictionary(values);
    var keys = new List<string>(routeValues.Keys); // will be modifying while iterating
    foreach (var key in keys)
    {
      var value = routeValues[key];
      if (value is Account.Organization org)
        routeValues[key] = org.Slug;
      else if (value is Account.Game game)
        routeValues[key] = game.Slug;
      else if (value is Share.Branch branch)
        routeValues[key] = branch.Slug;
    }
    return routeValues;
  }
}

//=================================================================================================
//
// Ugh. The insanse abstractions make it impossible to write tests. The only way to generate
// urls is via UrlHelper, the only way to get a UrlHelper is thru the UrlHelperFactory, and
// that requires constructing an ActionContext, and.... it's all just a spaghetti mess...
//
// .... on top of that you can't f*****g mock out IUrlHelper either because half of the methods
// we use, e.g. .Page(), are not actual methods on the interface but are instead extension
// methods, which make it impossible to use Moq or NSubstitute to mock them out...
//
// Basically ASP.NET is garbage, but here we are
//
// In order to make it easier to use and test I need to hide the use of UrlHelper (behind
// the UrlGenerator above) and in order to make that testable it can't use the UrlHelper directly
// so it does it through the UrlProvider below
//
// Je**s Ch***t this is utter garbage
//
//=================================================================================================


public interface IUrlProvider
{
  public string Scheme { get; }
  public string HostName { get; }
  public string Route(string name, RouteValueDictionary? values, string? scheme, string? host);
  public string Page(string pageName, string? pageHandler, RouteValueDictionary? values, string? scheme, string? host);
  public string Action(string action, string? controller, RouteValueDictionary? values, string? scheme, string? host);
  public string Content(string fileName);
}

public class UrlProvider : IUrlProvider
{
  private IUrlHelper urlHelper;

  public string Scheme { get; init; }
  public string HostName { get; init; }

  public UrlProvider(IUrlHelperFactory urlHelperFactory, IHttpContextAccessor httpContextAccessor, Config config)
  {
    var httpContext = httpContextAccessor.HttpContext;
    if (httpContext is null)
      throw new InvalidOperationException("no HttpContext available");

    var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
    urlHelper = urlHelperFactory.GetUrlHelper(actionContext);

    var publicUrl = config.Web.PublicUrl;
    Scheme = publicUrl.Scheme;
    HostName = publicUrl.IsDefaultPort ? publicUrl.Host : $"{publicUrl.Host}:{publicUrl.Port}";
  }

  public string Route(string routeName, RouteValueDictionary? values, string? scheme, string? host)
  {
    var value = urlHelper.RouteUrl(routeName, values, scheme, host);
    if (value is null)
      throw new InvalidOperationException($"unknown route {routeName}");
    return value;
  }

  public string Page(string pageName, string? pageHandler, RouteValueDictionary? values, string? scheme, string? host)
  {
    var value = urlHelper.Page(pageName, pageHandler, values, scheme, host);
    if (value is null)
      throw new InvalidOperationException($"unknown page {pageName} handler {pageHandler}");
    return value;
  }

  public string Action(string action, string? controller, RouteValueDictionary? values, string? scheme, string? host)
  {
    var value = urlHelper.Action(action, controller, values, scheme, host);
    if (value is null)
      throw new InvalidOperationException($"unknown action {controller}/{action}");
    return value;
  }

  public string Content(string fileName)
  {
    var value = urlHelper.Content(fileName);
    if (value is null)
      throw new InvalidOperationException($"unknown content {fileName}");
    return value;
  }
}