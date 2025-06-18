namespace Void.Platform.Domain;

//-------------------------------------------------------------------------------------------------

public enum Env
{
  Production,
  Development,
  Test,
}

public record Config
{
  public required Env Env { get; init; }
  public required string ServerName { get; init; }
  public required string DatabaseUrl { get; init; }
  public string? RedisCacheUrl { get; init; }
  public string? GitHubApiToken { get; set; }
}

//-------------------------------------------------------------------------------------------------

public class Application
{
  // dependencies
  public Config Config { get; init; }
  public IClock Clock { get; init; }
  public IRandom Random { get; init; }
  public IDatabase Db { get; init; }
  public ICache Cache { get; init; }
  public IMailer Mailer { get; init; }
  public ILogger Logger { get; init; }
  public IFileStore FileStore { get; init; }
  public IMinions Minions { get; init; }
  public IHttpClientFactory HttpClientFactory { get; init; }
  public Crypto.Encryptor Encryptor { get; init; }
  public Crypto.PasswordHasher PasswordHasher { get; init; }
  public Crypto.JwtGenerator JwtGenerator { get; init; }

  // sub-domains
  public Account Account { get; init; }
  public Share Share { get; init; }
  public Content Content { get; init; }
  public Downloads Downloads { get; init; }
  public SysAdmin SysAdmin { get; init; }

  // DI constructor
  public Application(
    Config config,
    IClock clock,
    IRandom random,
    IDatabase db,
    ICache cache,
    IMailer mailer,
    ILogger logger,
    IFileStore fileStore,
    IMinions minions,
    IHttpClientFactory httpClientFactory,
    Crypto.Encryptor encryptor,
    Crypto.PasswordHasher passwordHasher,
    Crypto.JwtGenerator jwtGenerator)
  {
    Config = config;
    Clock = clock;
    Random = random;
    Db = db;
    Cache = cache;
    Mailer = mailer;
    Logger = logger;
    FileStore = fileStore;
    Minions = minions;
    Encryptor = encryptor;
    PasswordHasher = passwordHasher;
    JwtGenerator = jwtGenerator;
    HttpClientFactory = httpClientFactory;
    Account = new Account(this);
    Share = new Share(this);
    Content = new Content(this);
    Downloads = new Downloads(this);
    SysAdmin = new SysAdmin(this);
  }

  public bool GitHubDisabled { get { return Config.GitHubApiToken is null; } }
  public bool GitHubEnabled { get { return Config.GitHubApiToken is not null; } }
}

//-------------------------------------------------------------------------------------------------

public abstract class SubDomain
{
  public Application App { get; init; }

  public SubDomain(Application app)
  {
    App = app;
  }

  protected Instant Now
  {
    get
    {
      return App.Clock.Now;
    }
  }

  protected IClock Clock
  {
    get
    {
      return App.Clock;
    }
  }

  protected IRandom Random
  {
    get
    {
      return App.Random;
    }
  }

  protected IDatabase Db
  {
    get
    {
      return App.Db;
    }
  }

  protected ICache Cache
  {
    get
    {
      return App.Cache;
    }
  }

  protected IMailer Mailer
  {
    get
    {
      return App.Mailer;
    }
  }

  protected ILogger Logger
  {
    get
    {
      return App.Logger;
    }
  }

  protected IFileStore FileStore
  {
    get
    {
      return App.FileStore;
    }
  }

  protected IMinions Minions
  {
    get
    {
      return App.Minions;
    }
  }

  protected Crypto.Encryptor Encryptor
  {
    get
    {
      return App.Encryptor;
    }
  }

  protected Crypto.PasswordHasher PasswordHasher
  {
    get
    {
      return App.PasswordHasher;
    }
  }

  protected Crypto.JwtGenerator JwtGenerator
  {
    get
    {
      return App.JwtGenerator;
    }
  }

  protected IHttpClientFactory HttpClientFactory
  {
    get
    {
      return App.HttpClientFactory;
    }
  }
}

//-------------------------------------------------------------------------------------------------