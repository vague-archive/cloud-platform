namespace Void.Platform.Web;

[AllowAnonymous]
public class JoinProviderPage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  private OAuth.Providers oauthProviders { get; init; }
  private IHttpClientFactory httpClientFactory { get; init; }

  public JoinProviderPage(OAuth.Providers oauthProviders, IHttpClientFactory httpClientFactory)
  {
    this.oauthProviders = oauthProviders;
    this.httpClientFactory = httpClientFactory;
  }

  //-----------------------------------------------------------------------------------------------

  [BindProperty]
  public required string Token { get; set; }

  [BindProperty]
  public required string TimeZone { get; set; }

  [BindProperty]
  public required string Locale { get; set; }

  public IActionResult OnPostSignup(string provider)
  {
    var oauth = oauthProviders.Get(provider);
    if (oauth is null)
      return NotFound();

    var (url, verifier, state) = oauth.Challenge(Url.JoinCallbackUrl(oauth.Provider));
    Current.Flash.Set("verifier", verifier);
    Current.Flash.Set("state", state);
    Current.Flash.Set("token", Token);
    Current.Flash.Set("timezone", TimeZone);
    Current.Flash.Set("locale", Locale);
    return Response.Htmx().Redirect(url);
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<IActionResult> OnGetCallback(string provider)
  {
    var oauth = oauthProviders.Get(provider);
    var token = Current.Flash.GetString("token");
    var verifier = Current.Flash.GetString("verifier");
    var state = Current.Flash.GetString("state");
    var timezone = Current.Flash.GetString("timezone");
    var locale = Current.Flash.GetString("locale");

    if (oauth is null || token is null || verifier is null || state is null)
      return NotFound();

    var identity = await oauth.Callback(Url.JoinCallbackUrl(oauth.Provider), verifier, state, HttpContext.Request, httpClientFactory.CreateClient());
    if (identity.Failed)
      return await Fail(token, identity.Error.Format());

    var invite = App.Account.GetInvite(token);
    if (invite is null)
      return await Fail(token, "Invitation is no longer available");

    var org = App.Account.GetOrganization(invite.OrganizationId!.Value);
    if (org is null)
      return await Fail(token, "Organization is no longer available");

    var user = App.Account.AcceptInviteForNewUser(invite, new Account.AcceptInviteCommand
    {
      Provider = identity.Value.Provider,
      Identifier = identity.Value.Identifier,
      UserName = identity.Value.UserName,
      FullName = identity.Value.FullName,
      TimeZone = timezone ?? International.DefaultTimeZone,
      Locale = locale ?? International.DefaultLocale,
    });
    if (user.Failed)
      return await Fail(token, user.Error.Format());

    await HttpContext.Login(user.Value);
    return Redirect(Url.OrganizationPage(org));
  }

  public async Task<IActionResult> Fail(string token, string message)
  {
    Current.Flash.Set("message", message);
    await Authentication.Logout(HttpContext);
    return Redirect(Url.JoinPage(token));
  }

  //-----------------------------------------------------------------------------------------------
}