namespace Void.Platform.Web.Api;

[Route("api")]
public class MonitorController : ApiController
{
  //-----------------------------------------------------------------------------------------------

  private ILogger Logger { get; init; }

  public MonitorController(ILogger logger)
  {
    Logger = logger;
  }

  //-----------------------------------------------------------------------------------------------

  [AllowAnonymous]
  [HttpGet("ping")]
  public IActionResult Ping()
  {
    return Json(new { ping = "pong" });
  }

  //-----------------------------------------------------------------------------------------------

  // TODO: work with Scarlett to update editor to use /api/account/me instead of api/auth/validate

  [AllowAnonymous]
  [Route("auth/validate")]
  [AcceptVerbs("OPTIONS")]
  public IActionResult ValidateOptions()
  {
    return NoContent();
  }

  [Route("auth/validate")]
  [AcceptVerbs("HEAD", "GET", "POST")]
  public IActionResult Validate()
  {
    return Ok("Valid Authorization Token");
  }

  //===============================================================================================
  // INTERNAL ENDPOINTS FOR INTEGRATION TESTING ERROR HANDLING (see ProgramTest.cs), SYSADMIN ONLY
  //===============================================================================================

  [Authorize(Policy.SysAdmin)]
  [HttpGet("test/ok")]
  public IActionResult TestOk()
  {
    return Ok("yolo");
  }

  [Authorize(Policy.SysAdmin)]
  [HttpGet("test/error")]
  public IActionResult TestError()
  {
    return Problem("uh oh");
  }

  [Authorize(Policy.SysAdmin)]
  [HttpGet("test/bad-request")]
  public IActionResult TestBadRequest()
  {
    return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>(){
      { "something", ["is invalid"]},
    }));
  }

  [Authorize(Policy.SysAdmin)]
  [HttpGet("test/crash")]
  public IActionResult TestCrash()
  {
    throw new Exception("bang! this is a test of an UNHANDLED exception from cloud-platform");
  }

  //-----------------------------------------------------------------------------------------------
}