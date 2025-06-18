namespace Void.Platform.Web;

public class ServeGameTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  private const string ASSET = "path/to/asset.txt";
  private const string CONTENT = "Hello World";
  private const string PASSWORD = "You shall not pass!";

  //===============================================================================================
  // TEST SERVE INDEX PAGE
  //===============================================================================================

  [Fact]
  public async Task TestServeGameIndexEnforcesTrailingSlashRedirect()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Get("/org/game/serve/deploy"); // NOTE: NO TRAILING SLASH
      var location = Assert.Http.Redirect(response);
      Assert.Equal("/org/game/serve/deploy/", location); // NOTE: REDIRECT URL HAS A TRAILING SLASH
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestServeGameIndex()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();

      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game);
      var deploy = test.Factory.CreateDeploy(branch, user);

      await test.FileStore.SaveTestFile(Path.Combine(deploy.Path, "index.html"), @"
        <!DOCTYPE html>
        <html>
        <head>
          <title>My Game</title>
          <meta name=""foo"" content=""bar""></meta>
          <script src=""game.js""></script>
        </head>
        <body>
          <canvas id=""game""></canvas>
        </body>
        </html>
      ");

      var response = await test.Get($"/{org.Slug}/{game.Slug}/serve/{branch.Slug}/");
      var doc = Assert.Html.Document(response);

      Assert.Http.HasHeader(Http.CacheControl.MustRevalidateStrict, Http.Header.CacheControl, response);
      Assert.Http.HasHeader(Clock.Now.ToRfc9110(), Http.Header.LastModified, response);

      Assert.Http.HasNoHeader(Http.Header.CrossOriginOpenerPolicy, response);
      Assert.Http.HasNoHeader(Http.Header.CrossOriginEmbedderPolicy, response);
      Assert.Http.HasNoHeader(Http.Header.CrossOriginResourcePolicy, response);
      Assert.Http.HasNoHeader(Http.Header.AccessControlAllowOrigin, response);

      var title = Assert.Html.Select("head title", doc);
      var canvas = Assert.Html.Select("body canvas", doc);
      var meta = Assert.Html.SelectAll("head meta", doc);
      var script = Assert.Html.SelectAll("head script", doc);

      Assert.Equal(5, meta.Count);
      Assert.Equal("foo", meta[0].GetAttribute("name"));
      Assert.Equal("void:platform:csrf", meta[1].GetAttribute("name"));
      Assert.Equal("void:platform:organization", meta[2].GetAttribute("name"));
      Assert.Equal("void:platform:game", meta[3].GetAttribute("name"));
      Assert.Equal("void:platform:branch", meta[4].GetAttribute("name"));

      Assert.Equal(2, script.Count);
      Assert.Equal("game.js", script[0].GetAttribute("src"));
      Assert.StartsWith("/serve.js", script[1].GetAttribute("src"));

      Assert.Equal("bar", meta[0].GetAttribute("content"));
      Assert.Equal(KnownAntiForgery.Token, meta[1].GetAttribute("content"));
      Assert.Equal(org.Slug, meta[2].GetAttribute("content"));
      Assert.Equal(game.Slug, meta[3].GetAttribute("content"));
      Assert.Equal(branch.Slug, meta[4].GetAttribute("content"));

      Assert.Equal("My Game", title.TextContent);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestServeToolIndexIncludesCorsHeaders()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();

      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var tool = test.Factory.CreateGame(org, purpose: Account.GamePurpose.Tool);
      var branch = test.Factory.CreateBranch(tool);
      var deploy = test.Factory.CreateDeploy(branch, user);

      await test.FileStore.SaveTestFile(Path.Combine(deploy.Path, "index.html"), @"
        <!DOCTYPE html>
        <html>
        <head></head>
        <body></body>
        </html>
      ");

      var response = await test.Get($"/{org.Slug}/{tool.Slug}/serve/{branch.Slug}/");
      Assert.Html.Document(response);

      Assert.Http.HasHeader(Http.CacheControl.MustRevalidateStrict, Http.Header.CacheControl, response);
      Assert.Http.HasHeader(Clock.Now.ToRfc9110(), Http.Header.LastModified, response);

      Assert.Http.HasHeader(Http.CORS.SameOrigin, Http.Header.CrossOriginOpenerPolicy, response);
      Assert.Http.HasHeader(Http.CORS.RequireCorp, Http.Header.CrossOriginEmbedderPolicy, response);
      Assert.Http.HasHeader(Http.CORS.CrossOrigin, Http.Header.CrossOriginResourcePolicy, response);
      Assert.Http.HasHeader("*", Http.Header.AccessControlAllowOrigin, response);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestServeGameIndexUnknownGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var org = test.Factory.CreateOrganization();
      var response = await test.Get($"/{org.Slug}/unknown/serve/latest/");
      Assert.Http.NotFound(response);
    }
  }

  [Fact]
  public async Task TestServeGameIndexUnknownDeploy()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var response = await test.Get($"/{org.Slug}/{game.Slug}/serve/unknown/");
      Assert.Http.NotFound(response);
    }
  }

  [Fact]
  public async Task TestServeGameIndexNotFound()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game);
      var deploy = test.Factory.CreateDeploy(branch, user);
      // NOTE: did not create an index.html file
      var response = await test.Get($"/{org.Slug}/{game.Slug}/serve/{branch.Slug}/");
      Assert.Http.NotFound(response);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestServeGameWithPasswordProtection()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();

      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game, password: PASSWORD);
      var deploy = test.Factory.CreateDeploy(branch, user);

      Assert.Preconditions.True(branch.HasPassword);

      await test.FileStore.SaveTestFile(Path.Combine(deploy.Path, "index.html"), @"
        <!DOCTYPE html>
        <html>
        <head>
          <title>My Game</title>
        </head>
        <body>
        </body>
        </html>
      ");

      var response = await test.Get($"/{org.Slug}/{game.Slug}/serve/{branch.Slug}/");
      var location = Assert.Http.Redirect(response);
      Assert.Equal($"/{org.Slug}/{game.Slug}/serve/{branch.Slug}/password", location);

      response = await test.Get(location);
      var doc = Assert.Html.Document(response,
          expectedUrl: $"https://localhost{location}",
          expectedTitle: "Password Required");
      Assert.Html.Select("[qa=game-password]", doc);

      response = await test.HxPost($"/{org.Slug}/{game.Slug}/serve/{branch.Slug}/password", test.BuildForm(new Dictionary<string, string>
      {
        {"password", PASSWORD},
      }));
      location = Assert.Http.Redirect(response);
      Assert.Equal($"/{org.Slug}/{game.Slug}/serve/{branch.Slug}/", location);

      response = await test.Get(location);
      Assert.Html.Document(response,
        expectedUrl: $"https://localhost/{org.Slug}/{game.Slug}/serve/{branch.Slug}/",
        expectedTitle: "My Game");
    }
  }

  //===============================================================================================
  // TEST SERVE ASSETS
  //===============================================================================================

  [Fact]
  public async Task TestServeGameAsset()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();

      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game);
      var deploy = test.Factory.CreateDeploy(branch, user);

      await test.FileStore.SaveTestFile(Path.Combine(deploy.Path, ASSET), CONTENT);

      var response = await test.Get($"/{org.Slug}/{game.Slug}/serve/{branch.Slug}/{ASSET}");
      var content = Assert.Http.Ok(response);
      Assert.Equal(CONTENT, content);

      Assert.Http.HasHeader(Http.ContentType.Text, Http.Header.ContentType, response);
      Assert.Http.HasHeader(Http.CacheControl.MustRevalidateStrict, Http.Header.CacheControl, response);
      Assert.Http.HasHeader(Clock.Now.ToRfc9110(), Http.Header.LastModified, response);

      Assert.Http.HasNoHeader(Http.Header.CrossOriginOpenerPolicy, response);
      Assert.Http.HasNoHeader(Http.Header.CrossOriginEmbedderPolicy, response);
      Assert.Http.HasNoHeader(Http.Header.CrossOriginResourcePolicy, response);
      Assert.Http.HasNoHeader(Http.Header.AccessControlAllowOrigin, response);
    }
  }

  //-----------------------------------------------------------------------------------------------
}