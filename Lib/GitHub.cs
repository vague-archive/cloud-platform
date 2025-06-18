namespace Void.Platform.Lib;

using System.Net.Http.Headers;
using System.Text.Json;

public class GitHub
{
  //-----------------------------------------------------------------------------------------------

  public static readonly Uri Endpoint = new Uri("https://api.github.com");
  public const string Version = "2022-11-28";
  public const string VersionHeader = "X-Github-Api-Version";
  public const string Accept = "application/vnd.github+json";
  public const string UserAgent = "void-platform";
  public const string Owner = "vaguevoid";

  //-----------------------------------------------------------------------------------------------

  private HttpClient client;
  private string token;

  public GitHub(HttpClient client, string token)
  {
    this.client = client;
    this.token = token;
  }

  //===============================================================================================
  // API
  //===============================================================================================

  public record User
  {
    public long Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
  }

  public async Task<User> GetCurrentUser()
  {
    using var json = Json.Parse(await Get("/user"));
    var id = json.RequiredLong("id");
    var login = json.RequiredString("login");
    var name = json.OptionalString("name") ?? login;
    var email = json.OptionalString("email");
    return new User
    {
      Id = id,
      Login = login,
      Name = name,
      Email = email,
    };
  }

  //-----------------------------------------------------------------------------------------------

  private async Task<string> Get(string path)
  {
    var url = new Uri(Endpoint, path);
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Accept));
    request.Headers.UserAgent.ParseAdd(UserAgent);
    request.Headers.Add(VersionHeader, Version);

    var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
    RuntimeAssert.True(response.Content.Headers.ContentType?.MediaType == "application/json");
    var content = await response.Content.ReadAsStringAsync();
    return content;
  }

  //===============================================================================================
  // RELEASES
  //===============================================================================================

  public enum ReleasePlatform
  {
    AppleArm,
    AppleIntel,
    Windows,
    LinuxArm,
    LinuxIntel,
    Unknown,
  }

  public record Release
  {
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required string TagName { get; set; }
    public required bool PreRelease { get; set; }
    public required bool Draft { get; set; }
    public required Instant PublishedOn { get; set; }
    public required string Body { get; set; }
    public required List<ReleaseAsset> Assets { get; set; }
  }

  public record ReleaseAsset
  {
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required string ContentType { get; set; }
    public required int ContentLength { get; set; }
    public required string Url { get; set; }
    public required ReleasePlatform Platform { get; set; }
  }

  public record ReleaseAssetDownload
  {
    public required ReleaseAsset Asset { get; init; }
    public required Stream Stream { get; init; }
    public required string ContentType { get; init; }
    public long? ContentLength { get; init; }
  }

  //-----------------------------------------------------------------------------------------------

  public string GetReleasesUrl(string repo)
    => $"/repos/{Owner}/{repo}/releases";

  public async Task<List<Release>> GetReleases(string repo)
  {
    using var json = Json.Parse(await Get(GetReleasesUrl(repo)));
    JsonElement root = json.RootElement;
    var releases = new List<Release>();
    foreach (JsonElement element in json.RootElement.EnumerateArray())
    {
      var name = element.OptionalString("name");
      var tagName = element.RequiredString("tag_name");
      var release = new Release
      {
        Id = element.RequiredLong("id"),
        Name = name ?? tagName,
        TagName = tagName,
        Draft = element.OptionalBool("draft", false),
        PreRelease = element.OptionalBool("prerelease", false),
        PublishedOn = element.RequiredInstant("published_at"),
        Body = element.OptionalString("body") ?? "",
        Assets = new List<ReleaseAsset>(),
      };
      if (release.Draft)
        continue;
      foreach (JsonElement asset in element.GetProperty("assets").EnumerateArray())
      {
        var assetName = asset.RequiredString("name");
        release.Assets.Add(new ReleaseAsset
        {
          Id = asset.RequiredLong("id"),
          Name = assetName,
          ContentType = asset.RequiredString("content_type"),
          ContentLength = asset.RequiredInt("size"),
          Url = asset.RequiredString("url"),
          Platform = IdentifyReleasePlatform(assetName),
        });
      }
      releases.Add(release);
    }
    return releases.OrderByDescending(r => r.PublishedOn).ToList();
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<Result<ReleaseAssetDownload>> Download(ReleaseAsset asset)
  {
    var url = new Uri(Endpoint, asset.Url);
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Http.ContentType.Bytes));
    request.Headers.UserAgent.ParseAdd(UserAgent);
    request.Headers.Add(VersionHeader, Version);

    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? Http.ContentType.Bytes;
    var contentLength = response.Content.Headers.ContentLength;
    var stream = await response.Content.ReadAsStreamAsync();

    return Result.Ok(new ReleaseAssetDownload
    {
      Asset = asset,
      Stream = stream,
      ContentType = contentType,
      ContentLength = contentLength,
    });
  }

  //-----------------------------------------------------------------------------------------------

  public static ReleaseAsset? FindAsset(List<Release> releases, long assetId)
  {
    foreach (var release in releases)
      foreach (var asset in release.Assets)
        if (asset.Id == assetId)
          return asset;
    return null;
  }

  public static ReleasePlatform IdentifyReleasePlatform(string name)
  {
    if (name.Contains("darwin"))
    {
      if (name.Contains("aarch64") || name.Contains("arm64"))
        return ReleasePlatform.AppleArm;
      else if (name.Contains("x64") || name.Contains("x86"))
        return ReleasePlatform.AppleIntel;
    }
    else if (name.Contains("linux"))
    {
      if (name.Contains("arm64"))
        return ReleasePlatform.LinuxArm;
      else if (name.Contains("x64") || name.Contains("x86"))
        return ReleasePlatform.LinuxIntel;
    }
    else if (
      name.EndsWith(".msi") ||
      name.EndsWith(".exe") ||
      name.Contains("windows") ||
      name.Contains("win32")
    )
    {
      return ReleasePlatform.Windows;
    }
    return ReleasePlatform.Unknown;
  }

  //-----------------------------------------------------------------------------------------------
}