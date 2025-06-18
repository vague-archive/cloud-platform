namespace Void.Platform.Web;

[LoadCurrentOrganization]
public class OrganizationSettingsPage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  [BindProperty]
  public required Account.UpdateOrganizationCommand Command { get; set; }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet()
  {
    Command = new Account.UpdateOrganizationCommand
    {
      Name = Current.Organization.Name,
      Slug = Current.Organization.Slug,
    };
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGetCancel()
  {
    Command = new Account.UpdateOrganizationCommand
    {
      Name = Current.Organization.Name,
      Slug = Current.Organization.Slug,
    };
    return Partial("Settings/Form", this);
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<IActionResult> OnPost()
  {
    var result = App.Account.UpdateOrganization(Current.Organization, Command);
    if (result.Failed)
    {
      Current.Page.Invalidate(result, nameof(Command));
      return Partial("Settings/Form", this);
    }

    // since updated organization slug is also stored in principal claims in the
    // cookie we need to replace the principal in a way that ensures
    // the claims in the cookie are updated, and do a full page redirect using the new slug

    var authenticatedUser = App.Account.GetAuthenticatedUser(Current.Principal.Id)!;
    await HttpContext.Login(authenticatedUser, resetSession: false);

    var redirectUrl = Url.SettingsPage(result.Value);
    RuntimeAssert.Present(redirectUrl);

    return Response
      .Htmx()
      .Redirect(redirectUrl);
  }

  //-----------------------------------------------------------------------------------------------
}