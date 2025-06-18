namespace Void.Platform.Web.Api;

[LoadCurrent]
[LoadCurrentGame]
[Route("api/{org}/{game}/share")]
public class ShareController : ApiController
{
  //-----------------------------------------------------------------------------------------------

  private ILogger Logger { get; init; }
  private IClock Clock { get; init; }
  private IRandom Random { get; init; }
  private Current Current { get; init; }
  private Application App { get; init; }
  private new UrlGenerator Url { get; init; }

  public ShareController(
    ILogger logger,
    IClock clock,
    IRandom random,
    Current current,
    Application app,
    UrlGenerator url)
  {
    Logger = logger;
    Clock = clock;
    Random = random;
    Current = current;
    App = app;
    Url = url;
  }

  //-----------------------------------------------------------------------------------------------

  [HttpPost("{label?}")]
  [MaxRequestBodySize(2L * 1024 * 1024 * 1024)] // 2 GB
  public async Task<IActionResult> Upload()
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

    var result = await App.Share.FullDeploy(new Share.FullDeployCommand
    {
      Archive = Request.Body,
      DeployedBy = user,
      Organization = org,
      Game = game,
      Slug = slug,
      Password = password,
    });

    if (result.Failed)
      return Problem(result.Error.Format());

    var deploy = result.Value;
    var branch = RuntimeAssert.Present(deploy.Branch);
    return Ok(new
    {
      Id = deploy.Id,
      Slug = branch.Slug,
      Url = Url.ServeGame(org, game, branch, true)
    });
  }

  //-----------------------------------------------------------------------------------------------
}