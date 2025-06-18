namespace Void.Platform.Domain;

public class DeployTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  const string PATH = "/path/to/my/game";

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestDeployStateProperties()
  {
    var empty = new Share.Deploy { Path = PATH };
    var deploying = new Share.Deploy { Path = PATH, State = Share.DeployState.Deploying };
    var ready = new Share.Deploy { Path = PATH, State = Share.DeployState.Ready };
    var failed = new Share.Deploy { Path = PATH, State = Share.DeployState.Failed };

    Assert.Equal(Share.DeployState.Deploying, empty.State);
    Assert.True(empty.IsDeploying);
    Assert.False(empty.IsReady);
    Assert.False(empty.HasFailed);

    Assert.Equal(Share.DeployState.Deploying, deploying.State);
    Assert.True(deploying.IsDeploying);
    Assert.False(deploying.IsReady);
    Assert.False(deploying.HasFailed);

    Assert.Equal(Share.DeployState.Ready, ready.State);
    Assert.False(ready.IsDeploying);
    Assert.True(ready.IsReady);
    Assert.False(ready.HasFailed);

    Assert.Equal(Share.DeployState.Failed, failed.State);
    Assert.False(failed.IsDeploying);
    Assert.False(failed.IsReady);
    Assert.True(failed.HasFailed);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestDeployFilePath()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.BuildUser();
      var org = test.Factory.BuildOrganization();
      var game = test.Factory.BuildGame(org);
      var main = test.Factory.BuildBranch(game, slug: "main");
      var demo = test.Factory.BuildBranch(game, slug: "demo");
      var blue = test.Factory.BuildDeploy(main, user, number: 1);
      var green = test.Factory.BuildDeploy(main, user, number: 2);
      var fancy = test.Factory.BuildDeploy(demo, user, number: 1);

      Assert.Equal($"share/{org.Id}/{game.Id}/main/1", blue.Path);
      Assert.Equal($"share/{org.Id}/{game.Id}/main/2", green.Path);
      Assert.Equal($"share/{org.Id}/{game.Id}/demo/1", fancy.Path);

      Assert.Equal($"share/{org.Id}/{game.Id}", Share.DeployPath(game));
      Assert.Equal($"share/{org.Id}/{game.Id}/main", Share.DeployPath(main));
      Assert.Equal($"share/{org.Id}/{game.Id}/demo", Share.DeployPath(demo));
      Assert.Equal($"share/{org.Id}/{game.Id}/main/42", Share.DeployPath(main, 42));
      Assert.Equal($"share/{org.Id}/{game.Id}/demo/99", Share.DeployPath(demo, 99));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestDeployFactory()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var branch = test.Factory.CreateBranch(game);
      var deploy = test.Factory.CreateDeploy(branch, user, path: "/path/to/files");

      Assert.Equal(org.Id, deploy.OrganizationId);
      Assert.Equal(game.Id, deploy.GameId);
      Assert.Equal(Share.DeployState.Ready, deploy.State);
      Assert.Equal(1, deploy.Number);
      Assert.Equal("/path/to/files", deploy.Path);
      Assert.Null(deploy.Error);
      Assert.Null(deploy.FailedOn);
      Assert.Null(deploy.DeployingOn);
      Assert.Null(deploy.DeletedOn);
      Assert.Null(deploy.DeletedReason);
      Assert.Equal(user.Id, deploy.DeployedBy);
      Assert.Null(deploy.DeployedByUser);
      Assert.Equal(Clock.Now, deploy.DeployedOn);
      Assert.Equal(Clock.Now, deploy.CreatedOn);
      Assert.Equal(Clock.Now, deploy.UpdatedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetDeploy()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game);
      var deploy1 = test.Factory.CreateDeploy(branch, user);
      var deploy2 = test.Factory.CreateDeploy(branch, user);

      var reload1a = test.App.Share.GetDeploy(deploy1.Id);
      var reload2a = test.App.Share.GetDeploy(deploy2.Id);

      Assert.Present(reload1a);
      Assert.Present(reload2a);

      Assert.Domain.Equal(deploy1, reload1a);
      Assert.Domain.Equal(deploy2, reload2a);

      Assert.Absent(test.App.Share.GetDeploy(Identify("unknown")));
    }
  }

  //-----------------------------------------------------------------------------------------------
}