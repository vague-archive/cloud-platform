namespace Void.Platform.Web;

using Microsoft.AspNetCore.Http;

public class OAuthTest : TestCase
{
  const string CODE = "example-grant-code";
  const string STATE = "example-csrf-state";
  const string VERIFIER = "example-code-verifier";
  const string ACCESS_TOKEN = "example-access-token";
  const string CALLBACK_URL = "http://localhost:3000/login/example/callback";

  //===============================================================================================
  // GITHUB
  //===============================================================================================

  [Fact]
  public void TestGitHubProperties()
  {
    var handler = new OAuth.GitHubHandler(configure("github"));

    Assert.Equal("github-client-id", handler.ClientId);
    Assert.Equal("github-client-secret", handler.ClientSecret);
    Assert.Equal("https://github.com/login/oauth/authorize", handler.AuthorizationEndpoint);
    Assert.Equal("https://github.com/login/oauth/access_token", handler.TokenEndpoint);
    Assert.Equal("read:user", handler.Scopes);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGitHubChallenge()
  {
    var handler = new OAuth.GitHubHandler(configure("example"));

    var (authUrl, verifier, state) = handler.Challenge(CALLBACK_URL);
    var authParams = Http.Params(authUrl.Query);

    Assert.Equal("https://github.com/login/oauth/authorize", authUrl.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped));
    Assert.Equal(["response_type", "client_id", "redirect_uri", "scope", "state", "code_challenge", "code_challenge_method"], authParams.AllKeys.ToList());
    Assert.Equal("code", authParams["response_type"]);
    Assert.Equal("example-client-id", authParams["client_id"]);
    Assert.Equal(CALLBACK_URL, authParams["redirect_uri"]);
    Assert.Equal("read:user", authParams["scope"]);
    Assert.Equal(state, authParams["state"]);
    Assert.Equal(Crypto.PkceEncode(Crypto.Sha256(verifier)), authParams["code_challenge"]);
    Assert.Equal("S256", authParams["code_challenge_method"]);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGitHubCallback()
  {
    var handler = new OAuth.GitHubHandler(configure("example"));

    HttpRequestMessage? tokenRequest = null;

    var bypass = new MockHttpMessageHandler();
    bypass.When("https://github.com/login/oauth/access_token")
      .With(r => { tokenRequest = r; return true; })
      .Respond("application/json", Json.Serialize(new { access_token = ACCESS_TOKEN }));
    bypass.When("https://api.github.com/user")
      .WithHeaders("Authorization", $"Bearer {ACCESS_TOKEN}")
      .Respond("application/json", Json.Serialize(new { id = 738109, login = "jakesgordon", name = "Jake Gordon" }));
    var httpClient = bypass.ToHttpClient();

    var context = new DefaultHttpContext();
    var request = context.Request;
    request.QueryString = new QueryString($"?code={CODE}&state={STATE}");

    var result = await handler.Callback(CALLBACK_URL, VERIFIER, STATE, request, httpClient);
    Assert.True(result.Succeeded);

    var identity = result.Value;
    Assert.Equal(Account.IdentityProvider.GitHub, identity.Provider);
    Assert.Equal("738109", identity.Identifier);
    Assert.Equal("jakesgordon", identity.UserName);
    Assert.Equal("Jake Gordon", identity.FullName);
    Assert.Null(identity.Email);

    // verify internals of the request made to the token endpoint
    Assert.NotNull(tokenRequest);
    Assert.IsType<FormUrlEncodedContent>(tokenRequest.Content);
    var tokenBody = await tokenRequest.Content.ReadAsStringAsync(CancelToken);
    var tokenParams = Http.Params(tokenBody);
    Assert.Equal(["grant_type", "code", "client_id", "client_secret", "redirect_uri", "code_verifier"], tokenParams.AllKeys.ToList());
    Assert.Equal("authorization_code", tokenParams["grant_type"]);
    Assert.Equal(CODE, tokenParams["code"]);
    Assert.Equal(handler.ClientId, tokenParams["client_id"]);
    Assert.Equal(handler.ClientSecret, tokenParams["client_secret"]);
    Assert.Equal(CALLBACK_URL, tokenParams["redirect_uri"]);
    Assert.Equal(VERIFIER, tokenParams["code_verifier"]);
  }

  //===============================================================================================
  // DISCORD
  //===============================================================================================

  [Fact]
  public void TestDiscordProperties()
  {
    var handler = new OAuth.DiscordHandler(configure("discord"));

    Assert.Equal("discord-client-id", handler.ClientId);
    Assert.Equal("discord-client-secret", handler.ClientSecret);
    Assert.Equal("https://discord.com/oauth2/authorize", handler.AuthorizationEndpoint);
    Assert.Equal("https://discord.com/api/oauth2/token", handler.TokenEndpoint);
    Assert.Equal("identify email", handler.Scopes);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestDiscordChallenge()
  {
    var handler = new OAuth.DiscordHandler(configure("discord"));

    var (authUrl, verifier, state) = handler.Challenge(CALLBACK_URL);
    var authParams = Http.Params(authUrl.Query);

    Assert.Equal("https://discord.com/oauth2/authorize", authUrl.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped));
    Assert.Equal(["response_type", "client_id", "redirect_uri", "scope", "state", "code_challenge", "code_challenge_method"], authParams.AllKeys.ToList());
    Assert.Equal("code", authParams["response_type"]);
    Assert.Equal("discord-client-id", authParams["client_id"]);
    Assert.Equal(CALLBACK_URL, authParams["redirect_uri"]);
    Assert.Equal("identify email", authParams["scope"]);
    Assert.Equal(state, authParams["state"]);
    Assert.Equal(Crypto.PkceEncode(Crypto.Sha256(verifier)), authParams["code_challenge"]);
    Assert.Equal("S256", authParams["code_challenge_method"]);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDiscordCallback()
  {
    var handler = new OAuth.DiscordHandler(configure("discord"));

    HttpRequestMessage? tokenRequest = null;

    var bypass = new MockHttpMessageHandler();
    bypass.When("https://discord.com/api/oauth2/token")
      .With(r => { tokenRequest = r; return true; })
      .Respond("application/json", Json.Serialize(new { access_token = ACCESS_TOKEN }));
    bypass.When("https://discord.com/api/v10/users/@me")
      .WithHeaders("Authorization", $"Bearer {ACCESS_TOKEN}")
      .Respond("application/json", Json.Serialize(new { id = "1204083812892418108", username = "jakesgordon", global_name = "Jake Gordon" }));
    var httpClient = bypass.ToHttpClient();

    var context = new DefaultHttpContext();
    var request = context.Request;
    request.QueryString = new QueryString($"?code={CODE}&state={STATE}");

    var result = await handler.Callback(CALLBACK_URL, VERIFIER, STATE, request, httpClient);
    Assert.True(result.Succeeded);

    var identity = result.Value;
    Assert.Equal(Account.IdentityProvider.Discord, identity.Provider);
    Assert.Equal("1204083812892418108", identity.Identifier);
    Assert.Equal("jakesgordon", identity.UserName);
    Assert.Equal("Jake Gordon", identity.FullName);
    Assert.Null(identity.Email);

    // verify internals of the request made to the token endpoint
    Assert.NotNull(tokenRequest);
    Assert.IsType<FormUrlEncodedContent>(tokenRequest.Content);
    var tokenBody = await tokenRequest.Content.ReadAsStringAsync(CancelToken);
    var tokenParams = Http.Params(tokenBody);
    Assert.Equal(["grant_type", "code", "client_id", "client_secret", "redirect_uri", "code_verifier"], tokenParams.AllKeys.ToList());
    Assert.Equal("authorization_code", tokenParams["grant_type"]);
    Assert.Equal(CODE, tokenParams["code"]);
    Assert.Equal(handler.ClientId, tokenParams["client_id"]);
    Assert.Equal(handler.ClientSecret, tokenParams["client_secret"]);
    Assert.Equal(CALLBACK_URL, tokenParams["redirect_uri"]);
    Assert.Equal(VERIFIER, tokenParams["code_verifier"]);
  }

  //===============================================================================================
  // GENERIC ERROR HANDLER BEHAVIOR (using GitHubHandler as example)
  //===============================================================================================

  [Fact]
  public async Task TestCallbackMissingCode()
  {
    var provider = new OAuth.GitHubHandler(configure("example"));

    var handler = new MockHttpMessageHandler();
    var httpClient = handler.ToHttpClient();
    var context = new DefaultHttpContext();
    var request = context.Request;
    request.QueryString = new QueryString($"?state={STATE}");

    var result = await provider.Callback(CALLBACK_URL, VERIFIER, STATE, request, httpClient);
    Assert.False(result.Succeeded);
    Assert.Equal("code is missing", result.Error.Format());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCallbackMissingVerifier()
  {
    var provider = new OAuth.GitHubHandler(configure("example"));

    var handler = new MockHttpMessageHandler();
    var httpClient = handler.ToHttpClient();
    var context = new DefaultHttpContext();
    var request = context.Request;
    request.QueryString = new QueryString($"?code={CODE}&state={STATE}");

    var result = await provider.Callback(CALLBACK_URL, null, STATE, request, httpClient);
    Assert.False(result.Succeeded);
    Assert.Equal("verifier is missing", result.Error.Format());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCallbackInvalidState()
  {
    var provider = new OAuth.GitHubHandler(configure("example"));

    var handler = new MockHttpMessageHandler();
    var httpClient = handler.ToHttpClient();
    var context = new DefaultHttpContext();
    var request = context.Request;
    request.QueryString = new QueryString($"?code={CODE}&state=invalidstate");

    var result = await provider.Callback(CALLBACK_URL, VERIFIER, STATE, request, httpClient);
    Assert.False(result.Succeeded);
    Assert.Equal("state is invalid", result.Error.Format());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCallbackMissingAccessToken()
  {
    var config = configure("example");
    var provider = new OAuth.GitHubHandler(config);

    var handler = new MockHttpMessageHandler();
    handler.When("https://github.com/login/oauth/access_token")
      .Respond("application/json", "{}");
    var httpClient = handler.ToHttpClient();

    var context = new DefaultHttpContext();
    var request = context.Request;
    request.QueryString = new QueryString($"?code=code&state={STATE}");

    var result = await provider.Callback(CALLBACK_URL, VERIFIER, STATE, request, httpClient);
    Assert.False(result.Succeeded);
    Assert.Equal("access token is missing", result.Error.Format());
  }

  //===============================================================================================
  // PROVIDER COLLECTION
  //===============================================================================================

  [Fact]
  public void TestProviderCollection()
  {
    var providers = new OAuth.Providers();
    Assert.False(providers.HasGitHub);
    Assert.False(providers.HasDiscord);

    providers.AddGitHub(null);
    providers.AddDiscord(null);

    Assert.False(providers.HasGitHub);
    Assert.False(providers.HasDiscord);

    providers.AddGitHub(configure("github"));

    Assert.True(providers.HasGitHub);
    Assert.False(providers.HasDiscord);

    providers.AddDiscord(configure("discord"));

    Assert.True(providers.HasGitHub);
    Assert.True(providers.HasDiscord);

    var handler = providers.Get("github");
    Assert.NotNull(handler);
    Assert.IsType<OAuth.GitHubHandler>(handler);
    Assert.Equal("github-client-id", handler.ClientId);
    Assert.Equal("github-client-secret", handler.ClientSecret);

    handler = providers.Get("discord");
    Assert.NotNull(handler);
    Assert.IsType<OAuth.DiscordHandler>(handler);
    Assert.Equal("discord-client-id", handler.ClientId);
    Assert.Equal("discord-client-secret", handler.ClientSecret);

    handler = providers.Get("google");
    Assert.Null(handler);

    handler = providers.Get("microsoft");
    Assert.Null(handler);
  }

  //===============================================================================================
  // PRIVATE HELPER METHODS
  //===============================================================================================

  private Config.OAuthConfig configure(string label)
  {
    return new Config.OAuthConfig
    {
      ClientId = $"{label}-client-id",
      ClientSecret = $"{label}-client-secret",
    };
  }

  //-----------------------------------------------------------------------------------------------
}