namespace Void.Platform.Web;

public class ConfigValues : Dictionary<string, string?>
{
  public ConfigValues() : base()
  {
  }

  public ConfigValues(IEnumerable<KeyValuePair<string, string?>> collection) : base(collection)
  {
  }
}

public record Config
{
  //-----------------------------------------------------------------------------------------------

  public const string TestEnvironmentName = "Test";
  public const string TestServerName = "test-server";
  public const string SwaggerDescription = "An Api for...";
  public const string SwaggerTitle = "Void Platform";
  public const string SwaggerVersion = "1.0";
  public const string CookieAuthenticationScheme = "cookie";
  public const string TokenAuthenticationScheme = "token";
  public const string PageAuthorizationPolicy = "page";
  public const string DefaultDatabaseUrl = "Host=localhost;User=platform;Password=platform;Database=platform_dev";
  public const string DefaultTestDatabaseUrl = "Host=localhost;User=platform;Password=platform;Database=platform_test;DefaultCommandTimeout=60;ConnectionTimeout=30";
  public const string DefaultHost = "localhost";
  public const string DefaultPublicRoot = "wwwroot";
  public const string DefaultEncryptKey = "x+eLvtCTh0dXznoyLt3gOGtGZWrvBmlz0u1Qqd1qmMU=";
  public const string DefaultSigningKey = "Zqlkii6IEpmFILaNd0ZZGFfbLqGZgFrJopnnjPttGyw=";
  public const string DefaultSupportEmail = "support@void.dev";
  public const string DefaultFileStorePath = "../.filestore";
  public const string DefaultKeysPath = "../.keys";
  public const string DefaultVpcCidr = "10.10.0.0/16";
  public const string AuthCookieName = "void-platform-auth";
  public const string SessionCookieName = "void-platform-session";
  public const string CsrfCookieName = "void-platform-csrf";
  public const string CsrfFieldName = "csrf-token";
  public const string CsrfHeaderName = "X-CSRF-TOKEN";
  public const string FlashCookieName = "void-platform-flash";
  public const string SandboxPostmarkApiToken = "6caade91-44e9-4e49-afd9-b957751159c9";

  //-----------------------------------------------------------------------------------------------

  public static class Key
  {
    public const string Host = "HOST";
    public const string Port = "PORT";
    public const string LogLevel = "LOG_LEVEL";
    public const string UrlScheme = "URL_SCHEME";
    public const string UrlHost = "URL_HOST";
    public const string UrlPort = "URL_PORT";
    public const string PublicRoot = "PUBLIC_ROOT";
    public const string EncryptKey = "ENCRYPT_KEY";
    public const string SigningKey = "SIGNING_KEY";
    public const string DatabaseUrl = "DATABASE_URL";
    public const string TestDatabaseUrl = "TEST_DATABASE_URL";
    public const string ProductionDatabaseUrl = "PRODUCTION_DATABASE_URL";
    public const string RedisCacheUrl = "REDIS_CACHE_URL";
    public const string GitHubClientId = "GITHUB_CLIENT_ID";
    public const string GitHubClientSecret = "GITHUB_CLIENT_SECRET";
    public const string GitHubApiToken = "GITHUB_API_TOKEN";
    public const string DiscordClientId = "DISCORD_CLIENT_ID";
    public const string DiscordClientSecret = "DISCORD_CLIENT_SECRET";
    public const string PostmarkApiToken = "POSTMARK_API_TOKEN";
    public const string SentryEndpoint = "SENTRY_ENDPOINT";
    public const string SupportEmail = "SUPPORT_EMAIL";
    public const string FileStorePath = "FILESTORE_PATH";
    public const string FileStoreBucket = "FILESTORE_BUCKET";
    public const string KeysPath = "KEYS_PATH";
    public const string EnablePasswordLogin = "ENABLE_PASSWORD_LOGIN";
    public const string EnableFirewall = "ENABLE_FIREWALL";
    public const string VpcCidr = "VPC_CIDR";
  }

  //-----------------------------------------------------------------------------------------------

  public record FileStoreConfig
  {
    public required string Path { get; init; }
    public string? Bucket { get; init; }
  }

