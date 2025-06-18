namespace Void.Platform.Web;

using System.Security.Claims;

[ApiController]
[ApiAccessControl]
[Authorize(Policy.TokenAuthenticated)]
public abstract class ApiController : ControllerBase
{
  //-----------------------------------------------------------------------------------------------

  private ApiSerializer Serializer { get; init; }

  public ApiController()
  {
    Serializer = new ApiSerializer();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult Json<T>(T value)
  {
    return Ok(Serializer.AsJson(value));
  }

  //-----------------------------------------------------------------------------------------------

  public new ClaimsPrincipal User
  {
    get
    {
      throw new NotImplementedException("use Current.Principal instead - you'll get our wrapper class");
    }
  }

  //-----------------------------------------------------------------------------------------------
}