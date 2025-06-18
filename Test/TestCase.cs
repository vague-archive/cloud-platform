namespace Void.Platform.Test;

//=================================================================================================
//
// Summary:
//   Base class for ALL tests
//     - provide a fixed IClock to allow time travel in tests
//     - provide a data Faker class
//     - other minor convenience methods
//
//=================================================================================================

public class TestCase
{
  //-----------------------------------------------------------------------------------------------

  public TestLogger Logger { get; init; }
  public TestClock Clock { get; init; }
  public TestRandom Random { get; init; }
  public Fake Fake { get; init; }

  //-----------------------------------------------------------------------------------------------

  public TestCase()
  {
    Logger = new TestLogger(TestContext.Current.TestOutputHelper);
    Clock = new TestClock();
    Random = new TestRandom();
    Fake = new Fake();
  }

  //-----------------------------------------------------------------------------------------------

  protected long Identify(string id)
  {
    return FixtureFactory.Identify(id);
  }

  //-----------------------------------------------------------------------------------------------

  public CancellationToken CancelToken
  {
    get
    {
      return TestContext.Current.CancellationToken;
    }
  }

  //===============================================================================================
  // BUILDERS FOR THINGS YOU MIGHT FIND USEFUL IN DOMAIN OR WEB TESTS
  //===============================================================================================

  public Database BuildTestDatabase(ILogger logger)
  {
    return Database.Transactional(logger, Void.Platform.Web.Config.TestDatabaseUrl, IsolationLevel.ReadCommitted);
  }

  public TestFileStore BuildTestFileStore()
  {
    return TestFileStore.New(Clock, Random, Logger);
  }

  public Cache BuildTestCache()
  {
    return new Cache();
  }

  public SandboxMailer BuildTestMailer()
  {
    return new SandboxMailer(TestConfig.MailerConfig, Logger);
  }

  public TestMinions BuildTestMinions()
  {
    return new TestMinions();
  }

  public Crypto.Encryptor BuildTestEncryptor()
  {
    return new Crypto.Encryptor(FixtureFactory.EncryptKey);
  }

  public Crypto.PasswordHasher BuildTestPasswordHasher()
  {
    return new Crypto.PasswordHasher();
  }

  public Crypto.JwtGenerator BuildTestJwtGenerator()
  {
    return new Crypto.JwtGenerator(FixtureFactory.SigningKey, Clock);
  }

  public MockHttpClientFactory BuildTestHttpClientFactory()
  {
    return new MockHttpClientFactory();
  }

  public FixtureFactory BuildTestFixtureFactory(Database db)
  {
    return new FixtureFactory(db, Clock);
  }

  //-----------------------------------------------------------------------------------------------
}