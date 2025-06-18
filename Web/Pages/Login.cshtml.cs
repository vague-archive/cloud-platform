namespace Void.Platform.Web;

[AllowAnonymous]
public class LoginPage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  private const string CLI = "cli";
  private const string JWT = "jwt";

  //-----------------------------------------------------------------------------------------------

  private Config Config { get; init; }
  private ILogger Logger { get; init; }
  private OAuth.Providers oauthProviders { get; init; }
  private IHttpClientFactory httpClientFactory { get; init; }
  private Crypto.JwtGenerator jwtGenerator { get; init; }
  private IHostEnvironment Environment { get; init; }

  public LoginPage(Config config, ILogger logger, OAuth.Providers oauthProviders, IHttpClientFactory httpClientFactory, Crypto.JwtGenerator jwtGenerator, IHostEnvironment env)
  {
    this.Config = config;
    this.Logger = logger;
    this.oauthProviders = oauthProviders;
    this.httpClientFactory = httpClientFactory;
    this.jwtGenerator = jwtGenerator;
    this.Environment = env;
    this.Command = new Account.LoginCommand();
  }

  //-----------------------------------------------------------------------------------------------

  [BindProperty]
  public Account.LoginCommand Command { get; set; }

  [BindProperty]
  public string? Message { get; set; }

  //-----------------------------------------------------------------------------------------------

  public string? Origin { get; set; }

  public bool Cli
  {
    get
    {
      return Request.Query.ContainsKey(CLI);
    }
  }

  //-----------------------------------------------------------------------------------------------

  private enum Mode
  {
    ChooseProvider,
    UsePassword,
  }

  private Mode PageMode { get; set; } = Mode.ChooseProvider;

  public bool ChooseProviderMode
  {
    get
    {
      return PageMode == Mode.ChooseProvider;
    }
  }

  public bool UsePasswordMode
  {
    get
    {
      return PageMode == Mode.UsePassword;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public bool HasEmailError { get { return Current.Page.HasError("Command.Email"); } }
  public bool HasPasswordError { get { return Current.Page.HasError("Command.Password"); } }
  public bool AutoFocusEmail { get { return Current.Page.HasNoErrors || HasEmailError; } }
  public bool AutoFocusPassword { get { return !HasEmailError && HasPasswordError; } }
  public bool CanSocialAuth { get { return CanGitHubAuth || CanDiscordAuth; } }
  public bool CanGitHubAuth { get { return oauthProviders.HasGitHub; } }
  public bool CanDiscordAuth { get { return oauthProviders.HasDiscord; } }
  public bool CanPasswordAuth { get { return Config.Enable.PasswordLogin; } }
  public bool HasMessage { get { return Message is not null; } }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet(string? provider, string? origin)
  {
    if (Current.Principal.IsLoggedIn)
      return LoginRedirect(origin, Cli);

    if (provider is null)
      return OnGetPage(Mode.ChooseProvider, origin);
    else if (provider == "password")
      return OnGetPage(Mode.UsePassword, origin);
    else
      return OnGetOAuthChallenge(provider, origin);
  }

  public async Task<IActionResult> OnGetCallback(string provider)
  {
    return await OnGetOAuthCallback(provider);
  }

  //-----------------------------------------------------------------------------------------------

  private IActionResult OnGetPage(Mode mode, string? origin)
  {
    PageMode = mode;
    Command = new Account.LoginCommand();
    Message = Current.Flash.GetString("message");
    Origin = origin;
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  private IActionResult OnGetOAuthChallenge(string provider, string? origin)
  {
    var oauth = oauthProviders.Get(provider);
    if (oauth is null)
      return NotFound();
    var callbackUrl = Url.LoginCallbackUrl(oauth.Provider);
    var (url, verifier, state) = oauth.Challenge(callbackUrl);
    Current.Flash.Set("verifier", verifier);
    Current.Flash.Set("state", state);
    Current.Flash.Set("origin", origin);
    Current.Flash.Set("cli", Cli);
    return new RedirectResult(url.ToString());
  }

  //-----------------------------------------------------------------------------------------------

  private async Task<IActionResult> OnGetOAuthCallback(string provider)
  {
    var oauth = oauthProviders.Get(provider);
    if (oauth is null)
      return NotFound();

    var callbackUrl = Url.LoginCallbackUrl(oauth.Provider);
    var verifier = Current.Flash.GetString("verifier");
    var state = Current.Flash.GetString("state");
    var origin = Current.Flash.GetString("origin");
    var cli = Current.Flash.GetBool("cli");
    var result = await oauth.Callback(callbackUrl, verifier, state, HttpContext.Request, httpClientFactory.CreateClient());
    if (result.Failed)
      return await FailCallback(result);

    var identity = result.Value;
    Logger.Information("[LOGIN] attempt by {identity}", Json.Serialize(identity));
    var user = App.Account.GetAuthenticatedUser(identity.Provider, identity.Identifier);
    if (user is null)
      return await FailCallback($"{identity.Provider} user @{identity.UserName} is not registered to use this application");

    return await OnLoginSuccess(user, origin, cli);
  }

  private async Task<IActionResult> FailCallback<T>(Result<T> result)
  {
    return await FailCallback(result.Error.Format());
  }

  private async Task<IActionResult> FailCallback(string message)
  {
    Logger.Warning("[LOGIN] failed - {message}", message);
    Current.Flash.Set("message", message);
    await Authentication.Logout(HttpContext);
    return Redirect(Url.LoginPage());
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<IActionResult> OnPostAsync(string? origin)
  {
    PageMode = Mode.UsePassword;
    Origin = origin;

    if (!CanPasswordAuth)
    {
      Current.Page.Invalidate("Command.Password", "is not enabled at this time");
      return Page();
    }

    var result = App.Account.Login(Command);
    if (result.Failed)
    {
      Current.Page.Invalidate(result, nameof(Command));
      return Page();
    }

    return await OnLoginSuccess(result.Value, Origin, Cli);
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<IActionResult> OnPostLogout()
  {
    Current.Flash.Set("message", "You have been logged out");
    await Authentication.Logout(HttpContext);
    return Redirect(Url.LoginPage());
  }

  //-----------------------------------------------------------------------------------------------

  private async Task<IActionResult> OnLoginSuccess(Account.AuthenticatedUser user, string? origin, bool cli)
  {
    await HttpContext.Login(user);
    return LoginRedirect(origin, cli);
  }

  private IActionResult LoginRedirect(string? origin, bool cli)
  {
    if (cli == true && origin is not null && Http.IsAbsoluteUrl(origin))
    {
      var jwt = jwtGenerator.Create(Current.Principal.Claims);
      var url = Http.WithParam(origin, JWT, jwt);
      return Redirect(url.ToString());
    }
    else if (origin is not null)
    {
      return Redirect(origin);
    }
    else
    {
      return Redirect(Url.HomePage());
    }
  }

  //-----------------------------------------------------------------------------------------------
}