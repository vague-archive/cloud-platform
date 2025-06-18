namespace Void.Platform.Web;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

//=================================================================================================
// TEST COOKIE AUTH HANDLER
//=================================================================================================

public class CookieAuthenticationTest : TestCase
{
  [Fact]
  public async Task TestValidatePrincipal()
  {
    using (var test = new DomainTest(this))
    {
      // this unit test verifies that if the principal was authenticated "recently"
      // then the claims are accepted and the user is considered logged in
      // without checking against the database

      var authenticatedOn = Clock.Now.Minus(CustomCookieAuthenticationEvents.RevalidateClaimsAfter).Plus(Duration.FromSeconds(1)); // STILL VALID - but only just
      var user = CreateAuthenticatedUser(test.Factory, authenticatedOn);
      var principal = UserPrincipal.From(user, TestConfig.AuthenticationScheme);
      var context = BuildCookieValidationContext(principal);
      var events = new CustomCookieAuthenticationEvents(test.App, Logger, Clock);

      await events.ValidatePrincipal(context);

      Assert.Equal(principal, context.Principal);
      AssertNeitherSignInNorSignOutCalled(context);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestValidatePrincipalRenewed()
  {
    using (var test = new DomainTest(this))
    {
      // this unit test verifies that if the principal was authenticated "NOT recently"
      // then the claims are revalidated against the database
      // - in this case the user is still valid and so a NEW PRINCIPAL IS GENERATED

      var authenticatedOn = Clock.Now.Minus(CustomCookieAuthenticationEvents.RevalidateClaimsAfter).Minus(Duration.FromSeconds(1)); // INVALID - but only just
      var user = CreateAuthenticatedUser(test.Factory, authenticatedOn);
      var principal = UserPrincipal.From(user, TestConfig.AuthenticationScheme);
      var context = BuildCookieValidationContext(principal);
      var events = new CustomCookieAuthenticationEvents(test.App, Logger, Clock);

      await events.ValidatePrincipal(context);

      Assert.NotNull(context.Principal);
      Assert.NotEqual(principal, context.Principal); // it got replaced

      var newPrincipal = context.Principal.Wrap();

      Assert.True(newPrincipal.IsLoggedIn);
      Assert.Equal(KnownUserId, newPrincipal.Id);
      Assert.Equal("John Doe", newPrincipal.Name);
      Assert.Equal("john@example.com", newPrincipal.Email);
      Assert.Equal("Europe/Paris", newPrincipal.TimeZone);
      Assert.Equal("en-US", newPrincipal.Locale);
      Assert.Empty(newPrincipal.Roles);
      Assert.Equal(["github:githubber"], newPrincipal.Identities);
      Assert.Equal([KnownOrganizationId], newPrincipal.Organizations);
      Assert.Equal(Clock.Now, newPrincipal.AuthenticatedOn); // we re-authenticated right now
      AssertSignInCalled(context);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestValidatePrincipalRejected()
  {
    using (var test = new DomainTest(this))
    {
      // this unit test verifies that if the principal was authenticated "NOT recently"
      // then the claims are revalidated against the database
      // - in this case the user has been disabled in the DB and so the claims are rejected

      var authenticatedOn = Clock.Now.Minus(CustomCookieAuthenticationEvents.RevalidateClaimsAfter).Minus(Duration.FromSeconds(1)); // INVALID - but only just
      var user = CreateAuthenticatedUser(test.Factory, authenticatedOn);
      var principal = UserPrincipal.From(user, TestConfig.AuthenticationScheme);
      var context = BuildCookieValidationContext(principal);
      var events = new CustomCookieAuthenticationEvents(test.App, Logger, Clock);

      test.Factory.DisableUser(user.Id);
      await events.ValidatePrincipal(context);
      Assert.Null(context.Principal);
      AssertSignOutCalled(context);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestValidatePrincipalMissingClaims()
  {
    using (var test = new DomainTest(this))
    {
      var claims = new List<Claim>();
      var principal = UserPrincipal.From(claims, TestConfig.AuthenticationScheme);
      var context = BuildCookieValidationContext(principal);
      var events = new CustomCookieAuthenticationEvents(test.App, Logger, Clock);

      await events.ValidatePrincipal(context);
      Assert.Null(context.Principal);
      AssertSignOutCalled(context);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestValidatePrincipalMissingPrincipal()
  {
    using (var test = new DomainTest(this))
    {
      var claims = new List<Claim>();
      var principal = UserPrincipal.From(claims, TestConfig.AuthenticationScheme);
      var context = BuildCookieValidationContext(principal);
      var events = new CustomCookieAuthenticationEvents(test.App, Logger, Clock);

      context.Principal = null;
      await events.ValidatePrincipal(context);
      Assert.Null(context.Principal);
      AssertNeitherSignInNorSignOutCalled(context);
    }
  }

  //-----------------------------------------------------------------------------------------------

  private const long KnownUserId = 42;
  private const long KnownOrganizationId = 100;

  private Account.AuthenticatedUser CreateAuthenticatedUser(FixtureFactory factory, Instant authenticatedOn)
  {
    var user = factory.CreateUser(id: KnownUserId, name: "John Doe", email: "john@example.com", timezone: "Europe/Paris", locale: "en-US");
    var github = factory.CreateIdentity(user, provider: Account.IdentityProvider.GitHub, username: "githubber");
    var org = factory.CreateOrganization(id: KnownOrganizationId, slug: "acme");
    factory.CreateMember(org, user);
    return factory.BuildAuthenticatedUser(user,
      roles: [],
      identities: [github],
      organizations: [org],
      authenticatedOn: authenticatedOn
    );
  }

  //-----------------------------------------------------------------------------------------------

  private CookieValidatePrincipalContext BuildCookieValidationContext(ClaimsPrincipal principal)
  {
    var services = new ServiceCollection();
    services.AddSingleton(CreateMockAuthenticationService());
    var serviceProvider = services.BuildServiceProvider();
    var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
    var scheme = new AuthenticationScheme(TestConfig.AuthenticationScheme, TestConfig.AuthenticationScheme, typeof(CookieAuthenticationHandler));
    var options = new CookieAuthenticationOptions();
    var authProperties = new AuthenticationProperties();
    var authTicket = new AuthenticationTicket(principal, authProperties, TestConfig.AuthenticationScheme);
    return new CookieValidatePrincipalContext(
      httpContext,
      scheme,
      options,
      authTicket
    );
  }

  //-----------------------------------------------------------------------------------------------

  public static IAuthenticationService CreateMockAuthenticationService()
  {
    var mock = Substitute.For<IAuthenticationService>();
    mock.SignInAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>())
      .Returns(Task.CompletedTask);
    mock.SignOutAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<AuthenticationProperties>())
      .Returns(Task.CompletedTask);
    return mock;
  }

  private void AssertSignInCalled(CookieValidatePrincipalContext context)
  {
    var mockAuthentication = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationService>();
    mockAuthentication.Received(1).SignInAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>());
    mockAuthentication.Received(0).SignOutAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<AuthenticationProperties>());
  }

  private void AssertSignOutCalled(CookieValidatePrincipalContext context)
  {
    var mockAuthentication = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationService>();
    mockAuthentication.Received(0).SignInAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>());
    mockAuthentication.Received(1).SignOutAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<AuthenticationProperties>());
  }

  private void AssertNeitherSignInNorSignOutCalled(CookieValidatePrincipalContext context)
  {
    var mockAuthentication = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationService>();
    mockAuthentication.Received(0).SignInAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>());
    mockAuthentication.Received(0).SignOutAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<AuthenticationProperties>());
  }
}

