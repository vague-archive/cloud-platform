namespace Void.Platform.Fixture;

public class Fake
{
  public Bogus.Faker Faker;

  public Fake()
  {
    Faker = new Bogus.Faker("en");
  }

  public Bogus.Randomizer Random
  {
    get
    {
      return Faker.Random;
    }
  }

  public string Identifier()
  {
    return Faker.Random.Uuid().ToString();
  }

  public string Token()
  {
    return Faker.Random.Hash();
  }

  public string CompanyName()
  {
    return Faker.Company.CompanyName();
  }

  public string ProductName()
  {
    return Faker.Commerce.ProductName();
  }

  public string GameName()
  {
    return Faker.Commerce.ProductName();
  }

  public string Description()
  {
    return Faker.Commerce.ProductDescription();
  }

  public string FullName()
  {
    return Faker.Name.FullName();
  }

  public string UserName()
  {
    return Faker.Internet.UserName();
  }

  public string Label()
  {
    return Faker.Random.Words(3);
  }

  public string Slug()
  {
    return Format.Slugify(Label());
  }

  public string Email()
  {
    return Faker.Internet.Email();
  }

  public string TimeZone()
  {
    return Faker.PickRandom(International.TimeZoneIds);
  }

  public string Locale()
  {
    return Faker.PickRandom(International.Locales);
  }

  public Account.IdentityProvider IdentityProvider()
  {
    return Faker.PickRandom<Account.IdentityProvider>();
  }

  public string Url()
  {
    return Faker.Internet.Url();
  }

  public string FileName()
  {
    return Faker.System.FileName();
  }

  public string FilePath()
  {
    return Faker.System.FilePath();
  }

  public string Version()
  {
    return Faker.System.Version().ToString();
  }

  public string ContentType()
  {
    return Faker.PickRandom(new string[]
    {
      Http.ContentType.Json,
      Http.ContentType.Text,
      Http.ContentType.Form,
      Http.ContentType.Javascript,
      Http.ContentType.Css,
      Http.ContentType.Bytes,
      Http.ContentType.Html,
      Http.ContentType.Wasm,
    });
  }

  public Instant RecentDateUtc()
  {
    var dt = DateTime.SpecifyKind(Faker.Date.Recent(), DateTimeKind.Utc);
    return Instant.FromDateTimeUtc(dt);
  }

  public string EncryptKey()
  {
    return Crypto.GenerateToken();
  }

  public string SigningKey()
  {
    return Crypto.GenerateToken();
  }

  public string AwsAccessKeyId
  {
    get
    {
      return "fake-aws-access-key-id";
    }
  }

  public string AwsSecretKey
  {
    get
    {
      return "fake-aws-secret-key";
    }
  }
}