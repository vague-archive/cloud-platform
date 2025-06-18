namespace Void.Platform.Domain;

public class GameTest : TestCase
{
  //===============================================================================================
  // TEST GAME FIXTURE FACTORY
  //===============================================================================================

  [Fact]
  public void TestGameFactory()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.CreateGame(org,
        id: "my-game",
        purpose: Account.GamePurpose.Game,
        name: "My Game",
        slug: "my-game",
        description: "My Great Game",
        isArchived: true,
        archivedOn: Clock.Now.Minus(Duration.FromDays(1)),
        createdOn: Clock.Now.Minus(Duration.FromDays(2)),
        updatedOn: Clock.Now.Minus(Duration.FromDays(3))
      );

      Assert.Equal(Identify("my-game"), game.Id);
      Assert.Equal(org.Id, game.OrganizationId);
      Assert.Equal(Account.GamePurpose.Game, game.Purpose);
      Assert.Equal("My Game", game.Name);
      Assert.Equal("my-game", game.Slug);
      Assert.Equal("My Great Game", game.Description);
      Assert.True(game.IsArchived);
      Assert.Equal(Clock.Now.Minus(Duration.FromDays(1)), game.ArchivedOn);
      Assert.Equal(Clock.Now.Minus(Duration.FromDays(2)), game.CreatedOn);
      Assert.Equal(Clock.Now.Minus(Duration.FromDays(3)), game.UpdatedOn);

      game = test.Factory.BuildGame(org);
      Assert.False(game.IsArchived);
      Assert.Null(game.ArchivedOn);
      Assert.Equal(Clock.Now, game.CreatedOn);
      Assert.Equal(Clock.Now, game.UpdatedOn);

      game = test.Factory.BuildGame(org, archivedOn: Clock.Now);
      Assert.True(game.IsArchived);
      Assert.Equal(Clock.Now, game.ArchivedOn);

