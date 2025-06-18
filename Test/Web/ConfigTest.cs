namespace Void.Platform.Web;

using Microsoft.Extensions.Configuration;

public class ConfigTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  public static ConfigValues CustomEnv = new ConfigValues
  {
    {"HOST", "0.0.0.0"},
    {"PORT", "4321"},
    {"URL_SCHEME", "https"},
    {"URL_HOST", "play.void.test"},
    {"URL_PORT", "443"},
    {"LOG_LEVEL", "Debug"},
    {"PUBLIC_ROOT", "custom-root"},
    {"ENCRYPT_KEY", "custom-encrypt-key"},
    {"SIGNING_KEY", "custom-signing-key"},
    {"KEYS_PATH", "/mnt/keys"},
    {"DATABASE_URL", "Host=custom-host;User=custom-user;Password=custom-password;Database=custom-database"},
    {"REDIS_CACHE_URL", "custom-redis-host:6379,defaultDatabase=7"},
    {"GITHUB_CLIENT_ID", "custom-github-client-id"},
    {"GITHUB_CLIENT_SECRET", "custom-github-client-secret"},
    {"GITHUB_API_TOKEN", "custom-github-api-token"},
    {"DISCORD_CLIENT_ID", "custom-discord-client-id"},
    {"DISCORD_CLIENT_SECRET", "custom-discord-client-secret"},
    {"POSTMARK_API_TOKEN", "custom-postmark-api-token"},
    {"SENTRY_ENDPOINT", "custom-sentry-endpoint"},
    {"SUPPORT_EMAIL", "custom-support-email@void.dev"},
    {"FILESTORE_PATH", "custom-filestore-path"},
    {"FILESTORE_BUCKET", "custom-filestore-bucket"},
    {"ENABLE_PASSWORD_LOGIN", "false"},
    {"ENABLE_FIREWALL", "false"},
    {"VPC_CIDR", "1.2.3.4/32"},
  };

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestConstants()
  {
    Assert.Equal("Test", Config.TestEnvironmentName);
    Assert.Equal("test-server", Config.TestServerName);
    Assert.Contains("An Api for...", Config.SwaggerDescription);
    Assert.Equal("Void Platform", Config.SwaggerTitle);
    Assert.Equal("1.0", Config.SwaggerVersion);
    Assert.Equal("cookie", Config.CookieAuthenticationScheme);
    Assert.Equal("token", Config.TokenAuthenticationScheme);
    Assert.Equal("Host=localhost;User=platform;Password=platform;Database=platform_dev", Config.DefaultDatabaseUrl);
    Assert.Equal("Host=localhost;User=platform;Password=platform;Database=platform_test;DefaultCommandTimeout=60;ConnectionTimeout=30", Config.DefaultTestDatabaseUrl);
    Assert.Equal("localhost", Config.DefaultHost);
    Assert.Equal("wwwroot", Config.DefaultPublicRoot);
    Assert.Equal("x+eLvtCTh0dXznoyLt3gOGtGZWrvBmlz0u1Qqd1qmMU=", Config.DefaultEncryptKey);
    Assert.Equal("Zqlkii6IEpmFILaNd0ZZGFfbLqGZgFrJopnnjPttGyw=", Config.DefaultSigningKey);
    Assert.Equal("support@void.dev", Config.DefaultSupportEmail);
    Assert.Equal("../.filestore", Config.DefaultFileStorePath);
    Assert.Equal("../.keys", Config.DefaultKeysPath);
    Assert.Equal("10.10.0.0/16", Config.DefaultVpcCidr);
    Assert.Equal("void-platform-auth", Config.AuthCookieName);
    Assert.Equal("void-platform-session", Config.SessionCookieName);
    Assert.Equal("void-platform-csrf", Config.CsrfCookieName);
    Assert.Equal("X-CSRF-TOKEN", Config.CsrfHeaderName);
    Assert.Equal("csrf-token", Config.CsrfFieldName);
    Assert.Equal("6caade91-44e9-4e49-afd9-b957751159c9", Config.SandboxPostmarkApiToken);
  }

  [Fact]
  public void TestEnvironmentVariableNames()
  {
    Assert.Equal("HOST", Config.Key.Host);
    Assert.Equal("PORT", Config.Key.Port);
    Assert.Equal("URL_SCHEME", Config.Key.UrlScheme);
    Assert.Equal("URL_HOST", Config.Key.UrlHost);
    Assert.Equal("URL_PORT", Config.Key.UrlPort);
    Assert.Equal("LOG_LEVEL", Config.Key.LogLevel);
    Assert.Equal("DATABASE_URL", Config.Key.DatabaseUrl);
    Assert.Equal("TEST_DATABASE_URL", Config.Key.TestDatabaseUrl);
    Assert.Equal("PRODUCTION_DATABASE_URL", Config.Key.ProductionDatabaseUrl);
    Assert.Equal("REDIS_CACHE_URL", Config.Key.RedisCacheUrl);
    Assert.Equal("PUBLIC_ROOT", Config.Key.PublicRoot);
    Assert.Equal("ENCRYPT_KEY", Config.Key.EncryptKey);
    Assert.Equal("SIGNING_KEY", Config.Key.SigningKey);
    Assert.Equal("GITHUB_CLIENT_ID", Config.Key.GitHubClientId);
    Assert.Equal("GITHUB_CLIENT_SECRET", Config.Key.GitHubClientSecret);
    Assert.Equal("GITHUB_API_TOKEN", Config.Key.GitHubApiToken);
    Assert.Equal("DISCORD_CLIENT_ID", Config.Key.DiscordClientId);
    Assert.Equal("DISCORD_CLIENT_SECRET", Config.Key.DiscordClientSecret);
    Assert.Equal("POSTMARK_API_TOKEN", Config.Key.PostmarkApiToken);
    Assert.Equal("SENTRY_ENDPOINT", Config.Key.SentryEndpoint);
    Assert.Equal("FILESTORE_PATH", Config.Key.FileStorePath);
    Assert.Equal("FILESTORE_BUCKET", Config.Key.FileStoreBucket);
    Assert.Equal("KEYS_PATH", Config.Key.KeysPath);
    Assert.Equal("ENABLE_FIREWALL", Config.Key.EnableFirewall);
    Assert.Equal("ENABLE_PASSWORD_LOGIN", Config.Key.EnablePasswordLogin);
    Assert.Equal("VPC_CIDR", Config.Key.VpcCidr);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestDefaultConfig()
  {
    var config = new Config(Env.Test);

    // domain config
    Assert.Equal(Env.Test, config.Domain.Env);
    Assert.Equal(Config.TestServerName, config.Domain.ServerName);
    Assert.Equal("Host=localhost;User=platform;Password=platform;Database=platform_dev", config.Domain.DatabaseUrl);
    Assert.Null(config.Domain.RedisCacheUrl);
    Assert.Null(config.Domain.GitHubApiToken);

    // web config
    Assert.Equal("localhost", config.Web.Host);
    Assert.Equal(3000, config.Web.Port);
    Assert.Equal(LogLevel.Information, config.Web.LogLevel);
    Assert.Equal(new Uri("http://localhost:3000/"), config.Web.PublicUrl);
    Assert.Equal("wwwroot", config.Web.PublicRoot);
    Assert.Equal("../.keys", config.Web.KeysPath);
    Assert.Equal(Config.DefaultEncryptKey, config.Web.EncryptKey);
    Assert.Equal(Config.DefaultSigningKey, config.Web.SigningKey);
    Assert.Equal(Config.AuthCookieName, config.Web.AuthCookie.Name);
    Assert.Equal("/", config.Web.AuthCookie.Path);
    Assert.True(config.Web.AuthCookie.HttpOnly);
    Assert.Equal("SameAsRequest", config.Web.AuthCookie.Secure.ToString());
    Assert.Equal("Lax", config.Web.AuthCookie.SameSite.ToString());
    Assert.Equal(7, config.Web.AuthCookie.MaxAge.TotalDays);
    Assert.Equal(Config.SessionCookieName, config.Web.SessionCookie.Name);
    Assert.Equal("/", config.Web.SessionCookie.Path);
    Assert.True(config.Web.SessionCookie.HttpOnly);
    Assert.Equal("SameAsRequest", config.Web.SessionCookie.Secure.ToString());
    Assert.Equal("Lax", config.Web.SessionCookie.SameSite.ToString());
    Assert.Equal(7, config.Web.SessionCookie.MaxAge.TotalDays);
    Assert.Null(config.Web.OAuth.GitHub);
    Assert.Null(config.Web.OAuth.Discord);
    Assert.Null(config.Web.SentryEndpoint);
    Assert.Equal(Config.DefaultVpcCidr, config.Web.VpcCidr);

    // mailer config
    Assert.Equal(Config.SandboxPostmarkApiToken, config.Mailer.ApiToken);
    Assert.Equal("Void", config.Mailer.ProductName);
    Assert.Equal("http://localhost:3000/", config.Mailer.ProductUrl.ToString());
    Assert.Equal("support@void.dev", config.Mailer.SupportEmail);

    // filestore config
    Assert.Equal("../.filestore", config.FileStore.Path);
    Assert.Null(config.FileStore.Bucket);

    // enable feature flags
    Assert.True(config.Enable.Firewall);
    Assert.True(config.Enable.PasswordLogin);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestCustomConfig()
  {
    var config = new Config(Env.Test, CustomEnv);

    // domain config
    Assert.Equal(Env.Test, config.Domain.Env);
    Assert.Equal(Config.TestServerName, config.Domain.ServerName);
    Assert.Equal("Host=custom-host;User=custom-user;Password=custom-password;Database=custom-database", config.Domain.DatabaseUrl);
    Assert.Equal("custom-redis-host:6379,defaultDatabase=7", config.Domain.RedisCacheUrl);
    Assert.Equal("custom-github-api-token", config.Domain.GitHubApiToken);

    // web config
    Assert.Equal("0.0.0.0", config.Web.Host);
    Assert.Equal(4321, config.Web.Port);
    Assert.Equal(LogLevel.Debug, config.Web.LogLevel);
    Assert.Equal(new Uri("https://play.void.test/"), config.Web.PublicUrl);
    Assert.Equal("custom-root", config.Web.PublicRoot);
    Assert.Equal("/mnt/keys", config.Web.KeysPath);
    Assert.Equal("custom-encrypt-key", config.Web.EncryptKey);
    Assert.Equal("custom-signing-key", config.Web.SigningKey);
    Assert.Equal(Config.AuthCookieName, config.Web.AuthCookie.Name);
    Assert.Equal("/", config.Web.AuthCookie.Path);
    Assert.True(config.Web.AuthCookie.HttpOnly);
    Assert.Equal("Always", config.Web.AuthCookie.Secure.ToString());
    Assert.Equal("Lax", config.Web.AuthCookie.SameSite.ToString());
    Assert.Equal(7, config.Web.AuthCookie.MaxAge.TotalDays);
    Assert.Equal(Config.SessionCookieName, config.Web.SessionCookie.Name);
    Assert.Equal("/", config.Web.SessionCookie.Path);
    Assert.True(config.Web.SessionCookie.HttpOnly);
    Assert.Equal("Always", config.Web.SessionCookie.Secure.ToString());
    Assert.Equal("Lax", config.Web.SessionCookie.SameSite.ToString());
    Assert.Equal(7, config.Web.SessionCookie.MaxAge.TotalDays);
    Assert.NotNull(config.Web.OAuth.GitHub);
    Assert.NotNull(config.Web.OAuth.Discord);
    Assert.Equal("custom-github-client-id", config.Web.OAuth.GitHub.ClientId);
    Assert.Equal("custom-github-client-secret", config.Web.OAuth.GitHub.ClientSecret);
    Assert.Equal("custom-discord-client-id", config.Web.OAuth.Discord.ClientId);
    Assert.Equal("custom-discord-client-secret", config.Web.OAuth.Discord.ClientSecret);
    Assert.Equal("custom-sentry-endpoint", config.Web.SentryEndpoint);
    Assert.Equal("1.2.3.4/32", config.Web.VpcCidr);

    // mailer config
    Assert.Equal("custom-postmark-api-token", config.Mailer.ApiToken);
    Assert.Equal("Void", config.Mailer.ProductName);
    Assert.Equal("https://play.void.test/", config.Mailer.ProductUrl.ToString());
    Assert.Equal("custom-support-email@void.dev", config.Mailer.SupportEmail);

    // filestore config
    Assert.Equal("custom-filestore-path", config.FileStore.Path);
    Assert.Equal("custom-filestore-bucket", config.FileStore.Bucket);

    // enable feature flags
    Assert.False(config.Enable.Firewall);
    Assert.False(config.Enable.PasswordLogin);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestConfigFromConfigurationBuilder()
  {
    var cfg = new ConfigurationBuilder().AddInMemoryCollection(CustomEnv).Build();

    var config1 = new Config(Env.Test, CustomEnv);
    var config2 = new Config(Env.Test, cfg);

    Assert.Equal(config1, config2);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestEcsIgnoreDummyValue()
  {
    var config = new Config(Env.Test, new ConfigValues
    {
      {"REDIS_CACHE_URL", "IGNORE"},
    });

    Assert.Null(config.Domain.RedisCacheUrl);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestRedacted()
  {
    var cfg = new Config(Env.Test, CustomEnv);

    var redacted = Json.Redact(cfg);

    Assert.Json.Properties([
      "domain",
      "web",
      "mailer",
      "fileStore",
      "enable",
    ], redacted);

    var domain = Assert.Json.Object(redacted["domain"]);
    var web = Assert.Json.Object(redacted["web"]);
    var mailer = Assert.Json.Object(redacted["mailer"]);
    var fileStore = Assert.Json.Object(redacted["fileStore"]);
    var enable = Assert.Json.Object(redacted["enable"]);

    Assert.Json.Properties([
      "env",
      "serverName",
      "databaseUrl",
      "redisCacheUrl",
      "gitHubApiToken",
    ], domain);

    Assert.Json.Equal("Test", domain["env"]);
    Assert.Json.Equal(Config.TestServerName, domain["serverName"]);
    Assert.Json.Equal("[REDACTED]", domain["databaseUrl"]);
    Assert.Json.Equal("[REDACTED]", domain["redisCacheUrl"]);
    Assert.Json.Equal("[REDACTED]", domain["gitHubApiToken"]);

    Assert.Json.Properties([
      "host",
      "port",
      "logLevel",
      "publicUrl",
      "publicRoot",
      "encryptKey",
      "signingKey",
      "authCookie",
      "sessionCookie",
      "oAuth",
      "keysPath",
      "sentryEndpoint",
      "vpcCidr",
    ], web);

    Assert.Json.Equal("0.0.0.0", web["host"]);
    Assert.Json.Equal(4321, web["port"]);
    Assert.Json.Equal("Debug", web["logLevel"]);
    Assert.Json.Equal("https://play.void.test:443", web["publicUrl"]);
    Assert.Json.Equal("custom-root", web["publicRoot"]);
    Assert.Json.Equal("[REDACTED]", web["encryptKey"]);
    Assert.Json.Equal("void-platform-auth", web["authCookie"]?["name"]);
    Assert.Json.Equal("/", web["authCookie"]?["path"]);
    Assert.Json.Equal(true, web["authCookie"]?["httpOnly"]);
    Assert.Json.Equal("Always", web["authCookie"]?["secure"]);
    Assert.Json.Equal("Lax", web["authCookie"]?["sameSite"]);
    Assert.Json.Equal("void-platform-session", web["sessionCookie"]?["name"]);
    Assert.Json.Equal("/", web["sessionCookie"]?["path"]);
    Assert.Json.Equal(true, web["sessionCookie"]?["httpOnly"]);
    Assert.Json.Equal("Always", web["sessionCookie"]?["secure"]);
    Assert.Json.Equal("Lax", web["sessionCookie"]?["sameSite"]);
    Assert.Json.Equal("7.00:00:00", web["sessionCookie"]?["maxAge"]);
    Assert.Json.Equal("custom-github-client-id", web["oAuth"]?["gitHub"]?["clientId"]);
    Assert.Json.Equal("[REDACTED]", web["oAuth"]?["gitHub"]?["clientSecret"]);
    Assert.Json.Equal("custom-discord-client-id", web["oAuth"]?["discord"]?["clientId"]);
    Assert.Json.Equal("[REDACTED]", web["oAuth"]?["discord"]?["clientSecret"]);

    Assert.Json.Properties([
      "apiToken",
      "productName",
      "productUrl",
      "supportEmail",
    ], mailer);

    Assert.Json.Equal("[REDACTED]", mailer["apiToken"]);
    Assert.Json.Equal("Void", mailer["productName"]);
    Assert.Json.Equal("https://play.void.test:443", mailer["productUrl"]);
    Assert.Json.Equal("custom-support-email@void.dev", mailer["supportEmail"]);

    Assert.Json.Properties([
      "path",
      "bucket",
    ], fileStore);

    Assert.Json.Equal("custom-filestore-path", fileStore["path"]);
    Assert.Json.Equal("custom-filestore-bucket", fileStore["bucket"]);

    Assert.Json.Properties([
      "firewall",
      "passwordLogin",
    ], enable);

    Assert.Json.Equal(false, enable["firewall"]);
    Assert.Json.Equal("[REDACTED]", enable["passwordLogin"]); // unnecessary redaction, but fine for now
  }

  //-----------------------------------------------------------------------------------------------
}