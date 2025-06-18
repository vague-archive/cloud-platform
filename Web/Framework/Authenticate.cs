namespace Void.Platform.Web;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

//=================================================================================================
//
// We register 2 authentication schemes:
//  * Config.CookieAuthenticationScheme = "cookie"
//  * Config.TokenAuthenticationScheme  = "token"
//
// The handler for each scheme is provided by:
//  * "cookie" - CookieAuthenticationHandler (provided by dotnet framework)
//  * "token"  - TokenAuthenticationHandler  (implemented by us below)
//
//=================================================================================================

public static class Authentication
{
  public static IServiceCollection AddVoidAuthentication(this IServiceCollection services, Config config)
  {
    var authBuilder = services.AddAuthentication();
    AddCookieAuthentication(authBuilder, services, config);
    AddTokenAuthentication(authBuilder, services, config);
    AddUserPrincipal(services);
    return services;
  }

  private static void AddCookieAuthentication(AuthenticationBuilder authBuilder, IServiceCollection services, Config config)
  {
    authBuilder
      .AddCookie(authenticationScheme: Config.CookieAuthenticationScheme, options =>
      {
        options.Cookie.Name = config.Web.AuthCookie.Name;
        options.Cookie.Path = config.Web.AuthCookie.Path;
        options.Cookie.HttpOnly = config.Web.AuthCookie.HttpOnly;
        options.Cookie.SecurePolicy = config.Web.AuthCookie.Secure;
        options.Cookie.SameSite = config.Web.AuthCookie.SameSite;
        options.Cookie.MaxAge = config.Web.AuthCookie.MaxAge;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = config.Web.AuthCookie.MaxAge;
        options.SlidingExpiration = false;
        options.ReturnUrlParameter = "origin";
        options.LoginPath = "/login";
        options.EventsType = typeof(CustomCookieAuthenticationEvents);
      });
    services.AddScoped<CustomCookieAuthenticationEvents>();
  }

  private static void AddTokenAuthentication(AuthenticationBuilder authBuilder, IServiceCollection services, Config config)
  {
    authBuilder
      .AddScheme<TokenAuthenticationOptions, TokenAuthenticationHandler>(Config.TokenAuthenticationScheme, options =>
      {
      });
  }

  private static void AddUserPrincipal(IServiceCollection services)
  {
    services.AddTransient<IClaimsTransformation, UserPrincipalTransformer>();
  }

  public static async Task<UserPrincipal> Login(this HttpContext context, Account.AuthenticatedUser user, bool resetSession = true, string scheme = Config.CookieAuthenticationScheme)
  {
    var principal = UserPrincipal.From(user, scheme);
    await context.SignInAsync(scheme, principal);
    context.ResetSession(resetSession);
    var current = context.RequestServices.GetService<Current>();
    if (current is not null)
      current.Principal = principal;
    return principal;
  }

  public static async Task Logout(this HttpContext context, string scheme = Config.CookieAuthenticationScheme)
  {
    await context.SignOutAsync(scheme);
    context.ResetSession();
  }

  public static void ResetSession(this HttpContext context, bool resetSession = true)
  {
    var sessionsEnabled = context.Features.Get<ISessionFeature>()?.Session?.IsAvailable ?? false;
    if (sessionsEnabled && context.Session.IsAvailable && resetSession)
    {
      context.Session.Clear();
    }
  }
}