  public record WebConfig
  {
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required LogLevel LogLevel { get; init; }
    public required Uri PublicUrl { get; init; }
    public required string PublicRoot { get; init; }
    public required string KeysPath { get; init; }
    public required string EncryptKey { get; init; }
    public required string SigningKey { get; init; }
    public required CookieConfig AuthCookie { get; init; }
    public required CookieConfig SessionCookie { get; init; }
    public required OAuthProviders OAuth { get; init; }
    public string? SentryEndpoint { get; init; }
    public required string VpcCidr { get; init; }
  }

  public record OAuthProviders
  {
    public OAuthConfig? GitHub { get; init; }
    public OAuthConfig? Discord { get; init; }
  }

  public record OAuthConfig
  {
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
  }

  public record CookieConfig
  {
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required bool HttpOnly { get; init; }
    public required CookieSecurePolicy Secure { get; init; }
    public required SameSiteMode SameSite { get; init; }
    public required TimeSpan MaxAge { get; init; }
  }

  public record EnableFlags
  {
    public required bool PasswordLogin { get; init; }
    public required bool Firewall { get; init; }
  }

  //-----------------------------------------------------------------------------------------------

  public Domain.Config Domain { get; init; }
  public WebConfig Web { get; init; }
  public MailerConfig Mailer { get; init; }
  public FileStoreConfig FileStore { get; init; }
  public EnableFlags Enable { get; init; }

  public Config(Env env) : this(env, new ConfigValues())
  {
  }

  public Config(Env env, IConfiguration cfg) : this(env, new ConfigValues(cfg.AsEnumerable()))
  {
  }

  public Config(Env env, ConfigValues values)
  {
    var host = Config.GetString(values, Key.Host, DefaultHost);
    var port = Config.GetInt(values, Key.Port, 3000);
    var logLevel = Config.GetEnum(values, Key.LogLevel, LogLevel.Information);
    var urlScheme = Config.GetString(values, Key.UrlScheme, "http");
    var urlHost = Config.GetString(values, Key.UrlHost, host);
    var urlPort = Config.GetInt(values, Key.UrlPort, port);
    var publicUrl = new Uri($"{urlScheme}://{urlHost}:{urlPort}");
    var publicRoot = Config.GetString(values, Key.PublicRoot, DefaultPublicRoot);
    var keysPath = Config.GetString(values, Key.KeysPath, DefaultKeysPath);
    var encryptKey = Config.GetString(values, Key.EncryptKey, DefaultEncryptKey);
    var signingKey = Config.GetString(values, Key.SigningKey, DefaultSigningKey);
    var cookieSecure = publicUrl.Scheme == "https" ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    var cookieDuration = TimeSpan.FromDays(7);
    var serverName = Config.GetServerName(env);

    Domain = new Domain.Config
    {
      Env = env,
      ServerName = serverName,
      DatabaseUrl = Config.GetString(values, Key.DatabaseUrl, DefaultDatabaseUrl),
      RedisCacheUrl = Config.GetOptionalString(values, Key.RedisCacheUrl),
      GitHubApiToken = Config.GetOptionalString(values, Key.GitHubApiToken),
    };

    Web = new WebConfig
    {
      Host = host,
      Port = port,
      LogLevel = logLevel,
      PublicUrl = publicUrl,
      PublicRoot = publicRoot,
      KeysPath = keysPath,
      EncryptKey = encryptKey,
      SigningKey = signingKey,
      AuthCookie = new CookieConfig
      {
        Name = Config.AuthCookieName,
        Path = "/",
        HttpOnly = true,
        Secure = cookieSecure,
        SameSite = SameSiteMode.Lax,
        MaxAge = cookieDuration,
      },
      SessionCookie = new CookieConfig
      {
        Name = Config.SessionCookieName,
        Path = "/",
        HttpOnly = true,
        Secure = cookieSecure,
        SameSite = SameSiteMode.Lax,
        MaxAge = cookieDuration,
      },
      OAuth = new OAuthProviders
      {
        GitHub = GetOAuth(values, Key.GitHubClientId, Key.GitHubClientSecret),
        Discord = GetOAuth(values, Key.DiscordClientId, Key.DiscordClientSecret),
      },
      SentryEndpoint = Config.GetOptionalString(values, Key.SentryEndpoint),
      VpcCidr = Config.GetString(values, Key.VpcCidr, Config.DefaultVpcCidr)
    };

    Mailer = new MailerConfig
    {
      ApiToken = Config.GetString(values, Key.PostmarkApiToken, SandboxPostmarkApiToken),
      ProductName = "Void",
      ProductUrl = Web.PublicUrl,
      SupportEmail = Config.GetString(values, Key.SupportEmail, DefaultSupportEmail),
    };

    FileStore = new FileStoreConfig
    {
      Path = Config.GetString(values, Key.FileStorePath, DefaultFileStorePath),
      Bucket = Config.GetOptionalString(values, Key.FileStoreBucket)
    };

    Enable = new EnableFlags
    {
      PasswordLogin = Config.GetBool(values, Key.EnablePasswordLogin, true),
      Firewall = Config.GetBool(values, Key.EnableFirewall, true)
    };
  }

