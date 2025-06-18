namespace Void.Platform.Domain;

public class DatabaseTest : TestCase
{
  [Fact]
  public void TestNodaTimeInstantHandler()
  {
    using (var test = new DomainTest(this))
    {
      // verify NodaTime.Instant round trips to our DB mysql timestamp(3) column correctly...

      var instant = Instant.FromUtc(2021, 1, 2, 10, 30, 45).PlusNanoseconds(123456789);
      var user = test.Factory.CreateUser(id: 1, createdOn: instant, updatedOn: instant);

      Assert.Equal(instant.TruncateToMilliseconds(), user.CreatedOn);
      Assert.Equal(instant.TruncateToMilliseconds(), user.UpdatedOn);
      Assert.Equal("2021-01-02T10:30:45.123Z", user.CreatedOn.ToIso8601());
      Assert.Equal("2021-01-02T10:30:45.123Z", user.UpdatedOn.ToIso8601());

      var reloaded = test.App.Account.GetUserById(1);

      Assert.NotNull(reloaded);
      Assert.Equal(user.Id, reloaded.Id);
      Assert.Equal(user.CreatedOn, reloaded.CreatedOn);
      Assert.Equal(user.UpdatedOn, reloaded.UpdatedOn);
    }
  }
}