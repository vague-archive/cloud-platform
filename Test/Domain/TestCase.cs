namespace Void.Platform.Test;

//=================================================================================================
//
// Summary:
//   context for domain layer tests
//     - starts an IDbConnection within a TransactionScope (which rolls back at end of test)
//     - configures the domain to use the transactional IDbConnection and our fake IClock
//     - configures a domain factory also using the transactional IDbConnection and our fake IClock
//     - provides domain layer test helpers like Identify()
//
//=================================================================================================

public class DomainTest : IDisposable
{
  public TestLogger Logger { get; init; }
  public TestClock Clock { get; init; }
  public TestRandom Random { get; init; }
  public Fake Fake { get; init; }
  public Database Db { get; init; }
  public Cache Cache { get; init; }
  public SandboxMailer Mailer { get; init; }
  public TestFileStore FileStore { get; init; }
  public TestMinions Minions { get; init; }
  public Crypto.Encryptor Encryptor { get; init; }
  public Crypto.PasswordHasher PasswordHasher { get; init; }
  public Crypto.JwtGenerator JwtGenerator { get; init; }
  public MockHttpClientFactory HttpClientFactory { get; init; }
  public Application App { get; init; }
  public FixtureFactory Factory { get; init; }
  public CancellationToken CancelToken { get; init; }

  public DomainTest(TestCase test)
  {
    Logger = test.Logger;
    Clock = test.Clock;
    Random = test.Random;
    Fake = test.Fake;
    Db = test.BuildTestDatabase(Logger);
    Cache = test.BuildTestCache();
    Mailer = test.BuildTestMailer();
    FileStore = test.BuildTestFileStore();
    Minions = test.BuildTestMinions();
    Encryptor = test.BuildTestEncryptor();
    PasswordHasher = test.BuildTestPasswordHasher();
    JwtGenerator = test.BuildTestJwtGenerator();
    HttpClientFactory = test.BuildTestHttpClientFactory();
    Factory = test.BuildTestFixtureFactory(Db);
    CancelToken = test.CancelToken;

    App = new Application(
      new Domain.Config   // ensure each test gets is own unique copy so mutations dont leak
      {
        Env = Env.Test,
        ServerName = Web.Config.TestServerName,
        DatabaseUrl = Web.Config.TestDatabaseUrl,
        GitHubApiToken = TestConfig.GitHubApiToken,
      },
      Clock,
      Random,
      Db,
      Cache,
      Mailer,
      Logger,
      FileStore,
      Minions,
      HttpClientFactory,
      Encryptor,
      PasswordHasher,
      JwtGenerator
    );
  }

  private bool disposed = false;
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  public virtual void Dispose(bool disposing)
  {
    if (disposed)
      return;

    if (disposing)
    {
      Db.Dispose();
      FileStore.Dispose();
    }
    disposed = true;
  }

  ~DomainTest()
  {
    Dispose(false);
  }

  //===============================================================================================
  // HELPER PROPERTIES and METHODS
  //===============================================================================================

  public MockHttpMessageHandler HttpHandler
  {
    get
    {
      return HttpClientFactory.Handler;
    }
  }

  public ValueTask SeedCache<T>(string key, T value)
  {
    return Cache.SetAsync<T>(key, value, token: CancelToken);
  }
}