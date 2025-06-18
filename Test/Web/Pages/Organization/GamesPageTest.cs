namespace Void.Platform.Web;

public class OrganizationGamesPageTest : TestCase
{
  const string GamesPath = "/atari/games";

  //===============================================================================================
  // TEST GAMES PAGE
  //===============================================================================================

  [Fact]
  public async Task TestGetPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get(GamesPath);
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/atari/games",
          expectedTitle: "Games - Atari");

      var gameLinks = Assert.Html.SelectAll("[qa=active-games] [qa=game-link]", doc);
      Assert.Present(gameLinks);
      Assert.Equal(3, gameLinks.Count);
      Assert.Equal("/atari/asteroids", gameLinks[0].GetAttribute("href"));
      Assert.Equal("/atari/pitfall", gameLinks[1].GetAttribute("href"));
      Assert.Equal("/atari/pong", gameLinks[2].GetAttribute("href"));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageUnknownOrg()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/unknown/games");
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
      var response = await test.Get(GamesPath);
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
      var response = await test.Get(GamesPath);
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fatari%2Fgames", location);
    }
  }

  //===============================================================================================
  // TEST CREATE GAME
  //===============================================================================================

  [Fact]
  public async Task TestCreateGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");

      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"CreateGame.Name", "New Name"},
        {"CreateGame.Slug", "New Slug"},
        {"CreateGame.Description", "New Description"},
        {"CreateGame.Purpose", "Game"},
      });

      var response = await test.HxPost("/atari/games?handler=CreateGame", formData);
      var doc = Assert.Html.Partial(response);

      var gameLinks = Assert.Html.SelectAll("[qa=active-games] [qa=game-link]", doc);
      Assert.Present(gameLinks);
      Assert.Equal(4, gameLinks.Count);
      Assert.Equal("/atari/asteroids", gameLinks[0].GetAttribute("href"));
      Assert.Equal("/atari/new-slug", gameLinks[1].GetAttribute("href"));
      Assert.Equal("/atari/pitfall", gameLinks[2].GetAttribute("href"));
      Assert.Equal("/atari/pong", gameLinks[3].GetAttribute("href"));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCancelCreateGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var response = await test.HxPost("/atari/games?handler=CancelCreateGame");
      var doc = Assert.Html.Partial(response);
      Assert.Html.Select("[qa=games]", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------
}