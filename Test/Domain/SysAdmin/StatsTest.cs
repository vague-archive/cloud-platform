namespace Void.Platform.Domain;

public class SysAdminStatsTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetDatabaseStats()
  {
    using (var test = new DomainTest(this))
    {
      var stats = await test.App.SysAdmin.RefreshDatabaseStats();
      Assert.Equal(6, stats.Organizations);
      Assert.Equal(10, stats.Users);
      Assert.Equal(14, stats.Tokens);
      Assert.Equal(10, stats.Games);
      Assert.Equal(0, stats.Branches);
      Assert.Equal(0, stats.Deploys);
      Assert.Equal(5, stats.Tools);
      Assert.Equal(Clock.Now, stats.CalculatedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------
}