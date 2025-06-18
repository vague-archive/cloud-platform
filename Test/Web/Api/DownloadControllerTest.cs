namespace Void.Platform.Web.Api;

public class DownloadControllerTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  public static readonly string REPO = "editor";
  public static readonly string CACHE_KEY = CacheKey.ForDownloads(REPO);

  //===============================================================================================
  // TEST DownloadReleaseAsset
  //===============================================================================================

  [Fact]
  public async Task TestDownloadReleaseAsset()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var assetContent = "abc";

      var asset1 = test.Factory.BuildGitHubReleaseAsset(id: 100, name: "fiasco_darwin_arm64.zip");
      var asset2 = test.Factory.BuildGitHubReleaseAsset(id: 200, name: "fiasco_linux_x86.zip");
      var asset3 = test.Factory.BuildGitHubReleaseAsset(id: 300, name: "fiasco_setup.exe");
      var oldest = test.Factory.BuildGitHubRelease(id: 1, name: "oldest", tagName: "v1.0", assets: []);
      var latest = test.Factory.BuildGitHubRelease(id: 2, name: "latest", tagName: "v2.0", assets: [asset1, asset2, asset3]);

      await test.SeedCache(CACHE_KEY, new List<GitHub.Release> { latest, oldest });

      test.HttpHandler.When(asset2.Url)
        .WithHeaders("Authorization", $"Bearer {TestConfig.GitHubApiToken}")
        .WithHeaders("Accept", Http.ContentType.Bytes)
        .WithHeaders("User-Agent", GitHub.UserAgent)
        .WithHeaders(GitHub.VersionHeader, GitHub.Version)
        .Respond("text/plain", assetContent);

      var response = await test.Get($"/api/download/editor/{asset2.Id}");
      var content = Assert.Http.Ok(response);
      Assert.Equal(assetContent, content);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDownloadReleaseAssetFailed()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Get("/api/download/secret/42");
      var error = Assert.Http.BadRequest(response);
      Assert.Equal("secret repository is not allowed", error);
    }
  }

  //-----------------------------------------------------------------------------------------------
}