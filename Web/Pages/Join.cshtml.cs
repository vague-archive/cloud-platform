namespace Void.Platform.Web;

[AllowAnonymous]
public class JoinPage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  private OAuth.Providers oauthProviders { get; init; }

  public JoinPage(OAuth.Providers oauthProviders)
  {
    this.oauthProviders = oauthProviders;
  }

  //-----------------------------------------------------------------------------------------------

  public required string Token { get; set; }
  public Account.Token? Invite { get; set; }
  public Account.Organization? Organization { get; set; }

  public bool Unavailable { get { return Invite is null || Organization is null; } }
  public bool CanSocialAuth { get { return CanGitHubAuth || CanDiscordAuth; } }
  public bool CanGitHubAuth { get { return oauthProviders.HasGitHub; } }
  public bool CanDiscordAuth { get { return oauthProviders.HasDiscord; } }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet(string token)
  {
    Token = token;
    Invite = App.Account.GetInvite(token);
    Organization = Invite?.OrganizationId is long orgId ? App.Account.GetOrganization(orgId) : null;
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<IActionResult> OnPostAccept(string token)
  {
    if (!Current.Principal.IsLoggedIn)
      return await FailAccept(token, "User must be logged in");

    var invite = App.Account.GetInvite(token);
    if (invite is null)
      return await FailAccept(token, "Invitation is no longer available");

    var result = App.Account.AcceptInviteForExistingUser(invite, Current.Principal.Id);
    if (result.Failed)
      return await FailAccept(token, result.Error.Format());

    var (user, org) = result.Value;
    await HttpContext.Login(user);
    return Response
      .Htmx()
      .Redirect(Url.OrganizationPage(org));
  }

  private async Task<IActionResult> FailAccept(string token, string message)
  {
    Current.Flash.Set("message", message);
    await Authentication.Logout(HttpContext);
    return Response
      .Htmx()
      .Redirect(Url.JoinPage(token));
  }

  //-----------------------------------------------------------------------------------------------
}