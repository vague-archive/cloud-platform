namespace Void.Platform.Domain;

public class SysAdminTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestAllOrganizations()
  {
    using (var test = new DomainTest(this))
    {
      var orgs = test.App.SysAdmin.AllOrganizations();
      Assert.Equal([
        "Aardvark Inc",
        "Atari",
        "Nintendo",
        "Secret",
        "Void",
        "Zoidberg Inc",
      ], orgs.Select(u => u.Name));

      var atari = test.Factory.LoadOrganization("atari");
      Assert.Domain.Equal(atari, orgs[1]);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestRecentBranches()
  {
    using (var test = new DomainTest(this))
    {

      var org1 = test.Factory.CreateOrganization();
      var org2 = test.Factory.CreateOrganization();
      var game1 = test.Factory.CreateGame(org1);
      var game2 = test.Factory.CreateGame(org2);
      var user1 = test.Factory.CreateUser();
      var user2 = test.Factory.CreateUser();

      var branch1 = test.Factory.CreateBranch(game1);
      var branch2 = test.Factory.CreateBranch(game2);
      var branch3 = test.Factory.CreateBranch(game1);
      var branch4 = test.Factory.CreateBranch(game2);

      var deploy1 = test.Factory.CreateDeploy(branch1, user1, deployedOn: Clock.Now.Minus(Duration.FromMinutes(1)));
      var deploy2 = test.Factory.CreateDeploy(branch2, user1, deployedOn: Clock.Now.Minus(Duration.FromHours(1)));
      var deploy3 = test.Factory.CreateDeploy(branch3, user2, deployedOn: Clock.Now.Minus(Duration.FromDays(1)));
      var deploy4 = test.Factory.CreateDeploy(branch4, user2, deployedOn: Clock.Now.Minus(Duration.FromDays(2)));

      var branches = await test.App.SysAdmin.RecentBranches(Duration.FromMinutes(10));
      Assert.Single(branches);
      Assert.Domain.Equal(branch1, branches[0]);
      Assert.Domain.Equal(org1, branches[0].Organization);
      Assert.Domain.Equal(game1, branches[0].Game);
      Assert.Domain.Equal(deploy1, branches[0].LatestDeploy);
      Assert.Domain.Equal(user1, branches[0].LatestDeploy?.DeployedByUser);

      branches = await test.App.SysAdmin.RecentBranches(Duration.FromHours(1));
      Assert.Equal(2, branches.Count);
      Assert.Domain.Equal(branch1, branches[0]);
      Assert.Domain.Equal(branch2, branches[1]);

      branches = await test.App.SysAdmin.RecentBranches(Duration.FromDays(1));
      Assert.Equal(3, branches.Count);
      Assert.Domain.Equal(branch1, branches[0]);
      Assert.Domain.Equal(branch2, branches[1]);
      Assert.Domain.Equal(branch3, branches[2]);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestExpiredBranches()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var cutoff = Duration.FromDays(10);
      var freshDate = Clock.Now.Minus(cutoff - Duration.FromMinutes(1));
      var expiredDate = Clock.Now.Minus(cutoff + Duration.FromMinutes(1));

      var freshBranch = test.Factory.CreateBranch(game, slug: "fresh");
      var expiredBranch = test.Factory.CreateBranch(game, slug: "expired");
      var pinnedBranch = test.Factory.CreateBranch(game, slug: "pinned", isPinned: true);

      var freshDeploy = test.Factory.CreateDeploy(freshBranch, user, deployedOn: freshDate);
      var expiredDeploy = test.Factory.CreateDeploy(expiredBranch, user, deployedOn: expiredDate);
      var pinnedDeploy = test.Factory.CreateDeploy(pinnedBranch, user, deployedOn: expiredDate);

      var expiredBranches = await test.App.SysAdmin.ExpiredBranches(cutoff);
      Assert.Present(expiredBranches);
      Assert.Single(expiredBranches);
      var expired = Assert.Present(expiredBranches[0]);
      Assert.Equal(expiredBranch.Id, expired.Id);
      Assert.Equal(org.Id, expired.Organization?.Id);
      Assert.Equal(game.Id, expired.Game?.Id);
      Assert.Equal(user.Id, expired.ActiveDeploy?.DeployedByUser?.Id);

      expiredBranches = await test.App.SysAdmin.ExpiredBranches(cutoff + Duration.FromDays(1));
      Assert.Present(expiredBranches);
      Assert.Empty(expiredBranches);

      expiredBranches = await test.App.SysAdmin.ExpiredBranches(cutoff - Duration.FromDays(1));
      Assert.Present(expiredBranches);
      Assert.Equal(2, expiredBranches.Count);
      Assert.Equal(freshBranch.Id, expiredBranches[0].Id);
      Assert.Equal(expiredBranch.Id, expiredBranches[1].Id);
    }
  }

  //-----------------------------------------------------------------------------------------------
}