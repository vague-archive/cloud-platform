namespace Void.Platform.Web;

using Provider = Account.IdentityProvider;

public static class OAuth
{
  //-----------------------------------------------------------------------------------------------

  public record Identity
  {
    public required Provider Provider { get; set; }
    public required string Identifier { get; set; }
    public required string UserName { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
  }

  //-----------------------------------------------------------------------------------------------

  public class Providers : Dictionary<Provider, Handler>
  {
    public void AddGitHub(Config.OAuthConfig? config)
    {
      if (config is not null)
        Add(Provider.GitHub, new GitHubHandler(config));
    }

    public void AddDiscord(Config.OAuthConfig? config)
    {
      if (config is not null)
        Add(Provider.Discord, new DiscordHandler(config));
    }

    public bool Has(Provider provider)
    {
      return this.ContainsKey(provider);
    }

    public bool HasGitHub
    {
      get
      {
        return this.Has(Provider.GitHub);
      }
    }

    public bool HasDiscord
    {
      get
      {
        return this.Has(Provider.Discord);
      }
    }

    public Handler? Get(string provider)
    {
      var type = provider.ToEnum<Provider>();
      if (ContainsKey(type))
        return this[type];
      else
        return null;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public class GitHubHandler : Handler
  {
    public GitHubHandler(Config.OAuthConfig config) : base(Account.IdentityProvider.GitHub, config)
    {
      AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
      TokenEndpoint = "https://github.com/login/oauth/access_token";
      Scopes = "read:user";
    }

    protected override async Task<Identity> Identify(HttpClient client, string token)
    {
      var githubApi = new GitHub(client, token);
      var githubUser = await githubApi.GetCurrentUser();
      return new Identity
      {
        Provider = Provider.GitHub,
        Identifier = githubUser.Id.ToString(),
        UserName = githubUser.Login,
        FullName = githubUser.Name,
        Email = githubUser.Email,
      };
    }
  }

  //-----------------------------------------------------------------------------------------------

  public class DiscordHandler : Handler
  {
    public DiscordHandler(Config.OAuthConfig config) : base(Account.IdentityProvider.Discord, config)
    {
      AuthorizationEndpoint = "https://discord.com/oauth2/authorize";
      TokenEndpoint = "https://discord.com/api/oauth2/token";
      Scopes = "identify email";
    }

    protected override async Task<Identity> Identify(HttpClient client, string token)
    {
      var discordApi = new DiscordApi(client, token);
      var discordUser = await discordApi.GetCurrentUser();
      return new Identity
      {
        Provider = Provider.Discord,
        Identifier = discordUser.Id,
        UserName = discordUser.UserName,
        FullName = discordUser.Name ?? discordUser.UserName,
        Email = discordUser.Email,
      };
    }
  }

  //-----------------------------------------------------------------------------------------------

  public abstract class Handler
  {
    public Account.IdentityProvider Provider { get; init; }
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }
    public string AuthorizationEndpoint { get; init; } = "";
    public string TokenEndpoint { get; init; } = "";
    public string Scopes { get; init; } = "";

    protected abstract Task<Identity> Identify(HttpClient client, string token);

    public Handler(Account.IdentityProvider provider, Config.OAuthConfig config)
    {
      Provider = provider;
      ClientId = config.ClientId;
      ClientSecret = config.ClientSecret;
    }

    public (Uri, string, string) Challenge(string callbackUrl)
    {
      var state = Crypto.GenerateToken();
      var (verifier, challenge) = Crypto.GeneratePkceVerifier();
      var url = new Uri(AuthorizationEndpoint).WithParams(new[]
      {
        new Http.Param("response_type", "code"),
        new Http.Param("client_id", ClientId),
        new Http.Param("redirect_uri", callbackUrl),
        new Http.Param("scope", Scopes),
        new Http.Param("state", state),
        new Http.Param("code_challenge", challenge),
        new Http.Param("code_challenge_method", "S256"),
      });
      return (url, verifier, state);
    }

    public async Task<Result<Identity>> Callback(string callbackUrl, string? verifier, string? expectedState, HttpRequest request, HttpClient client)
    {
      var error = request.Query["error"];
      if (String.Equals(error, "invalid_request"))
      {
        return Validation.Fail("request", request.Query["error_description"].ToString());
      }

      var code = request.Query["code"].ToString();
      if (String.IsNullOrEmpty(code))
        return Validation.Fail("code", "is missing");

      if (verifier is null)
        return Validation.Fail("verifier", "is missing");

      var state = request.Query["state"];
      if (!String.Equals(state, expectedState))
        return Validation.Fail("state", "is invalid");

      var tokenResponse = await client.PostAsync(TokenEndpoint, new FormUrlEncodedContent(new[]
      {
        new KeyValuePair<string, string>("grant_type", "authorization_code"),
        new KeyValuePair<string, string>("code", code),
        new KeyValuePair<string, string>("client_id", ClientId),
        new KeyValuePair<string, string>("client_secret", ClientSecret),
        new KeyValuePair<string, string>("redirect_uri", callbackUrl),
        new KeyValuePair<string, string>("code_verifier", verifier),
      }));
      var accessToken = await GetAccessToken(tokenResponse);
      if (accessToken is null)
        return Validation.Fail("access token", "is missing");

      var identity = await Identify(client, accessToken);
      if (identity is null)
        return Validation.Fail($"identity", "not found");

      return Result.Ok(identity);
    }
  }

  //-----------------------------------------------------------------------------------------------

  private static async Task<string?> GetAccessToken(HttpResponseMessage response)
  {
    response.EnsureSuccessStatusCode();
    var contentType = response.Content.Headers.ContentType;
    if (contentType is null)
      return null;

    var body = await response.Content.ReadAsStringAsync();

    switch (contentType.MediaType)
    {
      case "application/x-www-form-urlencoded":
        return GetAccessTokenFromForm(body);
      case "application/json":
        return GetAccessTokenFromJson(body);
      default:
        throw new Exception($"unexpected token content-type: {contentType.MediaType}");
    }
  }

  private static string? GetAccessTokenFromForm(string body)
  {
    return Http.Params(body).Get("access_token");
  }

  private static string? GetAccessTokenFromJson(string body)
  {
    using var doc = Json.Parse(body);
    return doc.RootElement.OptionalString("access_token");
  }

  //-----------------------------------------------------------------------------------------------

}