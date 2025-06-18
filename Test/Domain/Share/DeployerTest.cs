namespace Void.Platform.Domain;

public class DeployerTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  static readonly string SLUG = "latest";
  static readonly string PASSWORD = "you shall not pass!";
  static readonly string FIRST_CONTENT = "first";
  static readonly string SECOND_CONTENT = "second";
  static readonly string THIRD_CONTENT = "third";
  static readonly string FOURTH_CONTENT = "fourth";
  static readonly string FIFTH_CONTENT = "fifth";
  static readonly string SIXTH_CONTENT = "sixth";
  static readonly string FIRST_PATH = "path/to/first.txt";
  static readonly string SECOND_PATH = "path/to/second.txt";
  static readonly string THIRD_PATH = "path/to/third.txt";
  static readonly string FOURTH_PATH = "path/to/fourth.txt";
  static readonly string FIFTH_PATH = "path/to/fifth.txt";
  static readonly string SIXTH_PATH = "path/to/sixth.txt";
  static readonly string FIRST_BLAKE3 = Crypto.HexString(Crypto.Blake3(FIRST_CONTENT));
  static readonly string SECOND_BLAKE3 = Crypto.HexString(Crypto.Blake3(SECOND_CONTENT));
  static readonly string THIRD_BLAKE3 = Crypto.HexString(Crypto.Blake3(THIRD_CONTENT));
  static readonly string FOURTH_BLAKE3 = Crypto.HexString(Crypto.Blake3(FOURTH_CONTENT));
  static readonly string FIFTH_BLAKE3 = Crypto.HexString(Crypto.Blake3(FIFTH_CONTENT));
  static readonly string SIXTH_BLAKE3 = Crypto.HexString(Crypto.Blake3(SIXTH_CONTENT));

  //===============================================================================================
  // TEST FULL DEPLOYS
  //===============================================================================================

  [Fact]
  public async Task TestFullDeployLifecycle()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var user1 = test.Factory.CreateUser();
      var user2 = test.Factory.CreateUser();
      var user3 = test.Factory.CreateUser();
      var expectedPath = $"share/{org.Id}/{game.Id}/{SLUG}";
      var dt1 = Moment.From(2024, 1, 1, 1, 1, 1);
      var dt2 = Moment.From(2024, 2, 2, 2, 2, 2);
      var dt3 = Moment.From(2024, 3, 3, 3, 3, 3);
      var dt4 = Moment.From(2024, 4, 4, 4, 4, 4);

      //========================================
      // PREPARE 4 TEST ARCHIVES (3 GOOD, 1 BAD)
      //========================================

      using var archive1 = await test.FileStore.CreateTestArchive("archive1.tgz", new (string path, string content)[]
      {
        (FIRST_PATH, FIRST_CONTENT),
        (SECOND_PATH, SECOND_CONTENT),
      });

      using var archive2 = await test.FileStore.CreateTestArchive("archive2.tgz", new (string path, string content)[]
      {
        (THIRD_PATH, THIRD_CONTENT),
        (FOURTH_PATH, FOURTH_CONTENT),
      });

      using var archive3 = await test.FileStore.CreateTestArchive("archive3.tgz", new (string path, string content)[]
      {
        (FIFTH_PATH, FIFTH_CONTENT),
        (SIXTH_PATH, SIXTH_CONTENT),
      });

      await test.FileStore.SaveTestFile("bad.tgz", "not an archive");

      Assert.Domain.Files([
        "archive1.tgz",
        "archive2.tgz",
        "archive3.tgz",
        "bad.tgz",
      ], test);

      //===============================
      // USER1 CREATES THE FIRST DEPLOY
      //===============================

      Clock.Freeze(dt1);

      var result = await test.App.Share.FullDeploy(new Share.FullDeployCommand
      {
        Archive = archive1,
        DeployedBy = user1,
        Organization = org,
        Game = game,
        Slug = SLUG,
      });

      var deploy1 = Assert.Succeeded(result);
      var branch1 = Assert.Present(deploy1.Branch);

      Assert.Equal(org.Id, branch1.OrganizationId);
      Assert.Equal(game.Id, branch1.GameId);
      Assert.Equal(deploy1.Id, branch1.ActiveDeployId);
      Assert.Equal(deploy1.Id, branch1.LatestDeployId);
      Assert.Equal(SLUG, branch1.Slug);
      Assert.False(branch1.HasPassword);
      Assert.False(branch1.IsPinned);
      Assert.Equal(dt1, branch1.UpdatedOn);
      Assert.Equal(dt1, branch1.CreatedOn);

      Assert.Equal(org.Id, deploy1.OrganizationId);
      Assert.Equal(game.Id, deploy1.GameId);
      Assert.Equal(branch1.Id, deploy1.BranchId);
      Assert.Equal(Share.DeployState.Ready, deploy1.State);
      Assert.Equal(1, deploy1.Number);
      Assert.Equal($"{expectedPath}/1", deploy1.Path);
      Assert.Equal(user1.Id, deploy1.DeployedBy);
      Assert.Equal(dt1, deploy1.DeployedOn);
      Assert.Equal(dt1, deploy1.UpdatedOn);
      Assert.Equal(dt1, deploy1.CreatedOn);
      Assert.Null(deploy1.FailedOn);
      Assert.Null(deploy1.DeletedOn);
      Assert.Null(deploy1.DeletedReason);
      Assert.Null(deploy1.Error);

      Assert.Domain.Files([
        "archive1.tgz",
        "archive2.tgz",
        "archive3.tgz",
        "bad.tgz",
        $"{expectedPath}/1/{FIRST_PATH}",
        $"{expectedPath}/1/{SECOND_PATH}",
      ], test);

      Assert.Domain.NoJobsEnqueued(test);

      //===============================
      // USER2 CREATES A SECOND DEPLOY
      //===============================

      Clock.Freeze(dt2);

      result = await test.App.Share.FullDeploy(new Share.FullDeployCommand
      {
        Archive = archive2,
        DeployedBy = user2,
        Organization = org,
        Game = game,
        Slug = SLUG,
      });

      var deploy2 = Assert.Succeeded(result);
      var branch2 = Assert.Present(deploy2.Branch);

      Assert.Equal(branch1.Id, branch2.Id);
      Assert.Equal(deploy2.Id, branch2.ActiveDeployId);
      Assert.Equal(deploy2.Id, branch2.LatestDeployId);
      Assert.Equal(dt2, branch2.UpdatedOn);
      Assert.Equal(dt1, branch2.CreatedOn);

      Assert.Equal(Share.DeployState.Ready, deploy2.State);
      Assert.Equal(2, deploy2.Number);
      Assert.Equal($"{expectedPath}/2", deploy2.Path);
      Assert.Equal(user2.Id, deploy2.DeployedBy);
      Assert.Equal(dt2, deploy2.DeployedOn);
      Assert.Equal(dt2, deploy2.UpdatedOn);
      Assert.Equal(dt2, deploy2.CreatedOn);
      Assert.Null(deploy2.FailedOn);
      Assert.Null(deploy2.DeletedOn);
      Assert.Null(deploy2.DeletedReason);
      Assert.Null(deploy2.Error);

      Assert.Domain.Files([
        "archive1.tgz",
        "archive2.tgz",
        "archive3.tgz",
        "bad.tgz",
        $"{expectedPath}/1/{FIRST_PATH}",
        $"{expectedPath}/1/{SECOND_PATH}",
        $"{expectedPath}/2/{THIRD_PATH}",
        $"{expectedPath}/2/{FOURTH_PATH}",
      ], test);

      var job = Assert.Domain.Enqueued<MoveToTrashMinion, MoveToTrashMinion.Data>(test);
      Assert.Equal(deploy1.Path, job.Path);
      Assert.Equal("deploy has been replaced", job.Reason);

      //===================================
      // USER3 CREATES A THIRD (BAD) DEPLOY
      //===================================

      var badArchive = await test.FileStore.Load("bad.tgz");
      Assert.Present(badArchive);

      Clock.Freeze(dt3);

      result = await test.App.Share.FullDeploy(new Share.FullDeployCommand
      {
        Archive = badArchive,
        DeployedBy = user3,
        Organization = org,
        Game = game,
        Slug = SLUG,
      });

      var error = Assert.Failed(result);
      Assert.Equal($"failed to deploy {org.Slug}/{game.Slug}/{SLUG} to share/{org.Id}/{game.Id}/{SLUG}/3: The archive entry was compressed using an unsupported compression method.", error.Format());

      var branch3 = Assert.Present(test.Factory.LoadBranch(branch1.Id));
      var deploy3Id = Assert.Present(branch3.LatestDeployId);
      var deploy3 = Assert.Present(test.Factory.LoadDeploy(deploy3Id));

      Assert.Equal(deploy2.Id, branch3.ActiveDeployId);
      Assert.Equal(deploy3.Id, branch3.LatestDeployId);
      Assert.Equal(dt3, branch3.UpdatedOn);
      Assert.Equal(dt1, branch3.CreatedOn);

      Assert.Equal(Share.DeployState.Failed, deploy3.State);
      Assert.Equal(3, deploy3.Number);
      Assert.Equal($"{expectedPath}/3", deploy3.Path);
      Assert.Equal(user3.Id, deploy3.DeployedBy);
      Assert.Equal(dt3, deploy3.DeployedOn);
      Assert.Equal(dt3, deploy3.FailedOn);
      Assert.Equal(dt3, deploy3.UpdatedOn);
      Assert.Equal(dt3, deploy3.CreatedOn);
      Assert.Equal(dt3, deploy3.DeletedOn);
      Assert.Equal("deploy failed", deploy3.DeletedReason);
      Assert.Equal("The archive entry was compressed using an unsupported compression method.", deploy3.Error);

      Assert.Domain.Files([
        "archive1.tgz",
        "archive2.tgz",
        "archive3.tgz",
        "bad.tgz",
        $"{expectedPath}/1/{FIRST_PATH}",
        $"{expectedPath}/1/{SECOND_PATH}",
        $"{expectedPath}/2/{THIRD_PATH}",
        $"{expectedPath}/2/{FOURTH_PATH}",
      ], test);

      job = Assert.Domain.Enqueued<MoveToTrashMinion, MoveToTrashMinion.Data>(test);
      Assert.Equal(deploy3.Path, job.Path);
      Assert.Equal("deploy failed", job.Reason);

      //==========================================
      // USER3 FIXES AND RE-UPLOADS FOURTH DEPLOY
      //==========================================

      Clock.Freeze(dt4);

      result = await test.App.Share.FullDeploy(new Share.FullDeployCommand
      {
        Archive = archive3,
        DeployedBy = user3,
        Organization = org,
        Game = game,
        Slug = SLUG,
      });

      var deploy4 = Assert.Succeeded(result);
      var branch4 = Assert.Present(deploy4.Branch);

      Assert.Equal(branch1.Id, branch4.Id);
      Assert.Equal(deploy4.Id, branch4.ActiveDeployId);
      Assert.Equal(deploy4.Id, branch4.LatestDeployId);
      Assert.Equal(dt4, branch4.UpdatedOn);
      Assert.Equal(dt1, branch4.CreatedOn);

      Assert.Equal(Share.DeployState.Ready, deploy4.State);
      Assert.Equal(4, deploy4.Number);
      Assert.Equal($"{expectedPath}/4", deploy4.Path);
      Assert.Equal(user3.Id, deploy4.DeployedBy);
      Assert.Equal(dt4, deploy4.DeployedOn);
      Assert.Equal(dt4, deploy4.UpdatedOn);
      Assert.Equal(dt4, deploy4.CreatedOn);
      Assert.Null(deploy4.FailedOn);
      Assert.Null(deploy4.DeletedOn);
      Assert.Null(deploy4.DeletedReason);
      Assert.Null(deploy4.Error);

      Assert.Domain.Files([
        "archive1.tgz",
        "archive2.tgz",
        "archive3.tgz",
        "bad.tgz",
        $"{expectedPath}/1/{FIRST_PATH}",
        $"{expectedPath}/1/{SECOND_PATH}",
        $"{expectedPath}/2/{THIRD_PATH}",
        $"{expectedPath}/2/{FOURTH_PATH}",
        $"{expectedPath}/4/{FIFTH_PATH}",
        $"{expectedPath}/4/{SIXTH_PATH}",
      ], test);

      Assert.Equal(FIRST_CONTENT, await test.FileStore.Read($"{expectedPath}/1/{FIRST_PATH}"));
      Assert.Equal(SECOND_CONTENT, await test.FileStore.Read($"{expectedPath}/1/{SECOND_PATH}"));
      Assert.Equal(THIRD_CONTENT, await test.FileStore.Read($"{expectedPath}/2/{THIRD_PATH}"));
      Assert.Equal(FOURTH_CONTENT, await test.FileStore.Read($"{expectedPath}/2/{FOURTH_PATH}"));
      Assert.Equal(FIFTH_CONTENT, await test.FileStore.Read($"{expectedPath}/4/{FIFTH_PATH}"));
      Assert.Equal(SIXTH_CONTENT, await test.FileStore.Read($"{expectedPath}/4/{SIXTH_PATH}"));

      job = Assert.Domain.Enqueued<MoveToTrashMinion, MoveToTrashMinion.Data>(test);
      Assert.Equal(deploy2.Path, job.Path);
      Assert.Equal("deploy has been replaced", job.Reason);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestFullDeployWithPassword()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var user = test.Factory.CreateUser();

      using var archive = await test.FileStore.CreateTestArchive("build.tgz");

      var result = await test.App.Share.FullDeploy(new Share.FullDeployCommand
      {
        Archive = archive,
        DeployedBy = user,
        Organization = org,
        Game = game,
        Slug = SLUG,
        Password = PASSWORD,
      });

      var deploy = Assert.Succeeded(result);
      var branch = Assert.Present(deploy.Branch);
      Assert.True(branch.HasPassword);
      Assert.Equal(PASSWORD, branch.DecryptPassword(test.Encryptor));
    }
  }

  //===============================================================================================
  // TEST INCREMENTAL DEPLOYS
  //===============================================================================================

  [Fact]
  public async Task TestIncrementalDeployLifecycle()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var user1 = test.Factory.CreateUser();
      var user2 = test.Factory.CreateUser();
      var expectedPath = $"share/{org.Id}/{game.Id}/{SLUG}";
      var dt1a = Moment.From(2024, 1, 1, 1, 1, 10);
      var dt1b = Moment.From(2024, 1, 1, 1, 1, 20);
      var dt1c = Moment.From(2024, 1, 1, 1, 1, 30);
      var dt2a = Moment.From(2024, 2, 2, 2, 2, 10);
      var dt2b = Moment.From(2024, 2, 2, 2, 2, 20);
      var dt2c = Moment.From(2024, 2, 2, 2, 2, 30);

      //===========================
      // PREPARE 2 DEPLOY MANIFESTS
      //===========================

      var manifest1 = new Share.DeployAsset[]
      {
        new Share.DeployAsset { Path = FIRST_PATH, Blake3 = FIRST_BLAKE3, ContentLength = FIRST_CONTENT.Length },
        new Share.DeployAsset { Path = SECOND_PATH, Blake3 = SECOND_BLAKE3, ContentLength = SECOND_CONTENT.Length },
        new Share.DeployAsset { Path = THIRD_PATH, Blake3 = THIRD_BLAKE3, ContentLength = THIRD_CONTENT.Length },
        new Share.DeployAsset { Path = FOURTH_PATH, Blake3 = FOURTH_BLAKE3, ContentLength = FOURTH_CONTENT.Length },
      };

      var manifest2 = new Share.DeployAsset[]
      {
        new Share.DeployAsset { Path = THIRD_PATH, Blake3 = THIRD_BLAKE3, ContentLength = THIRD_CONTENT.Length },
        new Share.DeployAsset { Path = FOURTH_PATH, Blake3 = FOURTH_BLAKE3, ContentLength = FOURTH_CONTENT.Length },
        new Share.DeployAsset { Path = FIFTH_PATH, Blake3 = FIFTH_BLAKE3, ContentLength = FIFTH_CONTENT.Length },
        new Share.DeployAsset { Path = SIXTH_PATH, Blake3 = SIXTH_BLAKE3, ContentLength = SIXTH_CONTENT.Length },
      };

      //==========================================
      // USER1 STARTS THE FIRST INCREMENTAL DEPLOY
      //==========================================

      Clock.Freeze(dt1a);

      var result1a = await test.App.Share.IncrementalDeploy(new Share.IncrementalDeployCommand
      {
        Manifest = manifest1,
        DeployedBy = user1,
        Organization = org,
        Game = game,
        Slug = SLUG,
      });

      var (deploy1, missing1) = Assert.Succeeded(result1a);
      var branch1 = Assert.Present(deploy1.Branch);

      Assert.Equal(org.Id, branch1.OrganizationId);
      Assert.Equal(game.Id, branch1.GameId);
      Assert.Null(branch1.ActiveDeployId);
      Assert.Equal(deploy1.Id, branch1.LatestDeployId);
      Assert.Equal(SLUG, branch1.Slug);
      Assert.False(branch1.HasPassword);
      Assert.False(branch1.IsPinned);
      Assert.Equal(dt1a, branch1.UpdatedOn);
      Assert.Equal(dt1a, branch1.CreatedOn);

      Assert.Equal(org.Id, deploy1.OrganizationId);
      Assert.Equal(game.Id, deploy1.GameId);
      Assert.Equal(branch1.Id, deploy1.BranchId);
      Assert.Equal(Share.DeployState.Deploying, deploy1.State);
      Assert.Equal(1, deploy1.Number);
      Assert.Equal($"{expectedPath}/1", deploy1.Path);
      Assert.Equal(user1.Id, deploy1.DeployedBy);
      Assert.Equal(dt1a, deploy1.DeployedOn);
      Assert.Equal(dt1a, deploy1.UpdatedOn);
      Assert.Equal(dt1a, deploy1.CreatedOn);
      Assert.Null(deploy1.FailedOn);
      Assert.Null(deploy1.DeletedOn);
      Assert.Null(deploy1.DeletedReason);
      Assert.Null(deploy1.Error);

      Assert.Equivalent(new Share.DeployAsset[]
      {
        new Share.DeployAsset { Path = FIRST_PATH, Blake3 = FIRST_BLAKE3, ContentLength = FIRST_CONTENT.Length },
        new Share.DeployAsset { Path = SECOND_PATH, Blake3 = SECOND_BLAKE3, ContentLength = SECOND_CONTENT.Length },
        new Share.DeployAsset { Path = THIRD_PATH, Blake3 = THIRD_BLAKE3, ContentLength = THIRD_CONTENT.Length },
        new Share.DeployAsset { Path = FOURTH_PATH, Blake3 = FOURTH_BLAKE3, ContentLength = FOURTH_CONTENT.Length },
      }, missing1);

      //=================================
      // USER1 UPLOADS THE MISSING FILES
      //=================================

      Clock.Freeze(dt1b);

      using (var stream = FIRST_CONTENT.AsStream())
      {
        var result = await test.App.Share.UploadDeployAsset(new Share.UploadDeployAssetCommand
        {
          Organization = org,
          Game = game,
          Deploy = deploy1,
          Content = stream,
          ContentType = Http.ContentType.Text,
        });
        var (exists, blob) = Assert.Succeeded(result);
        Assert.False(exists);
        Assert.Equal(Content.ContentPath(FIRST_BLAKE3), blob.Path);
        Assert.Equal(FIRST_BLAKE3, blob.Blake3);
        Assert.Equal(FIRST_CONTENT.Length, blob.ContentLength);
        Assert.Equal(Http.ContentType.Text, blob.ContentType);
        Assert.Equal(dt1b, blob.CreatedOn);
        Assert.Equal(dt1b, blob.UpdatedOn);
      }

      using (var stream = SECOND_CONTENT.AsStream())
      {
        var result = await test.App.Share.UploadDeployAsset(new Share.UploadDeployAssetCommand
        {
          Organization = org,
          Game = game,
          Deploy = deploy1,
          Content = stream,
          ContentType = Http.ContentType.Text,
        });
        var (exists, blob) = Assert.Succeeded(result);
        Assert.False(exists);
        Assert.Equal(Content.ContentPath(SECOND_BLAKE3), blob.Path);
        Assert.Equal(SECOND_BLAKE3, blob.Blake3);
        Assert.Equal(SECOND_CONTENT.Length, blob.ContentLength);
        Assert.Equal(Http.ContentType.Text, blob.ContentType);
        Assert.Equal(dt1b, blob.CreatedOn);
        Assert.Equal(dt1b, blob.UpdatedOn);
      }

      using (var stream = THIRD_CONTENT.AsStream())
      {
        var result = await test.App.Share.UploadDeployAsset(new Share.UploadDeployAssetCommand
        {
          Organization = org,
          Game = game,
          Deploy = deploy1,
          Content = stream,
          ContentType = Http.ContentType.Text,
        });
        var (exists, blob) = Assert.Succeeded(result);
        Assert.False(exists);
        Assert.Equal(Content.ContentPath(THIRD_BLAKE3), blob.Path);
        Assert.Equal(THIRD_BLAKE3, blob.Blake3);
        Assert.Equal(THIRD_CONTENT.Length, blob.ContentLength);
        Assert.Equal(Http.ContentType.Text, blob.ContentType);
        Assert.Equal(dt1b, blob.CreatedOn);
        Assert.Equal(dt1b, blob.UpdatedOn);
      }

      using (var stream = FOURTH_CONTENT.AsStream())
      {
        var result = await test.App.Share.UploadDeployAsset(new Share.UploadDeployAssetCommand
        {
          Organization = org,
          Game = game,
          Deploy = deploy1,
          Content = stream,
          ContentType = Http.ContentType.Text,
        });
        var (exists, blob) = Assert.Succeeded(result);
        Assert.False(exists);
        Assert.Equal(Content.ContentPath(FOURTH_BLAKE3), blob.Path);
        Assert.Equal(FOURTH_BLAKE3, blob.Blake3);
        Assert.Equal(FOURTH_CONTENT.Length, blob.ContentLength);
        Assert.Equal(Http.ContentType.Text, blob.ContentType);
        Assert.Equal(dt1b, blob.CreatedOn);
        Assert.Equal(dt1b, blob.UpdatedOn);
      }

      Assert.Domain.Files([
        Content.ContentPath(FIRST_BLAKE3),
        Content.ContentPath(SECOND_BLAKE3),
        Content.ContentPath(THIRD_BLAKE3),
        Content.ContentPath(FOURTH_BLAKE3),
        $"{expectedPath}/1/{Share.ManifestFile}",
      ], test);

      //=============================================
      // USER1 ACTIVATES THE FIRST INCREMENTAL DEPLOY
      //=============================================

      Clock.Freeze(dt1c);

      var result1b = await test.App.Share.ActivateIncrementalDeploy(new Share.ActivateIncrementalDeployCommand
      {
        Organization = org,
        Game = game,
        Branch = branch1,
        Deploy = deploy1,
      });

      deploy1 = Assert.Succeeded(result1b);
      branch1 = Assert.Present(deploy1.Branch);

      Assert.Equal(org.Id, branch1.OrganizationId);
      Assert.Equal(game.Id, branch1.GameId);
      Assert.Equal(deploy1.Id, branch1.ActiveDeployId);
      Assert.Equal(deploy1.Id, branch1.LatestDeployId);
      Assert.Equal(SLUG, branch1.Slug);
      Assert.Equal(dt1c, branch1.UpdatedOn);
      Assert.Equal(dt1a, branch1.CreatedOn);

      Assert.Equal(org.Id, deploy1.OrganizationId);
      Assert.Equal(game.Id, deploy1.GameId);
      Assert.Equal(branch1.Id, deploy1.BranchId);
      Assert.Equal(Share.DeployState.Ready, deploy1.State);
      Assert.Equal(1, deploy1.Number);
      Assert.Equal($"{expectedPath}/1", deploy1.Path);
      Assert.Equal(user1.Id, deploy1.DeployedBy);
      Assert.Equal(dt1c, deploy1.DeployedOn);
      Assert.Equal(dt1c, deploy1.UpdatedOn);
      Assert.Equal(dt1a, deploy1.CreatedOn);
      Assert.Null(deploy1.FailedOn);
      Assert.Null(deploy1.DeletedOn);
      Assert.Null(deploy1.DeletedReason);
      Assert.Null(deploy1.Error);

      Assert.Domain.Files([
        Content.ContentPath(FIRST_BLAKE3),
        Content.ContentPath(SECOND_BLAKE3),
        Content.ContentPath(THIRD_BLAKE3),
        Content.ContentPath(FOURTH_BLAKE3),
        $"{expectedPath}/1/{Share.ManifestFile}",
        $"{expectedPath}/1/{FIRST_PATH}",
        $"{expectedPath}/1/{SECOND_PATH}",
        $"{expectedPath}/1/{THIRD_PATH}",
        $"{expectedPath}/1/{FOURTH_PATH}",
      ], test);

      //=========================================
      // USER2 STARTS A SECOND INCREMENTAL DEPLOY
      //=========================================

      Clock.Freeze(dt2a);

      var result2a = await test.App.Share.IncrementalDeploy(new Share.IncrementalDeployCommand
      {
        Manifest = manifest2,
        DeployedBy = user2,
        Organization = org,
        Game = game,
        Slug = SLUG,
      });

      var (deploy2, missing2) = Assert.Succeeded(result2a);
      var branch2 = Assert.Present(deploy2.Branch);

      Assert.Equal(branch1.Id, branch2.Id);
      Assert.Equal(org.Id, branch2.OrganizationId);
      Assert.Equal(game.Id, branch2.GameId);
      Assert.Equal(deploy1.Id, branch2.ActiveDeployId);
      Assert.Equal(deploy2.Id, branch2.LatestDeployId);
      Assert.Equal(SLUG, branch2.Slug);
      Assert.Equal(dt2a, branch2.UpdatedOn);
      Assert.Equal(dt1a, branch2.CreatedOn);

      Assert.Equal(org.Id, deploy2.OrganizationId);
      Assert.Equal(game.Id, deploy2.GameId);
      Assert.Equal(branch2.Id, deploy2.BranchId);
      Assert.Equal(Share.DeployState.Deploying, deploy2.State);
      Assert.Equal(2, deploy2.Number);
      Assert.Equal($"{expectedPath}/2", deploy2.Path);
      Assert.Equal(user2.Id, deploy2.DeployedBy);
      Assert.Equal(dt2a, deploy2.DeployedOn);
      Assert.Equal(dt2a, deploy2.UpdatedOn);
      Assert.Equal(dt2a, deploy2.CreatedOn);
      Assert.Null(deploy2.FailedOn);
      Assert.Null(deploy2.DeletedOn);
      Assert.Null(deploy2.DeletedReason);
      Assert.Null(deploy2.Error);

      Assert.Equivalent(new Share.DeployAsset[]
      {
        new Share.DeployAsset { Path = FIFTH_PATH, Blake3 = FIFTH_BLAKE3, ContentLength = FIFTH_CONTENT.Length },
        new Share.DeployAsset { Path = SIXTH_PATH, Blake3 = SIXTH_BLAKE3, ContentLength = SIXTH_CONTENT.Length },
      }, missing2);

      //=================================
      // USER2 UPLOADS THE MISSING FILES
      //=================================

      Clock.Freeze(dt2b);

      using (var stream = FIFTH_CONTENT.AsStream())
      {
        var result = await test.App.Share.UploadDeployAsset(new Share.UploadDeployAssetCommand
        {
          Organization = org,
          Game = game,
          Deploy = deploy2,
          Content = stream,
          ContentType = Http.ContentType.Text,
        });
        var (exists, blob) = Assert.Succeeded(result);
        Assert.False(exists);
        Assert.Equal(Content.ContentPath(FIFTH_BLAKE3), blob.Path);
        Assert.Equal(FIFTH_BLAKE3, blob.Blake3);
        Assert.Equal(FIFTH_CONTENT.Length, blob.ContentLength);
        Assert.Equal(Http.ContentType.Text, blob.ContentType);
        Assert.Equal(dt2b, blob.CreatedOn);
        Assert.Equal(dt2b, blob.UpdatedOn);
      }

      using (var stream = SIXTH_CONTENT.AsStream())
      {
        var result = await test.App.Share.UploadDeployAsset(new Share.UploadDeployAssetCommand
        {
          Organization = org,
          Game = game,
          Deploy = deploy2,
          Content = stream,
          ContentType = Http.ContentType.Text,
        });
        var (exists, blob) = Assert.Succeeded(result);
        Assert.False(exists);
        Assert.Equal(Content.ContentPath(SIXTH_BLAKE3), blob.Path);
        Assert.Equal(SIXTH_BLAKE3, blob.Blake3);
        Assert.Equal(SIXTH_CONTENT.Length, blob.ContentLength);
        Assert.Equal(Http.ContentType.Text, blob.ContentType);
        Assert.Equal(dt2b, blob.CreatedOn);
        Assert.Equal(dt2b, blob.UpdatedOn);
      }

      Assert.Domain.Files([
        Content.ContentPath(FIRST_BLAKE3),
        Content.ContentPath(SECOND_BLAKE3),
        Content.ContentPath(THIRD_BLAKE3),
        Content.ContentPath(FOURTH_BLAKE3),
        Content.ContentPath(FIFTH_BLAKE3),
        Content.ContentPath(SIXTH_BLAKE3),
        $"{expectedPath}/1/{Share.ManifestFile}",
        $"{expectedPath}/1/{FIRST_PATH}",
        $"{expectedPath}/1/{SECOND_PATH}",
        $"{expectedPath}/1/{THIRD_PATH}",
        $"{expectedPath}/1/{FOURTH_PATH}",
        $"{expectedPath}/2/{Share.ManifestFile}",
      ], test);

      //==============================================
      // USER2 ACTIVATES THE SECOND INCREMENTAL DEPLOY
      //==============================================

      Clock.Freeze(dt2c);

      var result2b = await test.App.Share.ActivateIncrementalDeploy(new Share.ActivateIncrementalDeployCommand
      {
        Organization = org,
        Game = game,
        Branch = branch2,
        Deploy = deploy2,
      });

      deploy2 = Assert.Succeeded(result2b);
      branch2 = Assert.Present(deploy2.Branch);

      Assert.Equal(org.Id, branch2.OrganizationId);
      Assert.Equal(game.Id, branch2.GameId);
      Assert.Equal(deploy2.Id, branch2.ActiveDeployId);
      Assert.Equal(deploy2.Id, branch2.LatestDeployId);
      Assert.Equal(SLUG, branch2.Slug);
      Assert.Equal(dt2c, branch2.UpdatedOn);
      Assert.Equal(dt1a, branch2.CreatedOn);

      Assert.Equal(org.Id, deploy2.OrganizationId);
      Assert.Equal(game.Id, deploy2.GameId);
      Assert.Equal(branch2.Id, deploy2.BranchId);
      Assert.Equal(Share.DeployState.Ready, deploy2.State);
      Assert.Equal(2, deploy2.Number);
      Assert.Equal($"{expectedPath}/2", deploy2.Path);
      Assert.Equal(user2.Id, deploy2.DeployedBy);
      Assert.Equal(dt2c, deploy2.DeployedOn);
      Assert.Equal(dt2c, deploy2.UpdatedOn);
      Assert.Equal(dt2a, deploy2.CreatedOn);
      Assert.Null(deploy2.FailedOn);
      Assert.Null(deploy2.DeletedOn);
      Assert.Null(deploy2.DeletedReason);
      Assert.Null(deploy2.Error);

      Assert.Domain.Files([
        Content.ContentPath(FIRST_BLAKE3),
        Content.ContentPath(SECOND_BLAKE3),
        Content.ContentPath(THIRD_BLAKE3),
        Content.ContentPath(FOURTH_BLAKE3),
        Content.ContentPath(FIFTH_BLAKE3),
        Content.ContentPath(SIXTH_BLAKE3),
        $"{expectedPath}/1/{Share.ManifestFile}",
        $"{expectedPath}/1/{FIRST_PATH}",
        $"{expectedPath}/1/{SECOND_PATH}",
        $"{expectedPath}/1/{THIRD_PATH}",
        $"{expectedPath}/1/{FOURTH_PATH}",
        $"{expectedPath}/2/{Share.ManifestFile}",
        $"{expectedPath}/2/{THIRD_PATH}",
        $"{expectedPath}/2/{FOURTH_PATH}",
        $"{expectedPath}/2/{FIFTH_PATH}",
        $"{expectedPath}/2/{SIXTH_PATH}",
      ], test);

      Assert.Equal(FIRST_CONTENT, await test.FileStore.Read(Content.ContentPath(FIRST_BLAKE3)));
      Assert.Equal(SECOND_CONTENT, await test.FileStore.Read(Content.ContentPath(SECOND_BLAKE3)));
      Assert.Equal(THIRD_CONTENT, await test.FileStore.Read(Content.ContentPath(THIRD_BLAKE3)));
      Assert.Equal(FOURTH_CONTENT, await test.FileStore.Read(Content.ContentPath(FOURTH_BLAKE3)));
      Assert.Equal(FIFTH_CONTENT, await test.FileStore.Read(Content.ContentPath(FIFTH_BLAKE3)));
      Assert.Equal(SIXTH_CONTENT, await test.FileStore.Read(Content.ContentPath(SIXTH_BLAKE3)));

      Assert.Equal(FIRST_CONTENT, await test.FileStore.Read($"{expectedPath}/1/{FIRST_PATH}"));
      Assert.Equal(SECOND_CONTENT, await test.FileStore.Read($"{expectedPath}/1/{SECOND_PATH}"));
      Assert.Equal(THIRD_CONTENT, await test.FileStore.Read($"{expectedPath}/1/{THIRD_PATH}"));
      Assert.Equal(FOURTH_CONTENT, await test.FileStore.Read($"{expectedPath}/1/{FOURTH_PATH}"));
      Assert.Equal(THIRD_CONTENT, await test.FileStore.Read($"{expectedPath}/2/{THIRD_PATH}"));
      Assert.Equal(FOURTH_CONTENT, await test.FileStore.Read($"{expectedPath}/2/{FOURTH_PATH}"));
      Assert.Equal(FIFTH_CONTENT, await test.FileStore.Read($"{expectedPath}/2/{FIFTH_PATH}"));
      Assert.Equal(SIXTH_CONTENT, await test.FileStore.Read($"{expectedPath}/2/{SIXTH_PATH}"));

      Assert.Equal(Json.Serialize(manifest1), await test.FileStore.Read($"{expectedPath}/1/{Share.ManifestFile}"));
      Assert.Equal(Json.Serialize(manifest2), await test.FileStore.Read($"{expectedPath}/2/{Share.ManifestFile}"));

      var job = Assert.Domain.Enqueued<MoveToTrashMinion, MoveToTrashMinion.Data>(test);
      Assert.Equal(deploy1.Path, job.Path);
      Assert.Equal("deploy has been replaced", job.Reason);

      Assert.Domain.NoMoreJobsEnqueued(test);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCannotUploadToOrActivateFailedDeploy()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game);
      var deploy = test.Factory.CreateDeploy(branch, user, state: Share.DeployState.Failed);
      var manifest = new Share.DeployAsset[] { };
      using var stream = FIRST_CONTENT.AsStream();

      var uploadResult = await test.App.Share.UploadDeployAsset(new Share.UploadDeployAssetCommand
      {
        Organization = org,
        Game = game,
        Deploy = deploy,
        Content = stream,
        ContentType = Http.ContentType.Text,
      });
      Assert.Failed(uploadResult);
      Assert.Equal("cannot upload to deploy that has already failed", uploadResult.Error.Format());

      var activateResult = await test.App.Share.ActivateIncrementalDeploy(new Share.ActivateIncrementalDeployCommand
      {
        Organization = org,
        Game = game,
        Branch = branch,
        Deploy = deploy,
      });
      Assert.Failed(activateResult);
      Assert.Equal("cannot activate deploy that has already failed", activateResult.Error.Format());
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCannotUploadToOrActivateReadyDeploy()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.CreateOrganization();
      var game = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game);
      var deploy = test.Factory.CreateDeploy(branch, user, state: Share.DeployState.Ready);
      var manifest = new Share.DeployAsset[] { };
      using var stream = FIRST_CONTENT.AsStream();

      var uploadResult = await test.App.Share.UploadDeployAsset(new Share.UploadDeployAssetCommand
      {
        Organization = org,
        Game = game,
        Deploy = deploy,
        Content = stream,
        ContentType = Http.ContentType.Text,
      });
      Assert.Failed(uploadResult);
      Assert.Equal("cannot upload to deploy that has already been activated", uploadResult.Error.Format());

      var activateResult = await test.App.Share.ActivateIncrementalDeploy(new Share.ActivateIncrementalDeployCommand
      {
        Organization = org,
        Game = game,
        Branch = branch,
        Deploy = deploy,
      });
      Assert.Failed(activateResult);
      Assert.Equal("cannot activate deploy that has already been activated", activateResult.Error.Format());
    }
  }

  //-----------------------------------------------------------------------------------------------
}