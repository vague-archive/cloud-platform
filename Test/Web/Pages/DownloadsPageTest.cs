namespace Void.Platform.Web;

public class DownloadsPageTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDownloadsPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      Clock.Freeze(2025, 2, 6, user.TimeZone);

      var editorCacheKey = CacheKey.ForDownloads("editor");

      var fiasco1a = test.Factory.BuildGitHubReleaseAsset(id: 111, name: "Fiasco-1.0.0.Setup.exe");
      var fiasco2a = test.Factory.BuildGitHubReleaseAsset(id: 222, name: "Fiasco-2.0.0.Setup.exe");
      var fiasco3a = test.Factory.BuildGitHubReleaseAsset(id: 333, name: "Fiasco-3.0.0.Setup.exe");
      var fiasco1 = test.Factory.BuildGitHubRelease(id: 1, name: "Fiasco", tagName: "1.0", assets: [fiasco1a]);
      var fiasco2 = test.Factory.BuildGitHubRelease(id: 2, name: "Fiasco", tagName: "2.0", assets: [fiasco2a]);
      var fiasco3 = test.Factory.BuildGitHubRelease(id: 3, name: "Fiasco", tagName: "3.0", preRelease: true, assets: [fiasco3a]);

      await test.SeedCache(editorCacheKey, new List<GitHub.Release> { fiasco3, fiasco2, fiasco1 });

      var response = await test.Get("/downloads");
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/downloads",
          expectedTitle: "Downloads");

      var editorCard = Assert.Html.Select("[qa=editor]", doc);
      var stableTitle = Assert.Html.Select("[qa=stable-title]", editorCard);
      var canaryTitle = Assert.Html.Select("[qa=canary-title]", editorCard);
      var stablePublishedOn = Assert.Html.Select("[qa=stable-published-on]", editorCard);
      var canaryPublishedOn = Assert.Html.Select("[qa=canary-published-on]", editorCard);
      var stableLinks = Assert.Html.SelectAll("[qa=stable-links] a", editorCard);
      var canaryLinks = Assert.Html.SelectAll("[qa=canary-links] a", editorCard);

      Assert.Equal("Stable (2.0)", stableTitle.TextContent);
      Assert.Equal("Feb 6, 2025", stablePublishedOn.TextContent);

      Assert.Equal("Canary (3.0)", canaryTitle.TextContent);
      Assert.Equal("Feb 6, 2025", canaryPublishedOn.TextContent);

      Assert.Single(stableLinks);
      Assert.Equal("Windows", stableLinks[0].TextContent.Trim());
      Assert.Equal("/downloads/editor/222", stableLinks[0].GetAttribute("href"));

      Assert.Single(canaryLinks);
      Assert.Equal("Windows", canaryLinks[0].TextContent.Trim());
      Assert.Equal("/downloads/editor/333", canaryLinks[0].GetAttribute("href"));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDownloadsPageAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Get("/downloads");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fdownloads", location);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDownloadAsset()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");

      var assetContent = "yolo";
      var asset = test.Factory.BuildGitHubReleaseAsset(id: 123, name: "fiasco_darwin_arm64.zip");

      await test.SeedCache(CacheKey.ForDownloads("editor"), new List<GitHub.Release> { test.Factory.BuildGitHubRelease(
        id: 1,
        name: "editor stuff",
        tagName: "2.0",
        assets: [asset]
      ) });

      var handler = new MockHttpMessageHandler();
      test.HttpHandler.When(asset.Url)
        .WithHeaders("Authorization", $"Bearer {TestConfig.GitHubApiToken}")
        .WithHeaders("Accept", Http.ContentType.Bytes)
        .WithHeaders("User-Agent", GitHub.UserAgent)
        .WithHeaders(GitHub.VersionHeader, GitHub.Version)
        .Respond("text/plain", assetContent);

      var response = await test.Get($"/downloads/editor/{asset.Id}");
      var content = Assert.Http.Ok(response);
      Assert.Equal(assetContent, content);
    }
  }

  //-----------------------------------------------------------------------------------------------
}