//=================================================================================================
// COOKIE AUTHENTICATION EVENT HANDLERS
//=================================================================================================

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
  //-----------------------------------------------------------------------------------------------

  public static readonly Duration RevalidateClaimsAfter = Duration.FromMinutes(10);

  //-----------------------------------------------------------------------------------------------

  private Application App { get; init; }
  private ILogger Logger { get; init; }
  private IClock Clock { get; init; }
  public string Scheme { get; init; } = Config.CookieAuthenticationScheme;

  //-----------------------------------------------------------------------------------------------

  public CustomCookieAuthenticationEvents(Application app, ILogger logger, IClock clock)
  {
    App = app;
    Logger = logger;
    Clock = clock;
  }

  //-----------------------------------------------------------------------------------------------

  public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
  {
    if (context.Request.IsHtmx())
    {
      // authn failed for anonymous htmx request, return 404 not found
      context.Response.StatusCode = Http.StatusCode.NotFound;
      return Task.CompletedTask;
    }
    else
    {
      // authn failed for anonymous full page request, redirect to login
      return base.RedirectToLogin(context);
    }
  }

  public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
  {
    // authn failed for authenticated user, return 404 not found (for both full page and htmx requests)
    context.Response.StatusCode = Http.StatusCode.NotFound;
    return Task.CompletedTask;
  }

  //-----------------------------------------------------------------------------------------------

  public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
  {
    if (context.Principal is null)
      return;

    if (!context.Principal.HasClaim(c => c.Type == UserClaim.SID))
    {
      Logger.Debug("no authentication claims to validate");
      await Reject(context);
      return;
    }

    var principal = context.Principal.Wrap();

    if (AuthenticatedRecently(principal, out var since))
    {
      Logger.Debug("skipped authentication claims validation because only {since} minutes have passed since {authenticatedOn} (NOW: {now})", Math.Round(since.TotalMinutes, 1), principal.AuthenticatedOn, Clock.Now);
    }
    else
    {
      var revalidatedUser = App.Account.GetAuthenticatedUser(principal.Id);
      if (revalidatedUser is null)
      {
        Logger.Warning("rejected authentication claims for {userId} {email} because user was not found (or is disabled)", principal.Id, principal.Email);
        await Reject(context);
      }
      else
      {
        Logger.Information("revalidated authentication claims for {userId} {email} {authenticatedOn}", revalidatedUser.Id, revalidatedUser.Email, revalidatedUser.AuthenticatedOn);
        await Replace(context, revalidatedUser);
      }
    }

    await Task.CompletedTask;
  }

  //-----------------------------------------------------------------------------------------------

  private async Task Reject(CookieValidatePrincipalContext context)
  {
    context.RejectPrincipal();
    await Authentication.Logout(context.HttpContext);
  }

  private async Task Replace(CookieValidatePrincipalContext context, Account.AuthenticatedUser user)
  {
    var principal = await context.HttpContext.Login(user, scheme: Scheme);
    context.ReplacePrincipal(principal);
  }

  private bool AuthenticatedRecently(UserPrincipal principal, out Duration since)
  {
    since = Clock.Now - principal.AuthenticatedOn;
    return since < RevalidateClaimsAfter;
  }

  //-----------------------------------------------------------------------------------------------
}

//=================================================================================================
// TOKEN AUTHENTICATION
//=================================================================================================

public class TokenAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class TokenAuthenticationHandler : AuthenticationHandler<TokenAuthenticationOptions>
{
  private const string BearerPrefix = "Bearer ";
  private Application App { get; init; }

  public TokenAuthenticationHandler(
    IOptionsMonitor<TokenAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    Application app
  ) : base(options, logger, encoder)
  {
    App = app;
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    if (!Request.Headers.ContainsKey(Http.Header.Authorization))
      return Task.FromResult(AuthenticateResult.NoResult());

    var header = Request.Headers[Http.Header.Authorization].ToString();
    if (!header.StartsWith(BearerPrefix))
      return Task.FromResult(AuthenticateResult.Fail("Invalid authorization header"));

    var token = header.Substring(BearerPrefix.Length).Trim();
    var user = AuthenticateToken(token);
    if (user is null)
      return Task.FromResult(AuthenticateResult.Fail("Invalid token"));

    var principal = UserPrincipal.From(user, Config.TokenAuthenticationScheme);
    var ticket = new AuthenticationTicket(principal, Config.TokenAuthenticationScheme);

    return Task.FromResult(AuthenticateResult.Success(ticket));
  }

  // expose protected method for unit testing
  public async Task<AuthenticateResult> TestHandleAuthenticateAsync()
  {
    return await HandleAuthenticateAsync();
  }

  //-----------------------------------------------------------------------------------------------

  private Account.AuthenticatedUser? AuthenticateToken(string token)
  {
    if (Crypto.LooksLikeJwt(token))
    {
      var claims = App.JwtGenerator.Verify(token);
      if (claims is null)
        return null;

      var claim = claims.Find(c => c.Type == UserClaim.Id);
      if (claim is null)
        return null;

      if (long.TryParse(claim.Value, out var id) == false)
        return null;

      return App.Account.GetAuthenticatedUser(id);
    }
    else
    {
      return App.Account.GetAuthenticatedUser(token);
    }
  }

  //-----------------------------------------------------------------------------------------------
}