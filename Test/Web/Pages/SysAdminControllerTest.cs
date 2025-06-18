namespace Void.Platform.Web;

public class SysAdminControllerTest : TestCase
{
  //===============================================================================================
  // TEST INDEX PAGE
  //===============================================================================================

  [Fact]
  public async Task TestIndexPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("sysadmin");
      var response = await test.Get("/sysadmin");
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/sysadmin",
          expectedTitle: "System Administration");

      var stats = Assert.Html.Select("[qa=sysadmin-stats]", doc);
      Assert.Equal("6", Assert.Html.Select("[qa=organizations]", stats).TextContent);
      Assert.Equal("10", Assert.Html.Select("[qa=users]", stats).TextContent);
      Assert.Equal("14", Assert.Html.Select("[qa=tokens]", stats).TextContent);
      Assert.Equal("10", Assert.Html.Select("[qa=games]", stats).TextContent);
      Assert.Equal("5", Assert.Html.Select("[qa=tools]", stats).TextContent);
      Assert.Equal("0", Assert.Html.Select("[qa=deploys]", stats).TextContent);

      var orgs = Assert.Html.SelectAll("[qa=sysadmin-organizations] [qa=organization]", doc);
      Assert.Equal(6, orgs.Count);
      Assert.Equal("/aardvark", orgs[0].GetAttribute("href"));
      Assert.Equal("/atari", orgs[1].GetAttribute("href"));
      Assert.Equal("/nintendo", orgs[2].GetAttribute("href"));
      Assert.Equal("/secret", orgs[3].GetAttribute("href"));
      Assert.Equal("/void", orgs[4].GetAttribute("href"));
      Assert.Equal("/zoidberg", orgs[5].GetAttribute("href"));

      Assert.Html.Select("[qa=sysadmin-recent-branches]", doc);
      Assert.Html.Select("[qa=sysadmin-expired-branches]", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestActiveUserForbidden()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var response = await test.Get("/sysadmin");
      Assert.Http.NotFound(response);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestAnonymousRedirectedToLogin()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Get("/sysadmin");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fsysadmin", location);
    }
  }

  //===============================================================================================
  // TEST AJAX ACTIONS
  //===============================================================================================

  [Fact]
  public async Task TestRefreshFileStats()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("sysadmin");
      var response = await test.HxPost("/sysadmin/refresh-file-stats");
      var html = Assert.Html.Partial(response);

      var stats = Assert.Html.Select("[qa=sysadmin-stats]", html);
      Assert.Equal("6", Assert.Html.Select("[qa=organizations]", stats).TextContent);
      Assert.Equal("10", Assert.Html.Select("[qa=users]", stats).TextContent);
      Assert.Equal("14", Assert.Html.Select("[qa=tokens]", stats).TextContent);
      Assert.Equal("10", Assert.Html.Select("[qa=games]", stats).TextContent);
      Assert.Equal("5", Assert.Html.Select("[qa=tools]", stats).TextContent);
      Assert.Equal("0", Assert.Html.Select("[qa=branches]", stats).TextContent);
      Assert.Equal("0", Assert.Html.Select("[qa=deploys]", stats).TextContent);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestSendExampleEmail()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("sysadmin");
      var response = await test.HxPost("/sysadmin/send-example-email");
      var html = Assert.Html.Partial(response);
      var message = Assert.Html.Select("h4", html);
      Assert.Equal("an email has been sent", message.TextContent);

      var sent = Assert.Mailed(test.Mailer, "example", user.Email);
      Assert.Equal("Hello World", sent.Data["message"]);
    }
  }

  //-----------------------------------------------------------------------------------------------
}