namespace Void.Platform.Domain;

public class DownloadsTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  static long ASSET_ID = 123;
  static string REPO = "editor";
  static string CACHE_KEY = CacheKey.ForDownloads(REPO);

  //===============================================================================================
  // TEST GetReleases()
  //===============================================================================================

  [Fact]
  public async Task TestGetReleasesFromGitHubApi()
  {
    using (var test = new DomainTest(this))
    {
      var asset1a = test.Factory.BuildGitHubReleaseAsset(id: 11, name: "11-darwin-arm64.zip");
      var asset1b = test.Factory.BuildGitHubReleaseAsset(id: 12, name: "12-darwin-x86.zip");
      var asset2a = test.Factory.BuildGitHubReleaseAsset(id: 21, name: "21-setup.exe");
      var asset3a = test.Factory.BuildGitHubReleaseAsset(id: 31, name: "31-setup.exe");

      var oldest = test.Factory.BuildGitHubRelease(id: 1, name: "oldest", tagName: "v1.0", assets: [asset1a, asset1b]);
      var latest = test.Factory.BuildGitHubRelease(id: 2, name: "latest", tagName: "v2.0", assets: [asset2a]);
      var prerelease = test.Factory.BuildGitHubRelease(id: 3, name: "prerelease", tagName: "v3.0", preRelease: true, assets: [asset3a]);
      var draft = test.Factory.BuildGitHubRelease(id: 4, name: "draft", tagName: "v4.0", draft: true, assets: []);

      Assert.Domain.CacheAbsent(test.Cache, CACHE_KEY);

      test.HttpHandler.When("https://api.github.com/repos/vaguevoid/editor/releases")
        .Respond("application/json", Json.Serialize(new[]{
          draft.AsGitHub(),
          prerelease.AsGitHub(),
          latest.AsGitHub(),
          oldest.AsGitHub(),
        }));

      var result = await test.App.Downloads.GetReleases(REPO);
      Assert.Succeeded(result);

      var releases = result.Value;
      Assert.Equal(3, releases.Count);

      Assert.Domain.Equal(prerelease, releases[0]);
      Assert.Domain.Equal(latest, releases[1]);
      Assert.Domain.Equal(oldest, releases[2]);

      var cached = Assert.Domain.CachePresent<List<GitHub.Release>>(test.Cache, CACHE_KEY);
      Assert.Equal(releases, cached);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetReleasesFromCache()
  {
    using (var test = new DomainTest(this))
    {
      var asset1a = test.Factory.BuildGitHubReleaseAsset(id: 11, name: "11-darwin-arm64.zip");
      var asset1b = test.Factory.BuildGitHubReleaseAsset(id: 12, name: "12-darwin-x86.zip");
      var asset2a = test.Factory.BuildGitHubReleaseAsset(id: 21, name: "21-setup.exe");

      var oldest = test.Factory.BuildGitHubRelease(id: 1, name: "oldest", tagName: "v1.0", assets: [asset1a, asset1b]);
      var latest = test.Factory.BuildGitHubRelease(id: 2, name: "latest", tagName: "v2.0", assets: [asset2a]);

      await test.SeedCache(CACHE_KEY, new List<GitHub.Release> { latest, oldest });

      var result = await test.App.Downloads.GetReleases(REPO);
      Assert.Succeeded(result);

      var releases = result.Value;
      Assert.Equal(2, releases.Count);

      Assert.Domain.Equal(latest, releases[0]);
      Assert.Domain.Equal(oldest, releases[1]);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetReleasesWithRefresh()
  {
    using (var test = new DomainTest(this))
    {
      var asset1a = test.Factory.BuildGitHubReleaseAsset(id: 11, name: "11-darwin-arm64.zip");
      var asset1b = test.Factory.BuildGitHubReleaseAsset(id: 12, name: "12-darwin-x86.zip");
      var asset2a = test.Factory.BuildGitHubReleaseAsset(id: 21, name: "21-setup.exe");
      var asset3a = test.Factory.BuildGitHubReleaseAsset(id: 31, name: "31-setup.exe");

      var oldest = test.Factory.BuildGitHubRelease(id: 1, name: "oldest", tagName: "v1.0", assets: [asset1a, asset1b]);
      var latest = test.Factory.BuildGitHubRelease(id: 2, name: "latest", tagName: "v2.0", assets: [asset2a]);
      var cached = test.Factory.BuildGitHubRelease(id: 3, name: "cached", tagName: "v3.0", assets: [asset3a]);

      await test.SeedCache(CACHE_KEY, new List<GitHub.Release> { cached });

      var result = await test.App.Downloads.GetReleases(REPO);
      Assert.Succeeded(result);
      Assert.Single(result.Value);
      Assert.Domain.Equal(cached, result.Value[0]);

      test.HttpHandler.When("https://api.github.com/repos/vaguevoid/editor/releases")
        .Respond("application/json", Json.Serialize(new[]{
          latest.AsGitHub(),
          oldest.AsGitHub(),
        }));

      result = await test.App.Downloads.GetReleases(REPO, refresh: true);
      Assert.Succeeded(result);
      Assert.Equal(2, result.Value.Count);
      Assert.Domain.Equal(latest, result.Value[0]);
      Assert.Domain.Equal(oldest, result.Value[1]);

      // verify refreshed result was cached
      var recached = test.Cache.GetOrDefault<List<GitHub.Release>>(CACHE_KEY);
      Assert.Equal(result.Value, recached);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetReleasesWithoutGitHubApiToken()
  {
    using (var test = new DomainTest(this))
    {
      test.App.Config.GitHubApiToken = null;
      var result = await test.App.Downloads.GetReleases(REPO);
      Assert.Failed(result);
      Assert.Equal("github integration is disabled", result.Error.Format());
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetReleasesFromUnsafeRepo()
  {
    using (var test = new DomainTest(this))
    {
      var result = await test.App.Downloads.GetReleases("engine");
      Assert.Failed(result);
      Assert.Equal("engine repository is not allowed", result.Error.Format());
    }
  }

  //===============================================================================================
  // TEST Download()
  //===============================================================================================

  [Fact]
  public async Task TestDownloadReleaseAsset()
  {
    using (var test = new DomainTest(this))
    {
      var asset = test.Factory.BuildGitHubReleaseAsset(id: ASSET_ID, name: "fiasco-darwin-arm64.zip");
      var release = test.Factory.BuildGitHubRelease(id: 1, name: "release", tagName: "v1.0", assets: [asset]);

      await test.SeedCache(CACHE_KEY, new List<GitHub.Release> { release });

      var content = "yolo";
      var contentType = Http.ContentType.TextUtf8;
      var contentLength = content.Count();

      test.HttpHandler
        .When(asset.Url)
        .Respond("text/plain", content);

      var result = await test.App.Downloads.Download(REPO, ASSET_ID);
      Assert.Succeeded(result);
      Assert.Equal(asset, result.Value.Asset);
      Assert.Equal(contentType, result.Value.ContentType);
      Assert.Equal(contentLength, result.Value.ContentLength);

      using StreamReader reader = new StreamReader(result.Value.Stream);
      Assert.Equal(content, reader.ReadToEnd());
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDownloadReleaseAssetCouldNotBeFound()
  {
    using (var test = new DomainTest(this))
    {
      var release = test.Factory.BuildGitHubRelease(id: 1, name: "release", tagName: "v1.0", assets: []);
      await test.SeedCache(CACHE_KEY, new List<GitHub.Release> { release });
      var result = await test.App.Downloads.Download(REPO, ASSET_ID);
      Assert.Failed(result);
      Assert.Equal($"editor repository could not find asset {ASSET_ID}", result.Error.Format());
    }
  }

  [Fact]
  public async Task TestDownloadReleaseAssetIsNotDownloadable()
  {
    using (var test = new DomainTest(this))
    {
      var asset = test.Factory.BuildGitHubReleaseAsset(id: ASSET_ID, name: "virus.exe");
      var release = test.Factory.BuildGitHubRelease(id: 1, name: "latest", tagName: "v1.0", assets: [asset]);
      await test.SeedCache(CACHE_KEY, new List<GitHub.Release> { release });
      var result = await test.App.Downloads.Download(REPO, ASSET_ID);
      Assert.Failed(result);
      Assert.Equal("virus.exe is not downloadable", result.Error.Format());
    }
  }

  //===============================================================================================
  // TEST MISC HELPERS
  //===============================================================================================

  [Fact]
  public void TestSafeRepositories()
  {
    Assert.True(Downloads.IsSafeRepo("editor"));
    Assert.False(Downloads.IsSafeRepo("jam"));
    Assert.False(Downloads.IsSafeRepo("engine"));
    Assert.False(Downloads.IsSafeRepo("platform"));
    Assert.False(Downloads.IsSafeRepo("tetris"));
    Assert.False(Downloads.IsSafeRepo("snakes"));
    Assert.False(Downloads.IsSafeRepo("other"));
    Assert.False(Downloads.IsSafeRepo("secret"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestIsDownloadableFileName()
  {
    // fiasco editor names
    Assert.True(Downloads.IsDownloadable("Fiasco-0.0.80.Setup.exe"));
    Assert.True(Downloads.IsDownloadable("Fiasco-darwin-arm64-0.0.80.zip"));
    Assert.True(Downloads.IsDownloadable("Fiasco-darwin-x64-0.0.80.zip"));

    // jam names
    Assert.False(Downloads.IsDownloadable("jam_darwin_arm64.zip"));
    Assert.False(Downloads.IsDownloadable("jam_darwin_intelx86.zip"));
    Assert.False(Downloads.IsDownloadable("jam_linux_arm64.zip"));
    Assert.False(Downloads.IsDownloadable("jam_linux_intelx86.zip"));
    Assert.False(Downloads.IsDownloadable("jam_ui_darwin_arm64.zip"));
    Assert.False(Downloads.IsDownloadable("jam_ui_windows_intelx86.zip"));
    Assert.False(Downloads.IsDownloadable("jam_vscode_extension.zip"));
    Assert.False(Downloads.IsDownloadable("jam_windows_intelx86.zip"));

    // unknown
    Assert.False(Downloads.IsDownloadable("unknown_darwin_arm64.zip"));
    Assert.False(Downloads.IsDownloadable("unknown_darwin_intelx86.zip"));
    Assert.False(Downloads.IsDownloadable("unknown_linux_arm64.zip"));
    Assert.False(Downloads.IsDownloadable("unknown_linux_intelx86.zip"));
    Assert.False(Downloads.IsDownloadable("unknown_ui_darwin_arm64.zip"));
    Assert.False(Downloads.IsDownloadable("unknown_ui_windows_intelx86.zip"));
    Assert.False(Downloads.IsDownloadable("unknown_vscode_extension.zip"));
    Assert.False(Downloads.IsDownloadable("unknown_windows_intelx86.zip"));
    Assert.False(Downloads.IsDownloadable("Source code (zip)"));
    Assert.False(Downloads.IsDownloadable("Source code (tar.gz)"));
    Assert.False(Downloads.IsDownloadable("unknown.exe"));
    Assert.False(Downloads.IsDownloadable("unknown.zip"));
    Assert.False(Downloads.IsDownloadable("unknown.zip"));
    Assert.False(Downloads.IsDownloadable(""));
  }

  //-----------------------------------------------------------------------------------------------
}