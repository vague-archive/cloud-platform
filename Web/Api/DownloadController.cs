namespace Void.Platform.Web.Api;

[Route("api/download")]
public class DownloadController : ApiController
{
  //-----------------------------------------------------------------------------------------------

  private Application App { get; init; }

  public DownloadController(Application app)
  {
    App = app;
  }

  //-----------------------------------------------------------------------------------------------

  [AllowAnonymous]
  [HttpGet("{repo}/{assetId}")]
  public async Task<IActionResult> DownloadReleaseAsset(string repo, long assetId)
  {
    var result = await App.Downloads.Download(repo, assetId);
    if (result.Failed)
      return BadRequest(result.Error.Format());

    Response.ContentLength = result.Value.ContentLength;
    return File(result.Value.Stream, result.Value.ContentType, result.Value.Asset.Name);
  }

  //-----------------------------------------------------------------------------------------------
}