      game = test.Factory.BuildGame(org, name: "My Game Name");
      Assert.Equal("my-game-name", game.Slug);
    }
  }

  //===============================================================================================
  // TEST GET GAMES
  //===============================================================================================

  [Fact]
  public void TestGetGameByIdOrSlug()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var game1 = test.Factory.CreateGame(org, id: 100, slug: "first");
      var game2 = test.Factory.CreateGame(org, id: 200, slug: "second");

      var reload1 = test.App.Account.GetGame(100);
      var reload2 = test.App.Account.GetGame(200);
      var reload3 = test.App.Account.GetGame(Identify("unknown"));

      Assert.Present(reload1);
      Assert.Present(reload2);
      Assert.Absent(reload3);

      Assert.Domain.Equal(game1, reload1);
      Assert.Domain.Equal(game2, reload2);

      reload1 = test.App.Account.GetGame(org, "first");
      reload2 = test.App.Account.GetGame(org, "SECOND");
      reload3 = test.App.Account.GetGame(org, "unknown");

      Assert.Present(reload1);
      Assert.Present(reload2);
      Assert.Absent(reload3);

      Assert.Domain.Equal(game1, reload1);
      Assert.Domain.Equal(game2, reload2);

      var otherOrg = test.Factory.CreateOrganization();
      reload1 = test.App.Account.GetGame(otherOrg, "first");
      reload2 = test.App.Account.GetGame(otherOrg, "SECOND");
      reload3 = test.App.Account.GetGame(otherOrg, "unknown");

      Assert.Absent(reload1);
      Assert.Absent(reload2);
      Assert.Absent(reload3);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetGamesForOrganization()
  {
    using (var test = new DomainTest(this))
    {
      var org1 = test.Factory.CreateOrganization();
      var org2 = test.Factory.CreateOrganization();

      test.Factory.CreateGame(org1, name: "CCC");
      test.Factory.CreateGame(org1, name: "AAA", purpose: Account.GamePurpose.Game);
      test.Factory.CreateGame(org1, name: "BBB", purpose: Account.GamePurpose.Tool);
      test.Factory.CreateGame(org2, name: "XXX");

      var games = test.App.Account.GetGamesForOrganization(org1);

      Assert.Equal([
        "AAA",
        "BBB",
        "CCC",
      ], games.Select(g => g.Name));

      games = test.App.Account.GetGamesForOrganization(org2);
      Assert.Equal([
        "XXX"
      ], games.Select(g => g.Name));

      games = test.App.Account.GetGamesForOrganization(test.Factory.LoadOrganization("atari"));
      Assert.Equal([
        "Asteroids",
        "E.T. the Extra-Terrestrial",
        "Pitfall",
        "Pong",
        "Retro Tool",
      ], games.Select(g => g.Name));

      games = test.App.Account.GetGamesForOrganization(test.Factory.LoadOrganization("nintendo"));
      Assert.Equal([
        "Donkey Kong",
        "Star Tool",
      ], games.Select(g => g.Name));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetPublicTools()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();

      var voidx = test.Factory.LoadOrganization("void");
      var atari = test.Factory.LoadOrganization("atari");
      var nintendo = test.Factory.LoadOrganization("nintendo");

      var magic = test.Factory.LoadGame(voidx, "magic-tool");
      var retro = test.Factory.LoadGame(atari, "retro-tool");
      var share = test.Factory.LoadGame(voidx, "share-tool");
      var star = test.Factory.LoadGame(nintendo, "star-tool");

      var day1 = Moment.From(2024, 1, 1);
      var day2 = Moment.From(2024, 1, 2);

      var magic1Branch = test.Factory.CreateBranch(magic, slug: "magic1");
      var magic2Branch = test.Factory.CreateBranch(magic, slug: "magic2");
      var retroBranch = test.Factory.CreateBranch(retro, slug: "retro");
      var shareBranch = test.Factory.CreateBranch(share, slug: "share");
      var starBranch = test.Factory.CreateBranch(star, slug: "star");

      var magic1Active = test.Factory.CreateDeploy(magic1Branch, user, id: 111, path: "/path/magic1/1", deployedOn: day1);
      var magic2Active = test.Factory.CreateDeploy(magic2Branch, user, id: 222, path: "/path/magic2/1", deployedOn: day2);
      var retroActive = test.Factory.CreateDeploy(retroBranch, user, id: 444, path: "/path/retro");
      var shareLatest = test.Factory.CreateDeploy(shareBranch, user, id: 555, path: "/path/share", asActive: false);

      var tools = test.App.Account.GetPublicTools();
      Assert.Equal(4, tools.Count);

      var tool1 = tools[0];
      var tool2 = tools[1];
      var tool3 = tools[2];
      var tool4 = tools[3];

      Assert.Equal(magic.Id, tool1.Id);
      Assert.Equal(retro.Id, tool2.Id);
      Assert.Equal(share.Id, tool3.Id);
      Assert.Equal(star.Id, tool4.Id);

      Assert.Equal("Magic Tool", tool1.Name);
      Assert.Equal("Retro Tool", tool2.Name);
      Assert.Equal("Share Tool", tool3.Name);
      Assert.Equal("Star Tool", tool4.Name);

      Assert.Present(tool1.Organization);
      Assert.Present(tool2.Organization);
      Assert.Present(tool3.Organization);
      Assert.Present(tool4.Organization);

      Assert.Equal("Void", tool1.Organization.Name);
      Assert.Equal("Atari", tool2.Organization.Name);
      Assert.Equal("Void", tool3.Organization.Name);
      Assert.Equal("Nintendo", tool4.Organization.Name);

      Assert.Present(tool1.Branches);
      Assert.Present(tool2.Branches);
      Assert.Present(tool3.Branches);
      Assert.Present(tool4.Branches);

      Assert.Equal(["magic2", "magic1"], tool1.Branches.Select(b => b.Slug)); // magic tool has 2 branches with active deploys on each
      Assert.Equal(["retro"], tool2.Branches.Select(b => b.Slug)); // retro tool has 1 branch with an active tool
      Assert.Equal([], tool3.Branches.Select(b => b.Slug)); // share tool has 1 branch with a deploy that is not yet active
      Assert.Equal([], tool4.Branches.Select(b => b.Slug)); // star tool has 1 branch but no deploys

      Assert.Equal(magic2Active.Id, tool1.Branches[0].ActiveDeployId);
      Assert.Equal(magic1Active.Id, tool1.Branches[1].ActiveDeployId);
      Assert.Equal(retroActive.Id, tool2.Branches[0].ActiveDeployId);
    }
  }

  //===============================================================================================
  // TEST CREATE GAME
  //===============================================================================================

  [Fact]
  public void TestCreateGame()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var result = test.App.Account.CreateGame(org, new Account.CreateGameCommand
      {
        Name = "My Game",
        Description = "Awesome"
      });
      var game = Assert.Succeeded(result);
      Assert.Equal(org.Id, game.OrganizationId);
      Assert.Equal("My Game", game.Name);
      Assert.Equal("my-game", game.Slug);
      Assert.Equal("Awesome", game.Description);
      Assert.Equal(Account.GamePurpose.Game, game.Purpose);
      Assert.False(game.IsArchived);
      Assert.Null(game.ArchivedOn);
      Assert.Equal(Clock.Now, game.CreatedOn);
      Assert.Equal(Clock.Now, game.UpdatedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestCreateGameMissingName()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var result = test.App.Account.CreateGame(org, new Account.CreateGameCommand
      {
        Name = "",
      });
      var errors = Assert.FailedValidation(result);
      Assert.Equal("name is missing", errors.Format());
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestCreateGameMissingSlug()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var result = test.App.Account.CreateGame(org, new Account.CreateGameCommand
      {
        Name = "My Game",
        Slug = "",
      });
      var errors = Assert.FailedValidation(result);
      Assert.Equal("slug is missing", errors.Format());
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestCreateGameSlugAlreadyExists()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var slug = "example-slug";

      var result = test.App.Account.CreateGame(org, new Account.CreateGameCommand
      {
        Name = "First",
        Slug = slug,
      });
      Assert.Succeeded(result);

      result = test.App.Account.CreateGame(org, new Account.CreateGameCommand
      {
        Name = "Second",
        Slug = slug,
      });

      var errors = Assert.FailedValidation(result);
      Assert.Equal("name already taken for this organization", errors.Format());
    }
  }

  //===============================================================================================
  // TEST UPDATE GAME
  //===============================================================================================

  [Fact]
  public void TestUpdateGame()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org,
        id: "test",
        name: "Old Name",
        slug: "old-slug",
        description: "Old Description",
        createdOn: Clock.Now.Minus(Duration.FromDays(10)),
        updatedOn: Clock.Now.Minus(Duration.FromDays(5))
      );

      var newName = "New Name";
      var newSlug = "new-slug";
      var newDescription = "New Description";

      var result = test.App.Account.UpdateGame(game, new Account.UpdateGameCommand
      {
        Name = newName,
        Slug = newSlug,
        Description = newDescription,
      });

      var updated = Assert.Succeeded(result);

      Assert.Equal(game.Id, updated.Id);
      Assert.Equal(newName, updated.Name);
      Assert.Equal(newSlug, updated.Slug);
      Assert.Equal(newDescription, updated.Description);
      Assert.Equal(game.CreatedOn, updated.CreatedOn);
      Assert.Equal(Clock.Now, updated.UpdatedOn);

      var reloaded = test.App.Account.GetGame(game.Id);
      Assert.Present(reloaded);
      Assert.Equal(game.Id, reloaded.Id);
      Assert.Equal(newName, reloaded.Name);
      Assert.Equal(newSlug, reloaded.Slug);
      Assert.Equal(newDescription, reloaded.Description);
      Assert.Equal(game.CreatedOn, reloaded.CreatedOn);
      Assert.Equal(Clock.Now, reloaded.UpdatedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUpdateGameMissingName()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var game = test.Factory.BuildGame(org);

      var result = test.App.Account.UpdateGame(game, new Account.UpdateGameCommand
      {
        Name = "",
        Slug = "",
        Description = "",
      });

      var errors = Assert.FailedValidation(result);
      Assert.Equal("name is missing", errors.Format());
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUpdateGameMissingSlug()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var game = test.Factory.BuildGame(org);

      var result = test.App.Account.UpdateGame(game, new Account.UpdateGameCommand
      {
        Name = "New Game Name",
        Slug = "",
        Description = "",
      });

      var errors = Assert.FailedValidation(result);
      Assert.Equal("slug is missing", errors.Format());
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUpdateGameSlugAlreadyExists()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var game1 = test.Factory.CreateGame(org, slug: "first-game");
      var game2 = test.Factory.CreateGame(org, slug: "second-game");

      var result = test.App.Account.UpdateGame(game2, new Account.UpdateGameCommand
      {
        Name = "New Game Name",
        Slug = game1.Slug,
      });

      var errors = Assert.FailedValidation(result);
      Assert.Equal("name already taken for this organization", errors.Format());
    }
  }

  //===============================================================================================
  // TEST ARCHIVE and DELETE GAME
  //===============================================================================================

  [Fact]
  public void TestArchiveAndRestoreGame()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("void");
      var game = test.Factory.LoadGame(org, "tetris");

      Assert.False(game.IsArchived);
      Assert.Null(game.ArchivedOn);

      test.App.Account.ArchiveGame(game);
      Assert.True(game.IsArchived);
      Assert.Equal(Clock.Now, game.ArchivedOn);

      var reloaded = test.App.Account.GetGame(game.Id);
      Assert.True(game.IsArchived);
      Assert.Equal(Clock.Now, game.ArchivedOn);

      test.App.Account.RestoreGame(game);
      Assert.False(game.IsArchived);
      Assert.Null(game.ArchivedOn);

      reloaded = test.App.Account.GetGame(game.Id);
      Assert.False(game.IsArchived);
      Assert.Null(game.ArchivedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestDeleteGame()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org, name: "Test");

      var result = test.App.Account.DeleteGame(game);
      Assert.Succeeded(result);

      var reloaded = test.App.Account.GetGame(game.Id);
      Assert.Absent(reloaded);

      var job = Assert.Domain.Enqueued<MoveToTrashMinion, MoveToTrashMinion.Data>(test);
      Assert.Equal(Share.DeployPath(game), job.Path);
      Assert.Equal("game deleted", job.Reason);
    }
  }

  //===============================================================================================
  // TEST DATABASE CONSTRAINTS
  //===============================================================================================

  [Fact]
  public void TestGameSlugMustBeUniqueWithinOrg()
  {
    using (var test = new DomainTest(this))
    {
      var org1 = test.Factory.CreateOrganization();
      var org2 = test.Factory.CreateOrganization();

      test.Factory.CreateGame(org1, slug: "foo");
      test.Factory.CreateGame(org1, slug: "bar");
      test.Factory.CreateGame(org2, slug: "foo");

      var ex = Assert.Throws<MySqlConnector.MySqlException>(() => test.Factory.CreateGame(org1, slug: "FOO"));
      Assert.Equal($"Duplicate entry '{org1.Id}-FOO' for key 'games.games_slug_index'", ex.Message);
    }
  }

  //-----------------------------------------------------------------------------------------------
}