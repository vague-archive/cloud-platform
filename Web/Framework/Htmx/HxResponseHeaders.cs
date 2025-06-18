namespace Void.Platform.Web.Htmx;

public class HxResponseHeaders
{
  public static class Key
  {
    public const string Location = "HX-Location";
    public const string PushUrl = "HX-Push-Url";
    public const string Redirect = "HX-Redirect";
    public const string Refresh = "HX-Refresh";
    public const string ReplaceUrl = "HX-Replace-Url";
    public const string Reselect = "HX-Reselect";
    public const string Response = "HX-Response";
    public const string Reswap = "HX-Reswap";
    public const string Retarget = "HX-Retarget";
    public const string Trigger = "HX-Trigger";
  }

  private IHeaderDictionary headers;

  public HxResponseHeaders(IHeaderDictionary headers)
  {
    this.headers = headers;
    this.headers[Key.Response] = "true";
  }

  public IActionResult Refresh()
  {
    headers[Key.Refresh] = "true";
    return new OkResult();
  }

  public IActionResult Redirect(Uri value)
  {
    return Redirect(value.ToString());
  }

  public IActionResult Redirect(string value)
  {
    headers[Key.Redirect] = value;
    return new OkResult();
  }
}