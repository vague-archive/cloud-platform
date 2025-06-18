namespace Void.Platform.Web;

[LoadCurrentOrganization]
public class OrganizationHomePage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet()
  {
    return Redirect(Url.GamesPage(Current.Organization));
  }

  //-----------------------------------------------------------------------------------------------
}