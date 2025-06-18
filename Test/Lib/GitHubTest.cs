namespace Void.Platform.Lib;

public class GitHubTest : TestCase
{
  //===============================================================================================
  // TEST GetCurrentUser()
  //===============================================================================================

  [Fact]
  public async Task TestGetCurrentUser()
  {
    var token = "SECRETZ";
    var handler = new MockHttpMessageHandler();
    handler.When("https://api.github.com/user")
      .WithHeaders("Authorization", $"Bearer {token}")
      .WithHeaders("Accept", GitHub.Accept)
      .WithHeaders("User-Agent", GitHub.UserAgent)
      .WithHeaders(GitHub.VersionHeader, GitHub.Version)
      .Respond("application/json", Json.Serialize(new
      {
        id = 42,
        login = "jakesgordon",
        name = "Jake Gordon",
      }));
    var client = handler.ToHttpClient();
    var api = new GitHub(client, token);

    var user = await api.GetCurrentUser();

    Assert.NotNull(user);
    Assert.Equal(42, user.Id);
    Assert.Equal("jakesgordon", user.Login);
    Assert.Equal("Jake Gordon", user.Name);
    Assert.Null(user.Email);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetCurrentUserWithEmail()
  {
    var token = "SECRETZ";
    var handler = new MockHttpMessageHandler();
    handler.When("https://api.github.com/user")
      .WithHeaders("Authorization", $"Bearer {token}")
      .WithHeaders("Accept", GitHub.Accept)
      .WithHeaders(GitHub.VersionHeader, GitHub.Version)
      .Respond("application/json", Json.Serialize(new
      {
        id = 42,
        login = "jakesgordon",
        name = "Jake Gordon",
        email = "jake@void.dev",
      }));
    var client = handler.ToHttpClient();
    var api = new GitHub(client, token);

    var user = await api.GetCurrentUser();

    Assert.NotNull(user);
    Assert.Equal(42, user.Id);
    Assert.Equal("jakesgordon", user.Login);
    Assert.Equal("Jake Gordon", user.Name);
    Assert.Equal("jake@void.dev", user.Email);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetCurrentUserWithNoName()
  {
    var token = "SECRETZ";
    var handler = new MockHttpMessageHandler();
    handler.When("https://api.github.com/user")
      .WithHeaders("Authorization", $"Bearer {token}")
      .WithHeaders("Accept", GitHub.Accept)
      .WithHeaders("User-Agent", GitHub.UserAgent)
      .WithHeaders(GitHub.VersionHeader, GitHub.Version)
      .Respond("application/json", Json.Serialize(new
      {
        id = 42,
        login = "jakesgordon",
      }));
    var client = handler.ToHttpClient();
    var api = new GitHub(client, token);

    var user = await api.GetCurrentUser();

    Assert.NotNull(user);
    Assert.Equal(42, user.Id);
    Assert.Equal("jakesgordon", user.Login);
    Assert.Equal("jakesgordon", user.Name);
    Assert.Null(user.Email);
  }

  //===============================================================================================
  // TEST Releases Utility Methods
  //===============================================================================================

  [Fact]
  public void TestIdentifyReleasePlatform()
  {
    // fiasco editor names
    Assert.Equal(GitHub.ReleasePlatform.Windows, GitHub.IdentifyReleasePlatform("Fiasco_windows-x86_64_0.0.80-setup.exe"));
    Assert.Equal(GitHub.ReleasePlatform.AppleArm, GitHub.IdentifyReleasePlatform("Fiasco_darwin-aarch64_0.0.80.app.zip"));
    Assert.Equal(GitHub.ReleasePlatform.AppleArm, GitHub.IdentifyReleasePlatform("Fiasco_darwin-arm64_0.0.80.app.zip"));
    Assert.Equal(GitHub.ReleasePlatform.AppleIntel, GitHub.IdentifyReleasePlatform("Fiasco_darwin-x86_64_0.0.80.zip"));

    // unknown
    Assert.Equal(GitHub.ReleasePlatform.Unknown, GitHub.IdentifyReleasePlatform("yolo"));
    Assert.Equal(GitHub.ReleasePlatform.Unknown, GitHub.IdentifyReleasePlatform(""));
  }

  //===============================================================================================
  // TEST GetReleases and FindAsset
  //===============================================================================================

  [Fact]
  public async Task TestGetReleasesAndFindAsset()
  {
    var token = "SECRETZ";
    var handler = new MockHttpMessageHandler();

    handler.When("https://api.github.com/repos/vaguevoid/editor/releases")
      .WithHeaders("Authorization", $"Bearer {token}")
      .WithHeaders("Accept", GitHub.Accept)
      .WithHeaders("User-Agent", GitHub.UserAgent)
      .WithHeaders(GitHub.VersionHeader, GitHub.Version)
      .Respond("application/json", Json.Serialize(new[]{
        new {
          id = 1,
          name = "oldest",
          tag_name = "v1.0",
          draft = false,
          prerelease = false,
          published_at = "2024-01-01T01:01:01Z",
          body = "oldest release",
          assets = new[]{
            new {
              id = 11,
              name = "11-darwin-arm64.zip",
              content_type = "application/zip",
              size = 11,
              url = "https://example.com/11-darwin-arm64.zip",
            },
            new {
              id = 12,
              name = "12-darwin-x86.zip",
              content_type = "application/zip",
              size = 12,
              url = "https://example.com/12-darwin-x86.zip",
            }
          }
        },
        new {
          id = 2,
          name = "latest",
          tag_name = "v2.0",
          draft = false,
          prerelease = true,
          published_at = "2024-02-02T02:02:02Z",
          body = "latest release",
          assets = new[]{
            new {
              id = 21,
              name = "21-setup.exe",
              content_type = "application/exe",
              size = 21,
              url = "https://example.com/21-setup.exe",
            }
          }
        },
        new {
          id = 3,
          name = "draft",
          tag_name = "draft",
          draft = true,
          prerelease = false,
          published_at = "2024-03-03T03:03:03Z",
          body = "draft release",
          assets = new[]{
            new {
              id = 31,
              name = "31-setup.exe",
              content_type = "application/exe",
              size = 31,
              url = "https://example.com/31-setup.exe",
            }
          }
        },
      }));

    var client = handler.ToHttpClient();
    var api = new GitHub(client, token);

    var releases = await api.GetReleases("editor");
    Assert.Equal(2, releases.Count);

    var r1 = releases[0];
    var r2 = releases[1];

    Assert.Equal(2, r1.Id);
    Assert.Equal("latest", r1.Name);
    Assert.Equal("v2.0", r1.TagName);
    Assert.True(r1.PreRelease);
    Assert.Equal("2024-02-02T02:02:02Z", r1.PublishedOn.ToIso8601());
    Assert.Equal("latest release", r1.Body);
    Assert.Single(r1.Assets);
    Assert.Equal(21, r1.Assets[0].Id);
    Assert.Equal("21-setup.exe", r1.Assets[0].Name);
    Assert.Equal("application/exe", r1.Assets[0].ContentType);
    Assert.Equal(21, r1.Assets[0].ContentLength);
    Assert.Equal("https://example.com/21-setup.exe", r1.Assets[0].Url);
    Assert.Equal(GitHub.ReleasePlatform.Windows, r1.Assets[0].Platform);

    Assert.Equal(1, r2.Id);
    Assert.Equal("oldest", r2.Name);
    Assert.Equal("v1.0", r2.TagName);
    Assert.False(r2.PreRelease);
    Assert.Equal("2024-01-01T01:01:01Z", r2.PublishedOn.ToIso8601());
    Assert.Equal("oldest release", r2.Body);
    Assert.Equal(2, r2.Assets.Count);
    Assert.Equal(11, r2.Assets[0].Id);
    Assert.Equal("11-darwin-arm64.zip", r2.Assets[0].Name);
    Assert.Equal("application/zip", r2.Assets[0].ContentType);
    Assert.Equal(11, r2.Assets[0].ContentLength);
    Assert.Equal("https://example.com/11-darwin-arm64.zip", r2.Assets[0].Url);
    Assert.Equal(GitHub.ReleasePlatform.AppleArm, r2.Assets[0].Platform);
    Assert.Equal(12, r2.Assets[1].Id);
    Assert.Equal("12-darwin-x86.zip", r2.Assets[1].Name);
    Assert.Equal("application/zip", r2.Assets[1].ContentType);
    Assert.Equal(12, r2.Assets[1].ContentLength);
    Assert.Equal("https://example.com/12-darwin-x86.zip", r2.Assets[1].Url);
    Assert.Equal(GitHub.ReleasePlatform.AppleIntel, r2.Assets[1].Platform);


    // also test FindAsset() since we have conveniently loaded a List<Release>
    Assert.Equal(r1.Assets[0], GitHub.FindAsset(releases, 21));
    Assert.Equal(r2.Assets[0], GitHub.FindAsset(releases, 11));
    Assert.Equal(r2.Assets[1], GitHub.FindAsset(releases, 12));
  }

  //===============================================================================================
  // TEST Download
  //===============================================================================================

  [Fact]
  public async Task TestDownloadAsset()
  {
    var asset = new GitHub.ReleaseAsset
    {
      Id = 1,
      Name = "editor-darwin-arm64.zip",
      ContentType = Http.ContentType.Bytes,
      ContentLength = 111,
      Url = "https://download.github.com/path/to/release/asset",
      Platform = GitHub.ReleasePlatform.AppleArm,
    };

    var content = "abc";
    var token = "SECRETZ";
    var handler = new MockHttpMessageHandler();
    handler.When(asset.Url)
      .WithHeaders("Authorization", $"Bearer {token}")
      .WithHeaders("Accept", Http.ContentType.Bytes)
      .WithHeaders("User-Agent", GitHub.UserAgent)
      .WithHeaders(GitHub.VersionHeader, GitHub.Version)
      .Respond("text/plain", content);

    var client = handler.ToHttpClient();
    var api = new GitHub(client, token);

    var result = await api.Download(asset);
    Assert.Succeeded(result);
    Assert.StartsWith("text/plain", result.Value.ContentType);
    Assert.Equal(content.Count(), result.Value.ContentLength);

    using StreamReader reader = new StreamReader(result.Value.Stream);
    Assert.Equal(content, reader.ReadToEnd());
  }

  //-----------------------------------------------------------------------------------------------
}