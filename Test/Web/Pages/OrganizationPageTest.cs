namespace Void.Platform.Web;

public class OrganizationPageTest : TestCase
{
  const string OrgPath = "/atari";

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestRedirectsToGamesPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get(OrgPath);
      var location = Assert.Http.Redirect(response);
      Assert.Equal("/atari/games", location);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageUnknownOrg()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/unknown");
      var doc = Assert.Html.Document(response, expectedStatus: Http.StatusCode.NotFound);
      Assert.Html.Title("Page Not Found", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact()]
  public async Task TestGetPageUnauthorized()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("outsider");
      var response = await test.Get(OrgPath);
      var doc = Assert.Html.Document(response, expectedStatus: Http.StatusCode.NotFound);
      Assert.Html.Title("Page Not Found", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Get(OrgPath);
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fatari", location);
    }
  }

  //-----------------------------------------------------------------------------------------------
}