namespace Void.Platform.Domain;

public class BranchTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  const string PASSWORD = "you shall not pass!";

  //===============================================================================================
  // TEST BRANCH FIXTURE FACTORY
  //===============================================================================================

  [Fact]
  public void TestBranchFixtureFactory()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var branch = test.Factory.CreateBranch(game,
        id: "my-branch",
        slug: "my-branch"
      );

      Assert.Equal(Identify("my-branch"), branch.Id);
      Assert.Equal(org.Id, branch.OrganizationId);
      Assert.Equal(game.Id, branch.GameId);
      Assert.Equal("my-branch", branch.Slug);
      Assert.Null(branch.Password);
      Assert.False(branch.IsPinned);
      Assert.Equal(Clock.Now, branch.CreatedOn);
      Assert.Equal(Clock.Now, branch.UpdatedOn);
    }
  }

  //===============================================================================================
  // TEST BRANCH PASSWORD PROPERTIES
  //===============================================================================================

  [Fact]
  public void TestBranchPassword()
  {
    var password = "Shhh, it's a secret";
    var encryptor = BuildTestEncryptor();
    var empty = new Share.Branch { Slug = "branch-without-password" };
    var secret1 = new Share.Branch(password, encryptor) { Slug = "branch-with-password-that-has-already-been-decrypted" };
    var secret2 = new Share.Branch { EncryptedPassword = encryptor.Encrypt(password), Slug = "branch-with-password-that-hasn't-been-decrypted-yet" };

    Assert.False(empty.HasPassword);
    Assert.Null(empty.Password);
    empty.SetPassword(password, encryptor);
    Assert.True(empty.HasPassword);
    Assert.Equal(password, empty.Password);

    Assert.True(secret1.HasPassword);
    Assert.Equal(password, secret1.Password);

    Assert.True(secret2.HasPassword);
    var ex = Assert.Throws<Exception>(() => secret2.Password);
    Assert.Equal("must call DecryptPassword() first", ex.Message);
    Assert.Equal(password, secret2.DecryptPassword(encryptor));
    Assert.Equal(password, secret2.Password);
  }

  //===============================================================================================
  // TEST GET BRANCHES
  //===============================================================================================

  [Fact]
  public void TestGetBranch()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game1 = test.Factory.CreateGame(org);
      var game2 = test.Factory.CreateGame(org);
      var branch1a = test.Factory.CreateBranch(game1, id: 101, slug: "1a");
      var branch2a = test.Factory.CreateBranch(game2, id: 200, slug: "2a");
      var active1a = test.Factory.CreateDeploy(branch1a, user, asActive: true, asLatest: false);
      var latest1a = test.Factory.CreateDeploy(branch1a, user, asActive: false, asLatest: true);

      var reload1a = test.App.Share.GetBranch(branch1a.Id);
      var reload2a = test.App.Share.GetBranch(branch2a.Id);

      Assert.Present(reload1a);
      Assert.Present(reload2a);

      Assert.Domain.Equal(branch1a, reload1a);
      Assert.Domain.Equal(branch2a, reload2a);

      Assert.Domain.Equal(active1a, reload1a.ActiveDeploy);
      Assert.Domain.Equal(latest1a, reload1a.LatestDeploy);
      Assert.Null(reload2a.ActiveDeploy);
      Assert.Null(reload2a.LatestDeploy);

      Assert.Absent(test.App.Share.GetBranch(Identify("unknown")));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetBranchBySlug()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game1 = test.Factory.CreateGame(org);
      var game2 = test.Factory.CreateGame(org);
      var branch1a = test.Factory.CreateBranch(game1, id: 101, slug: "1a");
      var branch2a = test.Factory.CreateBranch(game2, id: 200, slug: "2a");

      var active1a = test.Factory.CreateDeploy(branch1a, user, asActive: true, asLatest: false);
      var latest1a = test.Factory.CreateDeploy(branch1a, user, asActive: false, asLatest: true);

      var reload1a = test.App.Share.GetBranch(game1, "1a");
      var reload2a = test.App.Share.GetBranch(game2, "2a");

      Assert.Present(reload1a);
      Assert.Present(reload2a);

      Assert.Domain.Equal(branch1a, reload1a);
      Assert.Domain.Equal(branch2a, reload2a);

      Assert.Domain.Equal(active1a, reload1a.ActiveDeploy);
      Assert.Domain.Equal(latest1a, reload1a.LatestDeploy);
      Assert.Null(reload2a.ActiveDeploy);
      Assert.Null(reload2a.LatestDeploy);

      Assert.Absent(test.App.Share.GetBranch(game1, "2a"));
      Assert.Absent(test.App.Share.GetBranch(game2, "1a"));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetActiveBranchesForGame()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var main = test.Factory.CreateBranch(game, id: 1, slug: "main");
      var demo = test.Factory.CreateBranch(game, id: 2, slug: "demo");

      var mainDeploy = test.Factory.CreateDeploy(main, user, asActive: false, asLatest: false);
      var mainActive = test.Factory.CreateDeploy(main, user, asActive: true, asLatest: false);
      var mainLatest = test.Factory.CreateDeploy(main, user, asActive: false, asLatest: true);

      var demoDeploy = test.Factory.CreateDeploy(demo, user, asActive: false, asLatest: false);
      var demoActive = test.Factory.CreateDeploy(demo, user, asActive: true, asLatest: false);
      var demoLatest = test.Factory.CreateDeploy(demo, user, asActive: false, asLatest: true);

      var branches = test.App.Share.GetActiveBranchesForGame(game);
      Assert.Equal(2, branches.Count);
      var branch1 = Assert.Present(branches[0]);
      var branch2 = Assert.Present(branches[1]);

      Assert.Domain.Equal(demo, branch1);
      Assert.Domain.Equal(demoActive, branch1.ActiveDeploy);
      Assert.Domain.Equal(demoLatest, branch1.LatestDeploy);

      Assert.Domain.Equal(main, branch2);
      Assert.Domain.Equal(mainActive, branch2.ActiveDeploy);
      Assert.Domain.Equal(mainLatest, branch2.LatestDeploy);
    }
  }

  //===============================================================================================
  // TEST UPDATES
  //===============================================================================================

  [Fact]
  public void TestPinBranch()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game);
      Assert.False(branch.IsPinned);

      branch = Assert.Succeeded(test.App.Share.PinBranch(branch));
      Assert.True(branch.IsPinned);

      branch = Assert.Succeeded(test.App.Share.UnpinBranch(branch));
      Assert.False(branch.IsPinned);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestSetAndClearBranchPassword()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game);

      Assert.False(branch.HasPassword);
      Assert.Null(branch.Password);

      branch = Assert.Succeeded(test.App.Share.SetBranchPassword(branch, PASSWORD));

      Assert.True(branch.HasPassword);
      Assert.Equal(PASSWORD, branch.Password);

      var reloaded = test.Factory.LoadBranch(branch.Id);
      Assert.Present(reloaded);
      Assert.True(branch.HasPassword);
      Assert.Equal(PASSWORD, branch.Password);

      branch = Assert.Succeeded(test.App.Share.ClearBranchPassword(branch));

      Assert.False(branch.HasPassword);
      Assert.Null(branch.Password);
    }
  }

  //===============================================================================================
  // TEST DELETE BRANCH
  //===============================================================================================

  [Fact]
  public void TestDeleteBranch()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var user = test.Factory.CreateUser();
      var branch1 = test.Factory.CreateBranch(game, id: 1);
      var branch2 = test.Factory.CreateBranch(game, id: 2);
      var branch3 = test.Factory.CreateBranch(game, id: 3);

      var deploy = test.Factory.CreateDeploy(branch2, user, asActive: true, asLatest: true);

      test.App.Share.DeleteBranch(branch2);

      var reloaded1 = test.App.Share.GetBranch(branch1.Id);
      var reloaded2 = test.App.Share.GetBranch(branch2.Id);
      var reloaded3 = test.App.Share.GetBranch(branch3.Id);

      Assert.Present(reloaded1);
      Assert.Absent(reloaded2);
      Assert.Present(reloaded3);

      var reloadDeploy = test.App.Share.GetDeploy(deploy.Id);
      Assert.Absent(reloadDeploy);

      var job = Assert.Domain.Enqueued<MoveToTrashMinion, MoveToTrashMinion.Data>(test);
      Assert.Equal(Share.DeployPath(branch2), job.Path);
      Assert.Equal("branch deleted", job.Reason);
    }
  }

  //===============================================================================================
  // TEST BRANCH DB CONSTRAINTS
  //===============================================================================================

  [Fact]
  public void TestBranchSlugUniquenessConstraint()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var game1 = test.Factory.CreateGame(org);
      var game2 = test.Factory.CreateGame(org);
      var branch1a = test.Factory.CreateBranch(game1, slug: "aaa");
      var branch2a = test.Factory.CreateBranch(game2, slug: "aaa");

      var ex = Assert.Throws<MySqlConnector.MySqlException>(() => test.Factory.CreateBranch(game1, slug: "aaa"));
      Assert.Equal($"Duplicate entry '{game1.Id}-aaa' for key 'branches.branches_game_slug_index'", ex.Message);
    }
  }

  //-----------------------------------------------------------------------------------------------
}