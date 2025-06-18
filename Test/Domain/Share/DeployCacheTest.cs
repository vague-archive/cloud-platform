namespace Void.Platform.Domain;

public class DeployCacheTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetCachedDeployInfo()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var branch = test.Factory.CreateBranch(game, slug: "demo");
      var deploy = test.Factory.CreateDeploy(branch, user);

      Assert.False(test.Cache.Contains(CacheKey.ForGameServe("atari", "pong", "demo")));
      Assert.False(test.Cache.Contains(CacheKey.ForGameServe("void", "snakes", "demo")));

      var info = await test.App.Share.GetCachedDeployInfo("atari", "pong", "demo");
      Assert.Present(info);
      Assert.Equal(Account.GamePurpose.Game, info.Purpose);
      Assert.Equal($"share/{org.Id}/{game.Id}/{branch.Slug}/1", info.FilePath);

      Assert.True(test.Cache.Contains(CacheKey.ForGameServe("atari", "pong", "demo")));
      Assert.False(test.Cache.Contains(CacheKey.ForGameServe("void", "snakes", "demo")));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCachedDeployIsInvalidatedWhenNewActiveVersionIsFinalized()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var branch = test.Factory.CreateBranch(game);
      var deploy = test.Factory.CreateDeploy(branch, user);

      var firstPath = $"share/{org.Id}/{game.Id}/{branch.Slug}/1";
      var secondPath = $"share/{org.Id}/{game.Id}/{branch.Slug}/2";

      // preconditions
      Assert.Equal(firstPath, deploy.Path);

      // verify initial cached path (is blue)
      var info = await test.App.Share.GetCachedDeployInfo(org.Slug, game.Slug, branch.Slug);
      Assert.Equal(firstPath, info?.FilePath);

      // start a new deploy
      using var archive = await test.FileStore.CreateTestArchive("build.tgz");
      var result = await test.App.Share.FullDeploy(new Share.FullDeployCommand
      {
        Archive = archive,
        DeployedBy = user,
        Organization = org,
        Game = game,
        Slug = branch.Slug,
      });

      // verify the actual path has changed (to green)
      deploy = Assert.Succeeded(result);
      Assert.Equal(secondPath, deploy.Path);

      // verify the cached path has also changed (to green)
      info = await test.App.Share.GetCachedDeployInfo(org.Slug, game.Slug, branch.Slug);
      Assert.Equal(secondPath, info?.FilePath);
    }
  }

  //-----------------------------------------------------------------------------------------------
}