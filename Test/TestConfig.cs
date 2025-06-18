namespace Void.Platform.Test;

public static class TestConfig
{
  public static string Host = "localhost";
  public static int Port = 4242;
  public static string EnvironmentName = "test";
  public static string ProductName = "Void Test";
  public static string UrlScheme = "https";
  public static string UrlHost = "void.test";
  public static Uri PublicUrl = new Uri($"{UrlScheme}://{UrlHost}:{UrlPort}/");
  public static LogLevel LogLevel = TestLogLevel(LogLevel.Fatal);
  public static int UrlPort = 443;
  public static string SupportEmail = "test-support@void.dev";
  public static string GitHubClientId = "test-github-client-id";
  public static string GitHubClientSecret = "test-github-client-secret";
  public static string GitHubApiToken = "test-github-api-token";
  public static string DiscordClientId = "test-discord-client-id";
  public static string DiscordClientSecret = "test-discord-client-secret";
  public static string AuthenticationScheme = "test";

  public static MailerConfig MailerConfig = new MailerConfig
  {
    ProductName = ProductName,
    ProductUrl = PublicUrl,
    SupportEmail = SupportEmail,
  };

  public static string[] CommandLineArgs = new[] {
    $"ENVIRONMENT={EnvironmentName}",
    $"LOG_LEVEL={LogLevel}",
    $"HOST={Host}",
    $"PORT={Port}",
    $"URL_SCHEME={UrlScheme}",
    $"URL_HOST={UrlHost}",
    $"URL_PORT={UrlPort}",
    $"GITHUB_CLIENT_ID={GitHubClientId}",
    $"GITHUB_CLIENT_SECRET={GitHubClientSecret}",
    $"GITHUB_API_TOKEN={GitHubApiToken}",
    $"DISCORD_CLIENT_ID={DiscordClientId}",
    $"DISCORD_CLIENT_SECRET={DiscordClientSecret}",
  };

  private static LogLevel TestLogLevel(LogLevel defaultValue) // allow log level to be overridden in tests for debugging purposes
  {
    var value = Environment.GetEnvironmentVariable(Web.Config.Key.LogLevel);
    if (value is not null)
      return value.ToEnum<LogLevel>();
    return defaultValue;
  }
}