namespace Void.Platform.Web;

public class ProfilePage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  [BindProperty]
  public required Account.UpdateProfileCommand UpdateProfile { get; set; }

  //-----------------------------------------------------------------------------------------------

  public required List<Account.Token> AccessTokens { get; set; }
  public required Account.Token? GeneratedToken { get; set; }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet()
  {
    BuildUpdateProfileCommand();
    LoadAccessTokens();
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<IActionResult> OnPostUpdateProfile()
  {
    var user = App.Account.GetUserById(Current.Principal.Id);
    RuntimeAssert.Present(user);

    var result = App.Account.UpdateProfile(user, UpdateProfile);
    if (result.Failed)
    {
      Current.Page.Invalidate(result, nameof(UpdateProfile));
      return Partial("Profile/MyProfile", this);
    }

    // since updated values are also stored in principal claims in the
    // cookie we need to replace the principal in a way that ensures
    // the claims in the cookie are updated, and do a full page reload

    var authenticatedUser = App.Account.GetAuthenticatedUser(user.Id)!;
    await HttpContext.Login(authenticatedUser, resetSession: false);

    return Response
      .Htmx()
      .Refresh();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostCancelUpdateProfile()
  {
    BuildUpdateProfileCommand();
    return Partial("Profile/MyProfile", this);
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostGenerateAccessToken()
  {
    LoadAccessTokens();
    GeneratedToken = App.Account.GenerateAccessToken(Current.Principal.Id);
    return Partial("Profile/AccessTokens", this);
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostRevokeAccessToken(long tokenId)
  {
    LoadAccessTokens();
    var token = AccessTokens.Where(t => t.Id == tokenId).FirstOrDefault();
    if (token is not null)
    {
      App.Account.RevokeAccessToken(token);
    }
    AccessTokens = AccessTokens.Where(t => t.Id != tokenId).ToList();
    return Partial("Profile/AccessTokens", this);
  }

  //-----------------------------------------------------------------------------------------------

  private void BuildUpdateProfileCommand()
  {
    UpdateProfile = new Account.UpdateProfileCommand
    {
      Name = Current.Principal.Name,
      TimeZone = Current.Principal.TimeZone,
      Locale = Current.Principal.Locale,
    };
  }

  private void LoadAccessTokens()
  {
    AccessTokens = App.Account.GetAccessTokens(Current.Principal.Id);
    GeneratedToken = null;
  }

  //-----------------------------------------------------------------------------------------------
}