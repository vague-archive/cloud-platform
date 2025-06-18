namespace Void.Platform.Web;

public class DownloadsPage : BasePage
{
  public List<GitHub.Release> EditorReleases { get; set; } = new List<GitHub.Release>();
  public GitHub.Release? LatestEditorStableRelease { get; set; }
  public GitHub.Release? LatestEditorCanaryRelease { get; set; }

  //-----------------------------------------------------------------------------------------------

  public async Task<IActionResult> OnGet(string? repo = null, long? assetId = null, bool refresh = false)
  {
    if (repo is not null && assetId is not null)
    {
      return await DownloadAsset(repo, assetId.Value);
    }
    else
    {
      return await ShowDownloadsPage(refresh);
    }
  }

  //-----------------------------------------------------------------------------------------------

  private async Task<IActionResult> ShowDownloadsPage(bool refresh)
  {
    await LoadReleases(refresh);
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  private async Task<IActionResult> DownloadAsset(string repo, long assetId)
  {
    var result = await App.Downloads.Download(repo, assetId);
    if (result.Failed)
      return Redirect(Url.DownloadsPage());

    Response.ContentLength = result.Value.ContentLength;
    return File(result.Value.Stream, result.Value.ContentType, result.Value.Asset.Name);
  }

  //-----------------------------------------------------------------------------------------------

  private async Task LoadReleases(bool refresh = false)
  {
    if (App.GitHubEnabled)
    {
      var editor = await App.Downloads.GetReleases("editor", refresh);
      EditorReleases = editor.Succeeded ? editor.Value : new List<GitHub.Release>();
      LatestEditorStableRelease = EditorReleases.Where(r => !r.PreRelease).FirstOrDefault();
      LatestEditorCanaryRelease = EditorReleases.Where(r => r.PreRelease).FirstOrDefault();
    }
  }

  //-----------------------------------------------------------------------------------------------

  public record ReleaseLinks
  {
    public required string Repo;
    public required GitHub.Release Release;
    public required List<GitHub.ReleaseAsset> Assets;
  }

  public ReleaseLinks? FindEditorLinks(GitHub.Release release)
  {
    var assets = release.Assets.Where(a => a.Platform != GitHub.ReleasePlatform.Unknown).ToList();
    if (assets.Count == 0)
      return null;
    return new ReleaseLinks
    {
      Repo = "editor",
      Release = release,
      Assets = assets,
    };
  }

  //-----------------------------------------------------------------------------------------------
}