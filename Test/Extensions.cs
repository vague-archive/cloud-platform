namespace Void.Platform.Test;

//
// various helper extensions that make tests easier to write
// but code that's not required at runtime
//
public static class TestExtensions
{
  //-----------------------------------------------------------------------------------------------

  internal static dynamic AsGitHub(this GitHub.Release release)
  {
    return new
    {
      id = release.Id,
      name = release.Name,
      tag_name = release.TagName,
      prerelease = release.PreRelease,
      draft = release.Draft,
      published_at = release.PublishedOn,
      body = release.Body,
      assets = release.Assets.Select(a => a.AsGitHub()).ToList(),
    };
  }

  internal static dynamic AsGitHub(this GitHub.ReleaseAsset asset)
  {
    return new
    {
      id = asset.Id,
      name = asset.Name,
      content_type = asset.ContentType,
      size = asset.ContentLength,
      url = asset.Url.ToString(),
    };
  }

  //-----------------------------------------------------------------------------------------------
}