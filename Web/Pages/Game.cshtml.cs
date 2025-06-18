namespace Void.Platform.Web;

[LoadCurrentGame]
public class GameHomePage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet()
  {
    return RedirectToPage("/Game/Share",
      routeValues: new
      {
        org = Current.Organization.Slug,
        game = Current.Game.Slug,
      }
    );
  }

  //-----------------------------------------------------------------------------------------------
}