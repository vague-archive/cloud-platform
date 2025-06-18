namespace Void.Platform.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

public class FirewallTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  public const string IP1 = "1.1.1.1";
  public const string IP2 = "2.2.2.2";

  public const string ALLOWED_PATH = "/ping";
  public const string FORBIDDEN_PATH = "/.env";

  public const string ALLOWED_METHOD = "GET";
  public const string FORBIDDEN_METHOD = "CONNECT";

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestFirewallForbiddenPaths()
  {
    Assert.False(Firewall.IsPathForbidden("/"));
    Assert.False(Firewall.IsPathForbidden("/ping"));
    Assert.False(Firewall.IsPathForbidden("/login"));
    Assert.False(Firewall.IsPathForbidden("/profile"));
    Assert.False(Firewall.IsPathForbidden("/join"));
    Assert.False(Firewall.IsPathForbidden("/downloads"));
    Assert.False(Firewall.IsPathForbidden("/sysadmin"));
    Assert.False(Firewall.IsPathForbidden("/atari"));
    Assert.False(Firewall.IsPathForbidden("/atari/pong"));

    Assert.True(Firewall.IsPathForbidden("/.env"));
    Assert.True(Firewall.IsPathForbidden("/.php"));
    Assert.True(Firewall.IsPathForbidden("/.ssh"));
    Assert.True(Firewall.IsPathForbidden("/.git"));
    Assert.True(Firewall.IsPathForbidden("/foo/bar.php"));
    Assert.True(Firewall.IsPathForbidden("/foo/bar.env"));
    Assert.True(Firewall.IsPathForbidden("/wp-config"));
    Assert.True(Firewall.IsPathForbidden("/wp-config/foo/bar"));
    Assert.True(Firewall.IsPathForbidden("/tmp"));
    Assert.True(Firewall.IsPathForbidden("/tmp/foo/bar"));
    Assert.True(Firewall.IsPathForbidden("/autodiscover"));
    Assert.True(Firewall.IsPathForbidden("/autodiscover/autodiscover.json"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestFirewallForbiddenMethods()
  {
    Assert.False(Firewall.IsMethodForbidden("OPTIONS"));
    Assert.False(Firewall.IsMethodForbidden("HEAD"));
    Assert.False(Firewall.IsMethodForbidden("GET"));
    Assert.False(Firewall.IsMethodForbidden("POST"));
    Assert.False(Firewall.IsMethodForbidden("PUT"));
    Assert.False(Firewall.IsMethodForbidden("PATCH"));
    Assert.False(Firewall.IsMethodForbidden("DELETE"));
    Assert.False(Firewall.IsMethodForbidden("delete"));
    Assert.False(Firewall.IsMethodForbidden("Delete"));
    Assert.False(Firewall.IsMethodForbidden("DeLeTe"));

    Assert.True(Firewall.IsMethodForbidden("PROPFIND"));
    Assert.True(Firewall.IsMethodForbidden("PROPPATCH"));
    Assert.True(Firewall.IsMethodForbidden("MKCOL"));
    Assert.True(Firewall.IsMethodForbidden("COPY"));
    Assert.True(Firewall.IsMethodForbidden("MOVE"));
    Assert.True(Firewall.IsMethodForbidden("LOCK"));
    Assert.True(Firewall.IsMethodForbidden("UNLOCK"));
    Assert.True(Firewall.IsMethodForbidden("VERSION-CONTROL"));
    Assert.True(Firewall.IsMethodForbidden("REPORT"));
    Assert.True(Firewall.IsMethodForbidden("CHECKIN"));
    Assert.True(Firewall.IsMethodForbidden("CHECKOUT"));
    Assert.True(Firewall.IsMethodForbidden("UNCHECKOUT"));
    Assert.True(Firewall.IsMethodForbidden("MERGE"));
    Assert.True(Firewall.IsMethodForbidden("MKWORKSPACE"));
    Assert.True(Firewall.IsMethodForbidden("BASELINE-CONTROL"));
    Assert.True(Firewall.IsMethodForbidden("SEARCH"));
    Assert.True(Firewall.IsMethodForbidden("RPC_IN_DATA"));
    Assert.True(Firewall.IsMethodForbidden("RPC_OUT_DATA"));
    Assert.True(Firewall.IsMethodForbidden("BCOPY"));
    Assert.True(Firewall.IsMethodForbidden("BMOVE"));
    Assert.True(Firewall.IsMethodForbidden("LABEL"));
    Assert.True(Firewall.IsMethodForbidden("ORDERPATCH"));
    Assert.True(Firewall.IsMethodForbidden("CONNECT"));
    Assert.True(Firewall.IsMethodForbidden("TRACE"));
    Assert.True(Firewall.IsMethodForbidden("TRACK"));
    Assert.True(Firewall.IsMethodForbidden("track"));
    Assert.True(Firewall.IsMethodForbidden("Track"));
    Assert.True(Firewall.IsMethodForbidden("TrAcK"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestFirewallAllowed()
  {
    var cache = BuildTestCache();
    var next = new TestRequestDelegate();
    var opts = DefaultOptions();
    var firewall = new Firewall(next.Delegate, Logger, cache, opts);

    var ctx = BuildContext(ALLOWED_PATH, IP1);
    await firewall.Invoke(ctx);
    Assert.True(next.HasBeenCalled);
    Assert.Equal(Http.StatusCode.Ok, ctx.Response.StatusCode);
    Assert.False(cache.Contains(CacheKey.FirewallBlocked(IP1)));
  }

  [Fact]
  public async Task TestFirewallForbidden()
  {
    var cache = BuildTestCache();
    var next = new TestRequestDelegate();
    var opts = DefaultOptions();
    var firewall = new Firewall(next.Delegate, Logger, cache, opts);

    var ctx = BuildContext(FORBIDDEN_PATH, IP1);
    await firewall.Invoke(ctx);
    Assert.False(next.HasBeenCalled);
    Assert.Equal(Http.StatusCode.Forbidden, ctx.Response.StatusCode);
    Assert.True(cache.Contains(CacheKey.FirewallBlocked(IP1)));
  }

  [Fact]
  public async Task TestFirewallIpBlocked()
  {
    var cache = BuildTestCache();
    var next = new TestRequestDelegate();
    var opts = DefaultOptions();
    var firewall = new Firewall(next.Delegate, Logger, cache, opts);
    await cache.SetAsync(CacheKey.FirewallBlocked(IP1), true, token: CancelToken);
    var ctx = BuildContext(ALLOWED_PATH, IP1);
    await firewall.Invoke(ctx);
    Assert.False(next.HasBeenCalled);
    Assert.Equal(Http.StatusCode.Forbidden, ctx.Response.StatusCode);
  }

  [Fact]
  public async Task TestFirewallDisabled()
  {
    var cache = BuildTestCache();
    var next = new TestRequestDelegate();
    var opts = DefaultOptions(enabled: false);
    var firewall = new Firewall(next.Delegate, Logger, cache, opts);

    var ctx = BuildContext(FORBIDDEN_PATH, IP1);
    await firewall.Invoke(ctx);
    Assert.True(next.HasBeenCalled);
    Assert.Equal(Http.StatusCode.Ok, ctx.Response.StatusCode);
    Assert.False(cache.Contains(CacheKey.FirewallBlocked(IP1)));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestFirewallMethodBlockedWithoutBlockingIP()
  {
    var cache = BuildTestCache();
    var next = new TestRequestDelegate();
    var opts = DefaultOptions();
    var firewall = new Firewall(next.Delegate, Logger, cache, opts);

    var ctx = BuildContext(ALLOWED_PATH, IP1, method: FORBIDDEN_METHOD);
    await firewall.Invoke(ctx);
    Assert.False(next.HasBeenCalled);
    Assert.Equal(Http.StatusCode.MethodNotAllowed, ctx.Response.StatusCode);
    Assert.False(cache.Contains(CacheKey.FirewallBlocked(IP1)));
  }

  //-----------------------------------------------------------------------------------------------

  private HttpContext BuildContext(string path, string ip, string method = ALLOWED_METHOD)
  {
    var ctx = new DefaultHttpContext();
    ctx.Request.Method = method;
    ctx.Request.Path = path;
    ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
    return ctx;
  }

  private IOptions<Firewall.Options> DefaultOptions(bool enabled = true)
  {
    return Options.Create(new Firewall.Options
    {
      Enabled = enabled,
    });
  }

  //-----------------------------------------------------------------------------------------------
}