namespace Void.Platform.Web;

using Microsoft.AspNetCore.Routing;

public class UrlGeneratorTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  public const string TestScheme = "https";
  public const string TestHost = "example.com";
  public const string FileName = "/assets/script.js";

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUrlGenerator()
  {
    using (var test = new DomainTest(this))
    {
      var provider = new MockUrlProvider(Fake);
      var url = new UrlGenerator(provider);
      var user = test.Factory.BuildUser();
      var org = test.Factory.BuildOrganization(slug: "my-org");
      var game = test.Factory.BuildGame(org);
      var branch = test.Factory.BuildBranch(game);
      var deploy = test.Factory.BuildDeploy(branch, user);
      var token = Fake.Token();

      var expected = provider.ExpectsPage(pageName: "/Home");
      Assert.Equal(expected, url.HomePage());

      expected = provider.ExpectsPage(
        pageName: "/Home",
        scheme: TestScheme,
        host: TestHost);
      Assert.Equal(expected, url.HomePage(full: true));

      expected = provider.ExpectsPage(
        pageName: "/Downloads",
        values: new { });
      Assert.Equal(expected, url.DownloadsPage());

      expected = provider.ExpectsPage(
        pageName: "/Profile");
      Assert.Equal(expected, url.ProfilePage());

      expected = provider.ExpectsPage(
        pageName: "/Login",
        values: new { });
      Assert.Equal(expected, url.LoginPage());

      expected = provider.ExpectsPage(
        pageName: "/Login",
        values: new { origin = "/origin", cli = "true" });
      Assert.Equal(expected, url.LoginPage("/origin", true));

      expected = provider.ExpectsPage(
        pageName: "/Login",
        values: new { origin = "/origin" });
      Assert.Equal(expected, url.LoginPage("/origin", false));

      expected = provider.ExpectsPage(
        pageName: "/Login",
        values: new { provider = "github", origin = "/origin", cli = true });
      Assert.Equal(expected, url.GitHubLoginPage("/origin", true));

      expected = provider.ExpectsPage(
        pageName: "/Login",
        values: new { provider = "discord", origin = "/origin" });
      Assert.Equal(expected, url.DiscordLoginPage("/origin"));

      expected = provider.ExpectsPage(
        pageName: "/Organization",
        values: new { org = org.Slug });
      Assert.Equal(expected, url.OrganizationPage(org));

      expected = provider.ExpectsPage(
        pageName: "/Organization/Games",
        values: new { org = org.Slug });
      Assert.Equal(expected, url.GamesPage(org));

      expected = provider.ExpectsPage(
        pageName: "/Organization/Settings",
        values: new { org = org.Slug });
      Assert.Equal(expected, url.SettingsPage(org));

      expected = provider.ExpectsPage(
        pageName: "/Game/Share",
        values: new { org = org.Slug, game = game.Slug });
      Assert.Equal(expected, url.SharePage(org, game));

      expected = provider.ExpectsAction(
        action: "Index",
        controller: "ServeGame",
        values: new { org = org.Slug, game = game.Slug, slug = branch.Slug });
      Assert.Equal(expected + "/", url.ServeGame(org, game, branch));

      expected = provider.ExpectsAction(
        action: "Password",
        controller: "ServeGame",
        values: new { org = org.Slug, game = game.Slug, slug = branch.Slug });
      Assert.Equal(expected, url.ServeGamePassword(org, game, branch));

      expected = provider.ExpectsPage(
        pageName: "/Game/Settings",
        values: new { org = org.Slug, game = game.Slug });
      Assert.Equal(expected, url.SettingsPage(org, game));

      expected = provider.ExpectsAction(
        action: "Index",
        controller: "SysAdmin");
      Assert.Equal(expected, url.SysAdminPage());

      expected = provider.ExpectsAction(
        action: "DownloadMissingFile",
        controller: "SysAdmin",
        values: new { file = "example.txt" });
      Assert.Equal(expected, url.SysAdminDownloadMissingFile("example.txt"));

      expected = provider.ExpectsAction(
        action: "UploadMissingFile",
        controller: "SysAdmin",
        values: new { file = "example.txt" });
      Assert.Equal(expected, url.SysAdminUploadMissingFile("example.txt"));

      expected = provider.ExpectsAction(
        action: "CacheDelete",
        controller: "SysAdmin",
        values: new { key = "sample:key" });
      Assert.Equal(expected, url.SysAdminCacheDelete("sample:key"));

      expected = provider.ExpectsAction(
        action: "TrashDelete",
        controller: "SysAdmin",
        values: new { key = "sample:key" });
      Assert.Equal(expected, url.SysAdminTrashDelete("sample:key"));

      expected = provider.ExpectsAction(
        action: "DeleteExpiredBranch",
        controller: "SysAdmin",
        values: new { branchId = branch.Id });
      Assert.Equal(expected, url.SysAdminDeleteExpiredBranch(branch));

      expected = provider.ExpectsPage(
        pageName: "/Join",
        values: new { token });
      Assert.Equal(expected, url.JoinPage(token));

      expected = provider.ExpectsPage(
        pageName: "/Join",
        values: new { token },
        scheme: TestScheme,
        host: TestHost);
      Assert.Equal(expected, url.JoinPage(token, full: true));

      expected = provider.ExpectsPage(
        pageName: "/Login",
        pageHandler: "callback",
        values: new { provider = "github" },
        scheme: TestScheme,
        host: TestHost);
      Assert.Equal(expected, url.LoginCallbackUrl(Account.IdentityProvider.GitHub));

      expected = provider.ExpectsPage(
        pageName: "/Join/Provider",
        pageHandler: "callback",
        values: new { provider = "github" },
        scheme: TestScheme,
        host: TestHost);
      Assert.Equal(expected, url.JoinCallbackUrl(Account.IdentityProvider.GitHub));

      expected = provider.ExpectsContent(fileName: FileName);
      Assert.Equal(expected, url.Content(FileName));
    }
  }

  //===============================================================================================
  // MOCK URL PROVIDER
  //===============================================================================================

  private class MockUrlProvider : IUrlProvider
  {
    private record ExpectedRoute
    {
      public required string Name { get; init; }
      public object? Values { get; init; }
      public string? Scheme { get; init; }
      public string? Host { get; init; }
      public required string Result { get; init; }
    }

    private record ExpectedAction
    {
      public required string Action { get; init; }
      public required string Controller { get; init; }
      public object? Values { get; init; }
      public string? Scheme { get; init; }
      public string? Host { get; init; }
      public required string Result { get; init; }
    }

    private record ExpectedPage
    {
      public required string PageName { get; init; }
      public string? PageHandler { get; init; }
      public object? Values { get; init; }
      public string? Scheme { get; init; }
      public string? Host { get; init; }
      public required string Result { get; init; }
    }

    private record ExpectedContent
    {
      public required string FileName { get; init; }
      public required string Result { get; init; }
    }

    private Queue<object> Expected;
    private Fake Fake;

    public MockUrlProvider(Fake fake)
    {
      Expected = new Queue<object>();
      Fake = fake;
    }

    public string Scheme { get { return TestScheme; } }
    public string HostName { get { return TestHost; } }

    public string Route(string routeName, RouteValueDictionary? values, string? scheme, string? host)
    {
      var ex = Expected.Dequeue() as ExpectedRoute;
      Assert.Present(ex);
      Assert.Equal(ex.Name, routeName);
      Assert.Equivalent(ex.Values, values);
      Assert.Equal(ex.Scheme, scheme);
      Assert.Equal(ex.Host, host);
      return ex.Result;
    }

    public string Page(string pageName, string? pageHandler, RouteValueDictionary? values, string? scheme, string? host)
    {
      var ex = Expected.Dequeue() as ExpectedPage;
      Assert.Present(ex);
      Assert.Equal(ex.PageName, pageName);
      Assert.Equal(ex.PageHandler, pageHandler);
      Assert.Equivalent(ex.Values, values);
      Assert.Equal(ex.Scheme, scheme);
      Assert.Equal(ex.Host, host);
      return ex.Result;
    }

    public string Action(string action, string? controller, RouteValueDictionary? values, string? scheme, string? host)
    {
      var ex = Expected.Dequeue() as ExpectedAction;
      Assert.Present(ex);
      Assert.Equal(ex.Action, action);
      Assert.Equal(ex.Controller, controller);
      Assert.Equivalent(ex.Values, values);
      Assert.Equal(ex.Scheme, scheme);
      Assert.Equal(ex.Host, host);
      return ex.Result;
    }

    public string Content(string fileName)
    {
      var ex = Expected.Dequeue() as ExpectedContent;
      Assert.Present(ex);
      Assert.Equal(ex.FileName, fileName);
      return ex.Result;
    }

    public string ExpectsRoute(string routeName, object? values = null, string? scheme = null, string? host = null)
    {
      var result = Fake.Url();
      Expected.Enqueue(new ExpectedRoute
      {
        Name = routeName,
        Values = values is null ? null : new RouteValueDictionary(values),
        Scheme = scheme,
        Host = host,
        Result = result,
      });
      return result;
    }

    public string ExpectsPage(string pageName, string? pageHandler = null, object? values = null, string? scheme = null, string? host = null)
    {
      var result = Fake.Url();
      Expected.Enqueue(new ExpectedPage
      {
        PageName = pageName,
        PageHandler = pageHandler,
        Values = values is null ? null : new RouteValueDictionary(values),
        Scheme = scheme,
        Host = host,
        Result = result,
      });
      return result;
    }

    public string ExpectsAction(string action, string controller, object? values = null, string? scheme = null, string? host = null)
    {
      var result = Fake.Url();
      Expected.Enqueue(new ExpectedAction
      {
        Action = action,
        Controller = controller,
        Values = values is null ? null : new RouteValueDictionary(values),
        Scheme = scheme,
        Host = host,
        Result = result,
      });
      return result;
    }

    public string ExpectsContent(string fileName)
    {
      var result = Fake.Url();
      Expected.Enqueue(new ExpectedContent
      {
        FileName = fileName,
        Result = result,
      });
      return result;
    }
  }

  //-----------------------------------------------------------------------------------------------
}