//=================================================================================================
// TEST TOKEN AUTH HANDLER
//=================================================================================================

public class TokenAuthenticationTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestSuccessWithBearerToken()
  {
    using (var test = new DomainTest(this))
    {
      var token = Crypto.GenerateToken("active");
      var handler = await BuildTokenAuthenticationHandler(test.App, auth: $"Bearer {token}");

      var result = await handler.TestHandleAuthenticateAsync();
      Assert.True(result.Succeeded);
      Assert.False(result.None);
      Assert.Null(result.Failure);

      var principal = result.Principal as UserPrincipal;

      Assert.NotNull(principal);
      Assert.Equal(Identify("active"), principal.Id);
      Assert.Equal("Active User", principal.Name);
      Assert.Equal("active@example.com", principal.Email);
      Assert.Equal("America/Los_Angeles", principal.TimeZone);
      Assert.Equal("en-US", principal.Locale);
      Assert.Empty(principal.Roles);
      Assert.Equal(["github:active"], principal.Identities);
      Assert.Equal([Identify("atari")], principal.Organizations);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestSuccessWithJWT()
  {
    using (var test = new DomainTest(this))
    {
      var jwt = test.JwtGenerator.Create(new List<Claim>
      {
        new Claim("id", Identify("active").ToString())
      });

      var handler = await BuildTokenAuthenticationHandler(test.App, auth: $"Bearer {jwt}");

      var result = await handler.TestHandleAuthenticateAsync();
      Assert.True(result.Succeeded);
      Assert.False(result.None);
      Assert.Null(result.Failure);

      var principal = result.Principal as UserPrincipal;

      Assert.NotNull(principal);
      Assert.Equal(Identify("active"), principal.Id);
      Assert.Equal("Active User", principal.Name);
      Assert.Equal("active@example.com", principal.Email);
      Assert.Equal("America/Los_Angeles", principal.TimeZone);
      Assert.Equal("en-US", principal.Locale);
      Assert.Empty(principal.Roles);
      Assert.Equal(["github:active"], principal.Identities);
      Assert.Equal([Identify("atari")], principal.Organizations);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestMissingHeader()
  {
    using (var test = new DomainTest(this))
    {
      var handler = await BuildTokenAuthenticationHandler(test.App);
      var result = await handler.TestHandleAuthenticateAsync();
      Assert.False(result.Succeeded);
      Assert.True(result.None);
      Assert.Null(result.Failure);
      Assert.Null(result.Principal);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestMissingBearer()
  {
    using (var test = new DomainTest(this))
    {
      var handler = await BuildTokenAuthenticationHandler(test.App, auth: "Basic name:password");
      var result = await handler.TestHandleAuthenticateAsync();
      Assert.False(result.Succeeded);
      Assert.False(result.None);
      Assert.Null(result.Principal);
      Assert.Equal("Invalid authorization header", result.Failure?.Message);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestInvalidToken()
  {
    using (var test = new DomainTest(this))
    {
      var token = Crypto.GenerateToken("invalid-token");
      var handler = await BuildTokenAuthenticationHandler(test.App, auth: $"Bearer {token}");
      var result = await handler.TestHandleAuthenticateAsync();
      Assert.False(result.Succeeded);
      Assert.False(result.None);
      Assert.Null(result.Principal);
      Assert.Equal("Invalid token", result.Failure?.Message);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDisabledUser()
  {
    using (var test = new DomainTest(this))
    {
      var token = Crypto.GenerateToken("active");
      var handler = await BuildTokenAuthenticationHandler(test.App, auth: $"Bearer {token}");
      var result = await handler.TestHandleAuthenticateAsync();
      Assert.True(result.Succeeded);

      var principal = result.Principal as UserPrincipal;
      Assert.NotNull(principal);
      Assert.Equal("Active User", principal.Name);

      test.Factory.DisableUser(principal.Id);

      handler = await BuildTokenAuthenticationHandler(test.App, auth: "Bearer active");
      result = await handler.TestHandleAuthenticateAsync();
      Assert.False(result.Succeeded);
      Assert.False(result.None);
      Assert.Null(result.Principal);
      Assert.Equal("Invalid token", result.Failure?.Message);
    }
  }

  // ==============================================================================================
  // PRIVATE: HACK to make TokenAuthenticationHandler testable. Oh god how I hate the insane dotnet
  // OO abstractions that require ridiculous contortions to construct any kind of dependency
  // for unit testing. God this is absolute dogs**t. Who thought dotnet was a good idea. Yuck
  // No wonder the go-to default for dotnet is to moq the s**t out of everything, but that's
  // also an awful idea because (a) you end up testing phantom code and (b) the popular Moq
  // library includes spyware now. God I really miss the Elixir community.
  // ==============================================================================================

  private async Task<TokenAuthenticationHandler> BuildTokenAuthenticationHandler(Application app, string? auth = null)
  {
    var optionsMonitor = BuildOptionsMonitor();
    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var encoder = UrlEncoder.Default;

    var handler = new TokenAuthenticationHandler(
      optionsMonitor,
      loggerFactory,
      encoder,
      app);

    var context = new DefaultHttpContext();
    if (auth is not null)
      context.Request.Headers["Authorization"] = auth;

    await handler.InitializeAsync(new AuthenticationScheme(TestConfig.AuthenticationScheme, null, typeof(TokenAuthenticationHandler)), context);

    return handler;
  }

  private OptionsMonitor<TokenAuthenticationOptions> BuildOptionsMonitor()
  {
    var configureOptions = new List<IConfigureOptions<TokenAuthenticationOptions>>
    {
      new ConfigureTokenAuthenticationOptions()
    };

    var postConfigureOptions = new List<IPostConfigureOptions<TokenAuthenticationOptions>>
    {
      new PostConfigureTokenAuthenticationOptions()
    };

    var optionsFactory = new OptionsFactory<TokenAuthenticationOptions>(
        configureOptions,
        postConfigureOptions);

    return new OptionsMonitor<TokenAuthenticationOptions>(optionsFactory,
      Array.Empty<IOptionsChangeTokenSource<TokenAuthenticationOptions>>(),
      new OptionsCache<TokenAuthenticationOptions>());
  }

  public class ConfigureTokenAuthenticationOptions : IConfigureOptions<TokenAuthenticationOptions>
  {
    public void Configure(TokenAuthenticationOptions options)
    {
    }
  }

  public class PostConfigureTokenAuthenticationOptions : IPostConfigureOptions<TokenAuthenticationOptions>
  {
    public void PostConfigure(string? name, TokenAuthenticationOptions options)
    {
    }
  }

  //-----------------------------------------------------------------------------------------------
}