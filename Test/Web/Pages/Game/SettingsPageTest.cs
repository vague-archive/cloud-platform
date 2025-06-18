namespace Void.Platform.Web;

public class GameSettingsPageTest : TestCase
{
  //===============================================================================================
  // TEST GAME SETTINGS PAGE
  //===============================================================================================

  [Fact]
  public async Task TestGetPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/atari/pong/settings");
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/atari/pong/settings",
          expectedTitle: "Settings - Pong");
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageForArchivedGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/atari/et/settings");
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/atari/et/settings",
          expectedTitle: "Settings - E.T. the Extra-Terrestrial");
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageUnknownGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/atari/unknown/settings");
      var content = Assert.Http.NotFound(response);
      var doc = Assert.Html.Document(content);
      Assert.Html.Title("Page Not Found", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageUnauthorized()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("outsider");
      var response = await test.Get("/atari/pong/settings");
      var content = Assert.Http.NotFound(response);
      var doc = Assert.Html.Document(content);
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
      var response = await test.Get("/atari/pong/settings");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fatari%2Fpong%2Fsettings", location);
    }
  }

  //===============================================================================================
  // TEST UPDATE GAME SETTINGS
  //===============================================================================================

  [Fact]
  public async Task TestUpdateGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");

      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"UpdateGame.Name", "New Name"},
        {"UpdateGame.Slug", "New Slug"},
        {"UpdateGame.Description", "New Description"},
      });

      var response = await test.HxPost("/atari/pong/settings?handler=UpdateGame", formData);
      var location = Assert.Http.Redirect(response);
      Assert.Equal("/atari/new-slug/settings", location);

      response = await test.Get(location);
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/atari/new-slug/settings",
          expectedTitle: "Settings - New Name");

      var reloaded = test.Factory.LoadGame("pong");
      Assert.Equal("New Name", reloaded.Name);
      Assert.Equal("new-slug", reloaded.Slug);
      Assert.Equal("New Description", reloaded.Description);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCancelUpdateGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var response = await test.HxPost("/atari/pong/settings?handler=CancelUpdateGame");
      var doc = Assert.Html.Partial(response);
      Assert.Html.Select("[qa=active-game-settings]", doc);
    }
  }

  //===============================================================================================
  // TEST ARCHIVE, RESTORE, and DELETE GAME
  //===============================================================================================

  [Fact]
  public async Task TestArchiveGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");

      var game = test.Factory.LoadGame("pong");
      Assert.False(game.IsArchived);

      var response = await test.HxPost($"/atari/{game.Slug}/settings?handler=ArchiveGame");
      Assert.Http.Refresh(response);

      var reloaded = test.App.Account.GetGame(game.Id);
      Assert.Present(reloaded);
      Assert.True(reloaded.IsArchived);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestRestoreGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");

      var game = test.Factory.LoadGame("et");
      Assert.True(game.IsArchived);

      var response = await test.HxPost($"/atari/{game.Slug}/settings?handler=RestoreGame");
      Assert.Http.Refresh(response);

      var reloaded = test.App.Account.GetGame(game.Id);
      Assert.Present(reloaded);
      Assert.False(reloaded.IsArchived);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDeleteGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");

      var game = test.Factory.LoadGame("et");
      Assert.True(game.IsArchived);

      var response = await test.HxPost($"/atari/{game.Slug}/settings?handler=DeleteGame");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("/atari/games", location);

      var reloaded = test.App.Account.GetGame(game.Id);
      Assert.Absent(reloaded);
    }
  }

  //-----------------------------------------------------------------------------------------------
}