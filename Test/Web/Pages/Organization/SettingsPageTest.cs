namespace Void.Platform.Web;

public class OrganizationSettingsPageTest : TestCase
{
  const string SettingsPath = "/atari/settings";

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get(SettingsPath);
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/atari/settings",
          expectedTitle: "Settings - Atari");
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageUnknownOrg()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/unknown/settings");
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
      var response = await test.Get(SettingsPath);
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
      var response = await test.Get(SettingsPath);
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fatari%2Fsettings", location);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestPost()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");

      var org = test.Factory.LoadOrganization("atari");
      Assert.Equal("Atari", org.Name);
      Assert.Equal("atari", org.Slug);

      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Command.Name", "New Name"},
        {"Command.Slug", "new-slug"}
      });

      var response = await test.HxPost($"/{org.Slug}/settings", formData);
      var location = Assert.Http.Redirect(response);
      Assert.Equal("/new-slug/settings", location);

      response = await test.Get(location);
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/new-slug/settings",
          expectedTitle: "Settings - New Name");

      var reloaded = test.Factory.LoadOrganization("atari");
      Assert.Equal("New Name", reloaded.Name);
      Assert.Equal("new-slug", reloaded.Slug);
    }
  }

  //-----------------------------------------------------------------------------------------------
}