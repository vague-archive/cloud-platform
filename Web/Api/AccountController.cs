namespace Void.Platform.Web.Api;

[LoadCurrent]
[Route("api/account")]
public class AccountController : ApiController
{
  //-----------------------------------------------------------------------------------------------

  private Application App { get; init; }
  private Current Current { get; init; }
  private ILogger Logger { get; init; }

  public AccountController(Application app, Current current, ILogger logger)
  {
    App = app;
    Current = current;
    Logger = logger;
  }

  //-----------------------------------------------------------------------------------------------

  [Route("me")]
  [AcceptVerbs("OPTIONS")]
  [AllowAnonymous]
  public IActionResult MePreflight()
  {
    return NoContent();
  }

  [Route("me")]
  [AcceptVerbs("GET", "HEAD")]
  public IActionResult Me()
  {
    if (Current.HasUser)
      return Json(Current.User);
    else
      return NotFound();
  }

  //-----------------------------------------------------------------------------------------------
}