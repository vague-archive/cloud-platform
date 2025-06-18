namespace Void.Platform.Web.Htmx;

using Microsoft.AspNetCore.Http;

public class HxRequestHeadersTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestKeys()
  {
    Assert.Equal("HX-Boosted", HxRequestHeaders.Key.Boosted);
    Assert.Equal("HX-Current-URL", HxRequestHeaders.Key.CurrentUrl);
    Assert.Equal("HX-Prompt", HxRequestHeaders.Key.Prompt);
    Assert.Equal("HX-Request", HxRequestHeaders.Key.Request);
    Assert.Equal("HX-Target", HxRequestHeaders.Key.Target);
    Assert.Equal("HX-Trigger", HxRequestHeaders.Key.Trigger);
    Assert.Equal("HX-Trigger-Name", HxRequestHeaders.Key.TriggerName);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestEmptyHxRequestHeaders()
  {
    var context = new DefaultHttpContext();
    var headers = new HxRequestHeaders(context.Request.Headers);

    Assert.Equal("", headers.CurrentUrl);
    Assert.Equal("", headers.CurrentUrl);
    Assert.Equal("", headers.Prompt);
    Assert.Equal("", headers.Target);
    Assert.Equal("", headers.TriggerName);
    Assert.Equal("", headers.Trigger);
    Assert.False(headers.Boosted);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestFullHxRequestHeaders()
  {
    var context = new DefaultHttpContext();
    context.Request.Headers["HX-Boosted"] = "true";
    context.Request.Headers["HX-Current-URL"] = "current-url";
    context.Request.Headers["HX-Prompt"] = "prompt";
    context.Request.Headers["HX-Request"] = "request";
    context.Request.Headers["HX-Target"] = "target";
    context.Request.Headers["HX-Trigger"] = "trigger";
    context.Request.Headers["HX-Trigger-Name"] = "trigger-name";

    var headers = new HxRequestHeaders(context.Request.Headers);

    Assert.Equal("current-url", headers.CurrentUrl);
    Assert.Equal("prompt", headers.Prompt);
    Assert.Equal("target", headers.Target);
    Assert.Equal("trigger-name", headers.TriggerName);
    Assert.Equal("trigger", headers.Trigger);
    Assert.True(headers.Boosted);
  }

  //-----------------------------------------------------------------------------------------------
}