namespace Void.Platform.Web;

public class HomePage : BasePage
{
  private List<Account.Organization>? orgs;

  public List<Account.Organization> Organizations
  {
    get
    {
      RuntimeAssert.Present(orgs);
      return orgs;
    }
  }

  public IActionResult OnGet()
  {
    if (Current.Principal.BelongsToSingleOrganization)
    {
      var org = App.Account.GetOrganization(Current.Principal.Organizations.First());
      RuntimeAssert.Present(org);
      return Redirect(Url.OrganizationPage(org));
    }
    else
    {
      orgs = App.Account.GetUserOrganizations(Current.Principal.Id);
      return Page();
    }
  }
}