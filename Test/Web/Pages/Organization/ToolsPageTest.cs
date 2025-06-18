namespace Void.Platform.Web;

public class OrganizationToolsPageTest : TestCase
{
  const string ToolsPath = "/atari/tools";

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get(ToolsPath);
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/atari/tools",
          expectedTitle: "Tools - Atari");

      var toolLinks = Assert.Html.SelectAll("[qa=active-games] [qa=game-link]", doc);
      Assert.Present(toolLinks);
      Assert.Single(toolLinks);
      Assert.Equal("/atari/retro-tool", toolLinks[0].GetAttribute("href"));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageUnknownOrg()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/unknown/tools");
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
      var response = await test.Get(ToolsPath);
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
      var response = await test.Get(ToolsPath);
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fatari%2Ftools", location);
    }
  }

  //===============================================================================================
  // TEST CREATE TOOL
  //===============================================================================================

  [Fact]
  public async Task TestCreateTool()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");

      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"CreateGame.Name", "New Name"},
        {"CreateGame.Slug", "New Slug"},
        {"CreateGame.Description", "New Description"},
        {"CreateGame.Purpose", "Tool"},
      });

      var response = await test.HxPost("/atari/tools?handler=CreateGame", formData);
      var doc = Assert.Html.Partial(response);

      var toolLinks = Assert.Html.SelectAll("[qa=active-games] [qa=game-link]", doc);
      Assert.Present(toolLinks);
      Assert.Equal(2, toolLinks.Count);
      Assert.Equal("/atari/new-slug", toolLinks[0].GetAttribute("href"));
      Assert.Equal("/atari/retro-tool", toolLinks[1].GetAttribute("href"));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCancelCreateTool()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var response = await test.HxPost("/atari/tools?handler=CancelCreateGame");
      var doc = Assert.Html.Partial(response);
      Assert.Html.Select("[qa=games]", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------
}