  //-----------------------------------------------------------------------------------------------

  public static string GetString(ConfigValues values, string key)
  {
    if (values.ContainsKey(key))
    {
      return values[key]!;
    }
    else
    {
      throw new MissingException(key);
    }
  }

  public static string GetString(ConfigValues values, string key, string defaultValue)
  {
    if (values.ContainsKey(key))
    {
      return values[key]!;
    }
    else
    {
      return defaultValue;
    }
  }

  public static string? GetOptionalString(ConfigValues values, string key)
  {
    if (values.ContainsKey(key))
    {
      var value = values[key]!;
      if (value.ToUpper() == ECS_IGNORE)
        return null;
      return value;
    }
    else
    {
      return null;
    }
  }

  // ECS doesn't support optional secrets, so we need to ignore
  // a dummy sentional value
  private static string ECS_IGNORE = "IGNORE";

  //-----------------------------------------------------------------------------------------------

  public static int GetInt(ConfigValues values, string key)
  {
    if (values.ContainsKey(key))
    {
      return int.Parse(values[key]!);
    }
    else
    {
      throw new MissingException(key);
    }
  }

  public static int GetInt(ConfigValues values, string key, int defaultValue)
  {
    if (values.ContainsKey(key))
    {
      return int.Parse(values[key]!);
    }
    else
    {
      return defaultValue;
    }
  }

  public static T GetEnum<T>(ConfigValues values, string key, T defaultValue) where T : struct, Enum
  {
    return GetEnum<T>(values, key, defaultValue.ToString());
  }

  public static T GetEnum<T>(ConfigValues values, string key, string defaultValue) where T : struct, Enum
  {
    return GetString(values, key, defaultValue).ToEnum<T>();
  }

  //-----------------------------------------------------------------------------------------------

