namespace Void.Platform.Domain;

public class CacheTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestCacheKeyGenerator()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.BuildUser();
      var org = test.Factory.BuildOrganization(id: 100);
      var game = test.Factory.BuildGame(org, id: 200);
      var branch = test.Factory.BuildBranch(game);

      Assert.Equal("purpose:foo:bar:baz", CacheKey.For("purpose", "foo", "bar", "baz"));
      Assert.Equal("purpose:1:2:3", CacheKey.For("purpose", 1, 2, 3));
      Assert.Equal("purpose:foo:42", CacheKey.For("purpose", "foo", 42));

      Assert.Equal($"download:releases:editor", CacheKey.ForDownloads("editor"));
      Assert.Equal($"game:serve:atari:pong:demo", CacheKey.ForGameServe("atari", "pong", "demo"));
      Assert.Equal($"game:serve:void:snakes:latest", CacheKey.ForGameServe("void", "snakes", "latest"));
      Assert.Equal($"game:serve:password:{branch.Id}", CacheKey.ForGameServePassword(branch));
      Assert.Equal($"sysadmin:stats:database", CacheKey.ForSysAdminDatabaseStats());
      Assert.Equal($"sysadmin:stats:files", CacheKey.ForSysAdminFileStats());

      Assert.Equal($"trash:safe:share/1/8/main/1", CacheKey.SafeTrash("share/1/8/main/1"));
      Assert.Equal($"trash:unsafe:share/1/8/main", CacheKey.UnsafeTrash("share/1/8/main"));

      Assert.Equal($"firewall:blocked:1.2.3.4", CacheKey.FirewallBlocked("1.2.3.4"));
      Assert.Equal($"firewall:blocked:10.10.10.10", CacheKey.FirewallBlocked("10.10.10.10"));
    }
  }

  //-----------------------------------------------------------------------------------------------
}