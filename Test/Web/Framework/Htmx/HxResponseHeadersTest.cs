namespace Void.Platform.Web.Htmx;

using Microsoft.AspNetCore.Http;

public class HxResponseHeadersTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestKeys()
  {
    Assert.Equal("HX-Location", HxResponseHeaders.Key.Location);
    Assert.Equal("HX-Push-Url", HxResponseHeaders.Key.PushUrl);
    Assert.Equal("HX-Redirect", HxResponseHeaders.Key.Redirect);
    Assert.Equal("HX-Refresh", HxResponseHeaders.Key.Refresh);
    Assert.Equal("HX-Replace-Url", HxResponseHeaders.Key.ReplaceUrl);
    Assert.Equal("HX-Reselect", HxResponseHeaders.Key.Reselect);
    Assert.Equal("HX-Response", HxResponseHeaders.Key.Response);
    Assert.Equal("HX-Reswap", HxResponseHeaders.Key.Reswap);
    Assert.Equal("HX-Retarget", HxResponseHeaders.Key.Retarget);
    Assert.Equal("HX-Trigger", HxResponseHeaders.Key.Trigger);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestHxRefresh()
  {
    var context = new DefaultHttpContext();
    var headers = new HxResponseHeaders(context.Response.Headers);
    var result = headers.Refresh();
    Assert.Http.Ok(result);
    Assert.Http.HasHeader("true", HxResponseHeaders.Key.Response, context.Response);
    Assert.Http.HasHeader("true", HxResponseHeaders.Key.Refresh, context.Response);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestHxRedirect()
  {
    var location = "/some/place";
    var context = new DefaultHttpContext();
    var headers = new HxResponseHeaders(context.Response.Headers);
    var result = headers.Redirect(location);
    Assert.Http.Ok(result);
    Assert.Http.HasHeader("true", HxResponseHeaders.Key.Response, context.Response);
    Assert.Http.HasHeader(location, HxResponseHeaders.Key.Redirect, context.Response);
  }

  //-----------------------------------------------------------------------------------------------
}