  public static bool GetBool(ConfigValues values, string key, bool defaultValue)
  {
    if (values.ContainsKey(key))
    {
      return bool.Parse(values[key]!);
    }
    else
    {
      return defaultValue;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static OAuthConfig? GetOAuth(ConfigValues values, string clientIdKey, string clientSecretKey)
  {
    var clientId = GetOptionalString(values, clientIdKey);
    var clientSecret = GetOptionalString(values, clientSecretKey);
    if (clientId is null || clientSecret is null)
      return null;

    return new OAuthConfig
    {
      ClientId = clientId,
      ClientSecret = clientSecret,
    };
  }

  //-----------------------------------------------------------------------------------------------

  private static string GetServerName(Env env)
  {
    if (env == Env.Test)
    {
      return Config.TestServerName;
    }
    else
    {
      // TODO: get task ARN when run in ECS
      return System.Net.Dns.GetHostName();
    }
  }

  //-----------------------------------------------------------------------------------------------

  public void Log(ILogger logger)
  {
    var MISSING = "[MISSING]";
    var REDACTED = "[REDACTED]";

    // TODO: redact DatabaseUrl and RedisCacheUrl when finished debugging

    logger.Information("[CONFIG] Domain.Env:            {value}", Domain.Env);
    logger.Information("[CONFIG] Domain.ServerName:     {value}", Domain.ServerName);
    logger.Information("[CONFIG] Domain.DatabaseUrl:    {value}", RedactDbConn(Domain.DatabaseUrl, REDACTED));
    logger.Information("[CONFIG] Domain.RedisCacheUrl:  {value}", RedactRedisConn(Domain.RedisCacheUrl, REDACTED));
    logger.Information("[CONFIG] Domain.GitHubApiToken: {value}", Domain.GitHubApiToken is null ? MISSING : REDACTED);
    logger.Information("[CONFIG] Web.Host:              {value}", Web.Host);
    logger.Information("[CONFIG] Web.Port:              {value}", Web.Port);
    logger.Information("[CONFIG] Web.LogLevel:          {value}", Web.LogLevel);
    logger.Information("[CONFIG] Web.PublicUrl:         {value}", Web.PublicUrl);
    logger.Information("[CONFIG] Web.PublicRoot:        {value}", Web.PublicRoot);
    logger.Information("[CONFIG] Web.KeysPath:          {value}", Web.KeysPath);
    logger.Information("[CONFIG] Web.EncryptKey:        {value}", REDACTED);
    logger.Information("[CONFIG] Web.SigningKey:        {value}", REDACTED);
    logger.Information("[CONFIG] Web.AuthCookie:        {value}", Web.AuthCookie);
    logger.Information("[CONFIG] Web.SessionCookie:     {value}", Web.SessionCookie);
    logger.Information("[CONFIG] Web.OAuth.GitHub:      {value}", Web.OAuth.GitHub is null ? MISSING : REDACTED);
    logger.Information("[CONFIG] Web.OAuth.Discord:     {value}", Web.OAuth.Discord is null ? MISSING : REDACTED);
    logger.Information("[CONFIG] Web.SentryEndpoint:    {value}", Web.SentryEndpoint is null ? MISSING : REDACTED);
    logger.Information("[CONFIG] Web.VpcCidr:           {value}", Web.VpcCidr);
    logger.Information("[CONFIG] Mailer.ApiToken:       {value}", Mailer.ApiToken is null ? MISSING : REDACTED);
    logger.Information("[CONFIG] Mailer.ProductName:    {value}", Mailer.ProductName);
    logger.Information("[CONFIG] Mailer.ProductUrl:     {value}", Mailer.ProductUrl);
    logger.Information("[CONFIG] Mailer.SupportEmail:   {value}", Mailer.SupportEmail);
    logger.Information("[CONFIG] FileStore.Path:        {value}", FileStore.Path);
    logger.Information("[CONFIG] FileStore.Bucket:      {value}", FileStore.Bucket ?? MISSING);
    logger.Information("[CONFIG] Enable.Firewall        {value}", Enable.Firewall);
    logger.Information("[CONFIG] Enable.PasswordLogin   {value}", Enable.PasswordLogin);
  }

  private string RedactDbConn(string conn, string redacted)
  {
    var parts = conn.Split(';', StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < parts.Length; i++)
    {
      var kv = parts[i].Split('=', 2);
      if (kv.Length == 2 && kv[0].Trim().Equals("password", StringComparison.OrdinalIgnoreCase) ||
                            kv[0].Trim().Equals("pwd", StringComparison.OrdinalIgnoreCase))
      {
        parts[i] = $"{kv[0]}={redacted}";
      }
    }
    return string.Join(';', parts) + (conn.EndsWith(";") ? ";" : "");
  }

  private string? RedactRedisConn(string? conn, string redacted)
  {
    if (conn is null)
      return null;
    var parts = conn.Split(',', StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < parts.Length; i++)
    {
      var kv = parts[i].Split('=', 2);
      if (kv.Length == 2 && kv[0].Trim().Equals("password", StringComparison.OrdinalIgnoreCase))
      {
        parts[i] = $"{kv[0]}={redacted}";
      }
    }
    return string.Join(',', parts);
  }

  //-----------------------------------------------------------------------------------------------

  public class MissingException : Exception
  {
    public MissingException(string key) : base($"no value found for {key}") { }
  }

  //-----------------------------------------------------------------------------------------------

  public static string DatabaseUrl { get { return Environment.GetEnvironmentVariable(Key.DatabaseUrl) ?? DefaultDatabaseUrl; } }
  public static string TestDatabaseUrl { get { return Environment.GetEnvironmentVariable(Key.TestDatabaseUrl) ?? DefaultTestDatabaseUrl; } }
  public static string ProductionDatabaseUrl { get { return Environment.GetEnvironmentVariable(Key.ProductionDatabaseUrl) ?? throw new Exception("missing PRODUCTION_DATABASE_URL"); } }

  //-----------------------------------------------------------------------------------------------
}