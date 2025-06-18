namespace Void.Platform.Web;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

// When we hosted at fly.io, they provided a rudimentary WAF that blocked a lot of spam
// traffic from us. Now we've moved to AWS we don't get that anymore. AWS do have a WAF
// but it doesn't work with an NLB and I haven't had time to investigate the alternatives
// yet, so for a short term hack fix...
//
// ... this middleware implements a very naive WAF (web application firewall)... we should
// really do this at the infrastructure layer, but until then...

public class Firewall
{
  //-----------------------------------------------------------------------------------------------

  public record Options
  {
    public bool Enabled { get; set; } = true;
    public Duration BlockedFor { get; set; } = Duration.FromMinutes(5);
  }

  //-----------------------------------------------------------------------------------------------

  private static readonly List<Regex> ForbiddenPaths = new()
  {
    new Regex(@"\.env$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    new Regex(@"\.php$",  RegexOptions.IgnoreCase | RegexOptions.Compiled),
    new Regex(@"\.git$",  RegexOptions.IgnoreCase | RegexOptions.Compiled),
    new Regex(@"\.ssh$",  RegexOptions.IgnoreCase | RegexOptions.Compiled),
    new Regex(@"^\/wp-config", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    new Regex(@"^\/tmp", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    new Regex(@"^\/autodiscover", RegexOptions.IgnoreCase | RegexOptions.Compiled),
  };

  public static bool IsPathForbidden(string path)
  {
    return ForbiddenPaths.Any(re => re.IsMatch(path));
  }

  //-----------------------------------------------------------------------------------------------

  private static readonly HashSet<string> AllowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
  {
    "OPTIONS",
    "HEAD",
    "GET",
    "POST",
    "PATCH",
    "PUT",
    "DELETE",
  };

  public static bool IsMethodForbidden(string method)
  {
    return !AllowedMethods.Contains(method);
  }

  //-----------------------------------------------------------------------------------------------

  private ILogger Logger { get; init; }
  private ICache Cache { get; init; }
  private RequestDelegate Next { get; init; }
  private bool Enabled { get; init; }
  private Duration BlockedFor { get; init; }

  public Firewall(RequestDelegate next, ILogger logger, ICache cache, IOptions<Firewall.Options> opts)
  {
    Logger = logger;
    Cache = cache;
    Next = next;
    Enabled = opts.Value.Enabled;
    BlockedFor = opts.Value.BlockedFor;
  }

  //-----------------------------------------------------------------------------------------------

  public async Task Invoke(HttpContext ctx)
  {
    if (Enabled && IsMethodForbidden(ctx))
    {
      ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
    }
    else if (Enabled && await IsPathForbidden(ctx))
    {
      ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
    }
    else
    {
      await Next(ctx);
    }
  }

  private bool IsMethodForbidden(HttpContext ctx)
  {
    return IsMethodForbidden(ctx.Request.Method);
  }

  private async Task<bool> IsPathForbidden(HttpContext ctx)
  {
    var path = ctx.Request.Path.ToString();
    var ip = ctx.Connection.RemoteIpAddress?.ToString();

    if (ip is null)
    {
      Logger.Warning($"unexpected request is missing IP, could be a unit test, this should NOT happen in production, if we don't see this message in the production logs then I can block this request, but until I know for sure I'm logging it and letting it thru.");
      return false;
    }

    var cacheKey = CacheKey.FirewallBlocked(ip);
    if (await Cache.ContainsAsync(cacheKey))
    {
      Logger.Warning($"[WAF] TEMPORARILY BLOCKED {ip} FROM {ctx.Request.Path}{ctx.Request.QueryString}");
      return true;
    }

    if (IsPathForbidden(path))
    {
      var ua = ctx.Request.Headers[Http.Header.UserAgent].ToString();
      Logger.Warning($"[WAF] BLOCKED {ip} (ua: {ua}) FROM {ctx.Request.Path}{ctx.Request.QueryString} - WILL BE BLOCKED FOR {Format.Duration(BlockedFor)}");
      await Cache.SetAsync(cacheKey, true, new CacheEntryOptions(BlockedFor));
      return true;
    }

    return false;
  }
}

//=================================================================================================
// DI SERVICE BUILDER
//=================================================================================================

public static class FirewallExtensions
{
  public static IServiceCollection AddVoidFirewall(this IServiceCollection services, bool enabled = true)
  {
    return services.Configure<Firewall.Options>(opts =>
    {
      opts.Enabled = enabled;
    });
  }

  public static IApplicationBuilder UseVoidFirewall(this IApplicationBuilder builder)
  {
    return builder.UseMiddleware<Firewall>();
  }
}