namespace Void.Platform.Web;

public class ShareGamePageTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  const string PASSWORD = "You shall not pass!";

  //===============================================================================================
  // TEST GET SHARE PAGE
  //===============================================================================================

  [Fact]
  public async Task TestGetPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/atari/pong/share");
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/atari/pong/share",
          expectedTitle: "Share - Pong");
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageForArchivedGameRedirectsToSettings()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/atari/et/share");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("/atari/et/settings", location);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUnknownOrg()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/unknown/pong/share");
      var doc = Assert.Html.Document(response, expectedStatus: Http.StatusCode.NotFound);
      Assert.Html.Title("Page Not Found", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUnknownGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/atari/unknown/share");
      var doc = Assert.Html.Document(response, expectedStatus: Http.StatusCode.NotFound);
      Assert.Html.Title("Page Not Found", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUnauthorized()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("outsider");
      var response = await test.Get("/atari/pong/share");
      var doc = Assert.Html.Document(response, expectedStatus: Http.StatusCode.NotFound);
      Assert.Html.Title("Page Not Found", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Get("/atari/pong/share");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fatari%2Fpong%2Fshare", location);
    }
  }

  //===============================================================================================
  // TEST PIN BRANCH
  //===============================================================================================

  [Fact]
  public async Task TestPinBranch()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var branch = test.Factory.CreateBranch(game);

      Assert.Preconditions.False(branch.IsPinned);

      var response = await test.HxPost($"/{org.Slug}/{game.Slug}/share?handler=PinBranch&slug={branch.Slug}&isPinned=true");
      Assert.Http.Ok(response);

      var reloaded = test.Factory.LoadBranch(branch.Id);
      Assert.Present(reloaded);
      Assert.True(reloaded.IsPinned);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUnpinBranch()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var branch = test.Factory.CreateBranch(game, isPinned: true);

      Assert.Preconditions.True(branch.IsPinned);

      var response = await test.HxPost($"/{org.Slug}/{game.Slug}/share?handler=PinBranch&slug={branch.Slug}&isPinned=false");
      Assert.Http.Ok(response);

      var reloaded = test.Factory.LoadBranch(branch.Id);
      Assert.Present(reloaded);
      Assert.False(reloaded.IsPinned);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestPinUnknownBranch()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var unknownId = Identify("unknown");
      var response = await test.HxPost($"/{org.Slug}/{game.Slug}/share?handler=PinBranch&slug=unknown&isPinned=true");
      Assert.Http.NotFound(response);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestPinBranchFromMismatchedGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var other = test.Factory.LoadGame(org, "pitfall");
      var branch = test.Factory.CreateBranch(game);

      Assert.NotEqual(other.Id, branch.GameId);
      Assert.False(branch.IsPinned);

      var response = await test.HxPost($"/{org.Slug}/{other.Slug}/share?handler=PinBranch&slug={branch.Slug}&isPinned=true");
      Assert.Http.NotFound(response);

      var reloaded = test.Factory.LoadBranch(branch.Id);
      Assert.Present(reloaded);
      Assert.False(reloaded.IsPinned);
    }
  }

  //===============================================================================================
  // TEST SET PASSWORD
  //===============================================================================================

  [Fact]
  public async Task TestSetPassword()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var branch = test.Factory.CreateBranch(game);

      Assert.False(branch.HasPassword);
      Assert.Null(branch.Password);

      var response = await test.HxPost($"/{org.Slug}/{game.Slug}/share?handler=SetBranchPassword&slug={branch.Slug}&enabled=true&password={PASSWORD}");
      Assert.Http.Ok(response);

      var reloaded = test.Factory.LoadBranch(branch.Id);
      Assert.Present(reloaded);
      Assert.True(reloaded.HasPassword);
      Assert.Equal(PASSWORD, reloaded.DecryptPassword(test.Encryptor));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestClearPassword()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var branch = test.Factory.CreateBranch(game, password: PASSWORD);

      Assert.True(branch.HasPassword);
      Assert.Equal(PASSWORD, branch.Password);

      var response = await test.HxPost($"/{org.Slug}/{game.Slug}/share?handler=SetBranchPassword&slug={branch.Slug}&enabled=false");
      Assert.Http.Ok(response);

      var reloaded = test.Factory.LoadBranch(branch.Id);
      Assert.Present(reloaded);
      Assert.False(reloaded.HasPassword);
      Assert.Null(reloaded.Password);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestSetPasswordOnUnknownBranch()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var response = await test.HxPost($"/{org.Slug}/{game.Slug}/share?handler=SetBranchPassword&slug=unknown&enabled=true&password=y0l0");
      Assert.Http.NotFound(response);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestSetPasswordOnBranchFromMismatchedGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var other = test.Factory.LoadGame(org, "pitfall");
      var branch = test.Factory.CreateBranch(game);

      Assert.NotEqual(other.Id, branch.GameId);
      Assert.False(branch.HasPassword);
      Assert.Null(branch.Password);

      var response = await test.HxPost($"/{org.Slug}/{other.Slug}/share?handler=SetBranchPassword&slug={branch.Slug}&enabled=true&password={PASSWORD}");
      Assert.Http.NotFound(response);

      var reloaded = test.Factory.LoadBranch(branch.Id);
      Assert.Present(reloaded);
      Assert.False(reloaded.HasPassword);
      Assert.Null(reloaded.Password);
    }
  }

  //===============================================================================================
  // TEST DELETE BRANCH
  //===============================================================================================

  [Fact]
  public async Task TestDeleteBranch()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var branch = test.Factory.CreateBranch(game);

      var response = await test.HxPost($"/{org.Slug}/{game.Slug}/share?handler=DeleteBranch&slug={branch.Slug}");
      Assert.Http.Ok(response);

      var reloaded = test.App.Share.GetBranch(branch.Id);
      Assert.Absent(reloaded);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDeleteUnknownBranch()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var response = await test.HxPost($"/{org.Slug}/{game.Slug}/share?handler=DeleteBranch&slug=unknown");
      Assert.Http.NotFound(response);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDeleteBranchFromMismatchedGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var other = test.Factory.LoadGame(org, "pitfall");
      var branch = test.Factory.CreateBranch(game);

      Assert.NotEqual(other.Id, branch.GameId);

      var response = await test.HxPost($"/{org.Slug}/{other.Slug}/share?handler=DeleteBranch&slug={branch.Slug}");
      Assert.Http.NotFound(response);

      var reloaded = test.Factory.LoadBranch(branch.Id);
      Assert.Present(reloaded);
    }
  }

  //-----------------------------------------------------------------------------------------------
}