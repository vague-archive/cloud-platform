namespace Void.Platform.Web.Api;

[LoadCurrent]
[LoadCurrentGame]
[Route("api/{org}/{game}/deploy")]
public class DeployController : ApiController
{
  //-----------------------------------------------------------------------------------------------

  private IRandom Random { get; init; }
  private Current Current { get; init; }
  private Application App { get; init; }
  private new UrlGenerator Url { get; init; }

  public DeployController(IRandom random, Current current, Application app, UrlGenerator url)
  {
    Random = random;
    Current = current;
    App = app;
    Url = url;
  }

  //-----------------------------------------------------------------------------------------------

  [HttpPost("{label?}")]
  public async Task<IActionResult> Start([FromBody] Share.DeployAsset[] manifest)
  {
    var org = Current.Organization;
    var game = Current.Game;
    var user = Current.User;
    var password = HttpContext.GetHeader(Http.Header.XDeployPassword);

    var label = Request.GetHeader(Http.Header.XDeployLabel)
      ?? Request.GetRouteValue("label") as string
      ?? Request.GetQueryParam("label")
      ?? Random.Identifier();

    var slug = Format.Slugify(label);

    var result = await App.Share.IncrementalDeploy(new Share.IncrementalDeployCommand
    {
      Manifest = manifest,
      DeployedBy = user,
      Organization = org,
      Game = game,
      Slug = slug,
      Password = password,
    });

    if (result.Failed)
      return Problem(result.Error.Format());

    var (deploy, missing) = result.Value;

    Response.Headers.Append(Http.Header.XDeployId, deploy.Id.ToString());
    return Accepted(missing);
  }

  //-----------------------------------------------------------------------------------------------

  [HttpPost("{deployId}/upload/{*asset}")]
  public async Task<IActionResult> Upload(long deployId, string asset)
  {
    var org = Current.Organization;
    var game = Current.Game;
    var deploy = App.Share.GetDeploy(deployId);
    if (deploy is null || deploy.GameId != Current.Game.Id)
      return NotFound();

    var contentType = Http.DeriveContentType(asset);

    var result = await App.Share.UploadDeployAsset(new Share.UploadDeployAssetCommand
    {
      Organization = org,
      Game = game,
      Deploy = deploy,
      Content = Request.Body,
      ContentType = contentType,
    });
    if (result.Failed)
      return Problem(result.Error.Format());

    var (exists, blob) = result.Value;
    return Ok(blob);
  }

  //-----------------------------------------------------------------------------------------------

  [HttpPost("{deployId}/activate")]
  public async Task<IActionResult> Activate(long deployId)
  {
    var org = Current.Organization;
    var game = Current.Game;
    var deploy = App.Share.GetDeploy(deployId);
    if (deploy is null || deploy.GameId != game.Id)
      return NotFound();

    var branch = App.Share.GetBranch(deploy.BranchId);
    if (branch is null)
      return NotFound();

    // TODO: REMOVE THIS - IT IS A TEMPORARY PERFORMANCE TUNING OPTION
    var concurrency = Request.Query.TryGetValue("concurrency", out var str) && int.TryParse(str, out var cc) ? cc : 8;

    var result = await App.Share.ActivateIncrementalDeploy(new Share.ActivateIncrementalDeployCommand
    {
      Organization = org,
      Game = game,
      Branch = branch,
      Deploy = deploy,
      Concurrency = concurrency,
    });
    if (result.Failed)
      return Problem(result.Error.Format());

    return Ok(new
    {
      DeployId = deploy.Id,
      Slug = branch.Slug,
      Url = Url.ServeGame(org, game, branch, true)
    });
  }

  //-----------------------------------------------------------------------------------------------
}