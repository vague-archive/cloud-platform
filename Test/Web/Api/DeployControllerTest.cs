namespace Void.Platform.Web.Api;

public class DeployControllerTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  readonly static string FIRST_CONTENT = "first";
  readonly static string SECOND_CONTENT = "second";
  readonly static string THIRD_CONTENT = "third";
  readonly static string FIRST_BLAKE3 = Crypto.HexString(Crypto.Blake3(FIRST_CONTENT));
  readonly static string SECOND_BLAKE3 = Crypto.HexString(Crypto.Blake3(SECOND_CONTENT));
  readonly static string THIRD_BLAKE3 = Crypto.HexString(Crypto.Blake3(THIRD_CONTENT));
  readonly static string FIRST_ASSET_PATH = "path/to/first.txt";
  readonly static string SECOND_ASSET_PATH = "path/to/second.txt";
  readonly static string THIRD_ASSET_PATH = "path/to/third.txt";
  readonly static string FIRST_CONTENT_PATH = Content.ContentPath(FIRST_BLAKE3);
  readonly static string SECOND_CONTENT_PATH = Content.ContentPath(SECOND_BLAKE3);
  readonly static string THIRD_CONTENT_PATH = Content.ContentPath(THIRD_BLAKE3);

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDeployLifecycle()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");

      var user = test.Factory.LoadUser("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.CreateGame(org);
      var slug = Fake.Slug();
      var expectedPath = $"share/{org.Id}/{game.Id}/{slug}";
      var dt1a = Moment.From(2024, 1, 1, 1, 1, 10);
      var dt1b = Moment.From(2024, 1, 1, 1, 1, 20);
      var dt1c = Moment.From(2024, 1, 1, 1, 1, 30);
      var dt2a = Moment.From(2024, 2, 2, 2, 2, 10);
      var dt2b = Moment.From(2024, 2, 2, 2, 2, 20);
      var dt2c = Moment.From(2024, 2, 2, 2, 2, 30);

      //=========================================
      // PREPARE 2 (OVERLAPPING) DEPLOY MANIFESTS
      //=========================================

      var manifest1 = new Share.DeployAsset[]
      {
        new Share.DeployAsset { Path = FIRST_ASSET_PATH, Blake3 = FIRST_BLAKE3, ContentLength = FIRST_CONTENT.Length },
        new Share.DeployAsset { Path = SECOND_ASSET_PATH, Blake3 = SECOND_BLAKE3, ContentLength = SECOND_CONTENT.Length },
      };

      var manifest2 = new Share.DeployAsset[]
      {
        new Share.DeployAsset { Path = SECOND_ASSET_PATH, Blake3 = SECOND_BLAKE3, ContentLength = SECOND_CONTENT.Length },
        new Share.DeployAsset { Path = THIRD_ASSET_PATH, Blake3 = THIRD_BLAKE3, ContentLength = THIRD_CONTENT.Length },
      };

      //==========================================
      // USER1 STARTS THE FIRST INCREMENTAL DEPLOY
      //==========================================

      Clock.Freeze(dt1a);

      var response1a = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy/{slug}", manifest1);
      var result1a = Assert.Http.Result(Http.StatusCode.Accepted, response1a);
      var missing1 = Json.Deserialize<Share.DeployAsset[]>(result1a);
      var deploy1Id = long.Parse(Assert.Http.HasHeader(Http.Header.XDeployId, response1a));
      var deploy1 = Assert.Present(test.App.Share.GetDeploy(deploy1Id));
      var branch1 = Assert.Present(test.App.Share.GetBranch(deploy1.BranchId));

      Assert.Equal(org.Id, branch1.OrganizationId);
      Assert.Equal(game.Id, branch1.GameId);
      Assert.Null(branch1.ActiveDeployId);
      Assert.Equal(deploy1.Id, branch1.LatestDeployId);
      Assert.Equal(slug, branch1.Slug);
      Assert.Equal(dt1a, branch1.UpdatedOn);

      Assert.Equal(org.Id, deploy1.OrganizationId);
      Assert.Equal(game.Id, deploy1.GameId);
      Assert.Equal(branch1.Id, deploy1.BranchId);
      Assert.Equal(Share.DeployState.Deploying, deploy1.State);
      Assert.Equal(1, deploy1.Number);
      Assert.Equal($"{expectedPath}/1", deploy1.Path);
      Assert.Equal(user.Id, deploy1.DeployedBy);
      Assert.Equal(dt1a, deploy1.DeployedOn);
      Assert.Equal(dt1a, deploy1.UpdatedOn);
      Assert.Equal(dt1a, deploy1.CreatedOn);

      Assert.Equivalent(new Share.DeployAsset[]
      {
        new Share.DeployAsset { Path = FIRST_ASSET_PATH, Blake3 = FIRST_BLAKE3, ContentLength = FIRST_CONTENT.Length },
        new Share.DeployAsset { Path = SECOND_ASSET_PATH, Blake3 = SECOND_BLAKE3, ContentLength = SECOND_CONTENT.Length },
      }, missing1);

      //=================================
      // USER1 UPLOADS THE MISSING FILES
      //=================================

      Clock.Freeze(dt1b);

      using (var stream = FIRST_CONTENT.AsStream())
      {
        var response = await test.Post($"/api/{org.Slug}/{game.Slug}/deploy/{deploy1Id}/upload/{FIRST_ASSET_PATH}", stream);
        var result = Assert.Json.Object(response);
        var blobId = Assert.Json.Number(result["id"]);
        var blob = Assert.Present(test.App.Content.GetObject(blobId));
        Assert.Equal(FIRST_CONTENT_PATH, blob.Path);
        Assert.Equal(FIRST_BLAKE3, blob.Blake3);
        Assert.Equal(FIRST_CONTENT.Length, blob.ContentLength);
        Assert.Equal(Http.ContentType.Text, blob.ContentType);
        Assert.Equal(dt1b, blob.CreatedOn);
        Assert.Equal(dt1b, blob.UpdatedOn);
      }

      using (var stream = SECOND_CONTENT.AsStream())
      {
        var response = await test.Post($"/api/{org.Slug}/{game.Slug}/deploy/{deploy1Id}/upload/{SECOND_ASSET_PATH}", stream);
        var result = Assert.Json.Object(response);
        var blobId = Assert.Json.Number(result["id"]);
        var blob = Assert.Present(test.App.Content.GetObject(blobId));
        Assert.Equal(SECOND_CONTENT_PATH, blob.Path);
        Assert.Equal(SECOND_BLAKE3, blob.Blake3);
        Assert.Equal(SECOND_CONTENT.Length, blob.ContentLength);
        Assert.Equal(Http.ContentType.Text, blob.ContentType);
        Assert.Equal(dt1b, blob.CreatedOn);
        Assert.Equal(dt1b, blob.UpdatedOn);
      }

      Assert.Domain.Files([
        Content.ContentPath(FIRST_BLAKE3),
        Content.ContentPath(SECOND_BLAKE3),
        $"{expectedPath}/1/{Share.ManifestFile}",
      ], test);

      //=============================================
      // USER1 ACTIVATES THE FIRST INCREMENTAL DEPLOY
      //=============================================

      Clock.Freeze(dt1c);

      var response1b = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy/{deploy1Id}/activate", manifest1);
      var result1b = Assert.Json.Object(response1b);

      Assert.Equal(deploy1Id, Assert.Json.Number(result1b["deployId"]));
      Assert.Equal(slug, Assert.Json.String(result1b["slug"]));
      Assert.Equal($"https://void.test/{org.Slug}/{game.Slug}/serve/{slug}/", Assert.Json.String(result1b["url"]));

      deploy1 = Assert.Present(test.App.Share.GetDeploy(deploy1.Id));
      branch1 = Assert.Present(test.App.Share.GetBranch(branch1.Id));

      Assert.Equal(org.Id, branch1.OrganizationId);
      Assert.Equal(game.Id, branch1.GameId);
      Assert.Equal(deploy1.Id, branch1.ActiveDeployId);
      Assert.Equal(deploy1.Id, branch1.LatestDeployId);
      Assert.Equal(slug, branch1.Slug);
      Assert.Equal(dt1c, branch1.UpdatedOn);
      Assert.Equal(dt1a, branch1.CreatedOn);

      Assert.Equal(org.Id, deploy1.OrganizationId);
      Assert.Equal(game.Id, deploy1.GameId);
      Assert.Equal(branch1.Id, deploy1.BranchId);
      Assert.Equal(Share.DeployState.Ready, deploy1.State);
      Assert.Equal(1, deploy1.Number);
      Assert.Equal($"{expectedPath}/1", deploy1.Path);
      Assert.Equal(user.Id, deploy1.DeployedBy);
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
        $"{expectedPath}/1/{Share.ManifestFile}",
        $"{expectedPath}/1/{FIRST_ASSET_PATH}",
        $"{expectedPath}/1/{SECOND_ASSET_PATH}",
      ], test);

      //=========================================
      // USER2 STARTS A SECOND INCREMENTAL DEPLOY
      //=========================================

      Clock.Freeze(dt2a);

      var response2a = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy/{slug}", manifest2);
      var result2a = Assert.Http.Result(Http.StatusCode.Accepted, response2a);
      var missing2 = Json.Deserialize<Share.DeployAsset[]>(result2a);
      var deploy2Id = long.Parse(Assert.Http.HasHeader(Http.Header.XDeployId, response2a));
      var deploy2 = Assert.Present(test.App.Share.GetDeploy(deploy2Id));
      var branch2 = Assert.Present(test.App.Share.GetBranch(deploy2.BranchId));

      Assert.Equal(org.Id, branch2.OrganizationId);
      Assert.Equal(game.Id, branch2.GameId);
      Assert.Equal(deploy1.Id, branch2.ActiveDeployId);
      Assert.Equal(deploy2.Id, branch2.LatestDeployId);
      Assert.Equal(slug, branch2.Slug);
      Assert.Equal(dt2a, branch2.UpdatedOn);
      Assert.Equal(dt1a, branch2.CreatedOn);

      Assert.Equal(org.Id, deploy2.OrganizationId);
      Assert.Equal(game.Id, deploy2.GameId);
      Assert.Equal(branch2.Id, deploy2.BranchId);
      Assert.Equal(Share.DeployState.Deploying, deploy2.State);
      Assert.Equal(2, deploy2.Number);
      Assert.Equal($"{expectedPath}/2", deploy2.Path);
      Assert.Equal(user.Id, deploy2.DeployedBy);
      Assert.Equal(dt2a, deploy2.DeployedOn);
      Assert.Equal(dt2a, deploy2.UpdatedOn);
      Assert.Equal(dt2a, deploy2.CreatedOn);

      Assert.Equivalent(new Share.DeployAsset[]
      {
        new Share.DeployAsset { Path = THIRD_ASSET_PATH, Blake3 = THIRD_BLAKE3, ContentLength = THIRD_CONTENT.Length },
      }, missing2);

      Assert.Domain.Files([
        Content.ContentPath(FIRST_BLAKE3),
        Content.ContentPath(SECOND_BLAKE3),
        $"{expectedPath}/1/{Share.ManifestFile}",
        $"{expectedPath}/1/{FIRST_ASSET_PATH}",
        $"{expectedPath}/1/{SECOND_ASSET_PATH}",
        $"{expectedPath}/2/{Share.ManifestFile}",
      ], test);

      //=================================
      // USER2 UPLOADS THE MISSING FILES
      //=================================

      Clock.Freeze(dt2b);

      using (var stream = THIRD_CONTENT.AsStream())
      {
        var response = await test.Post($"/api/{org.Slug}/{game.Slug}/deploy/{deploy2Id}/upload/{THIRD_ASSET_PATH}", stream);
        var result = Assert.Json.Object(response);
        var blobId = Assert.Json.Number(result["id"]);
        var blob = Assert.Present(test.App.Content.GetObject(blobId));
        Assert.Equal(THIRD_CONTENT_PATH, blob.Path);
        Assert.Equal(THIRD_BLAKE3, blob.Blake3);
        Assert.Equal(THIRD_CONTENT.Length, blob.ContentLength);
        Assert.Equal(Http.ContentType.Text, blob.ContentType);
        Assert.Equal(dt2b, blob.CreatedOn);
        Assert.Equal(dt2b, blob.UpdatedOn);
      }

      Assert.Domain.Files([
        Content.ContentPath(FIRST_BLAKE3),
        Content.ContentPath(SECOND_BLAKE3),
        Content.ContentPath(THIRD_BLAKE3),
        $"{expectedPath}/1/{Share.ManifestFile}",
        $"{expectedPath}/1/{FIRST_ASSET_PATH}",
        $"{expectedPath}/1/{SECOND_ASSET_PATH}",
        $"{expectedPath}/2/{Share.ManifestFile}",
      ], test);

      //==============================================
      // USER2 ACTIVATES THE SECOND INCREMENTAL DEPLOY
      //==============================================

      Clock.Freeze(dt2c);

      var response2b = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy/{deploy2Id}/activate", manifest2);
      var result2b = Assert.Json.Object(response2b);

      Assert.Equal(deploy2Id, Assert.Json.Number(result2b["deployId"]));
      Assert.Equal(slug, Assert.Json.String(result2b["slug"]));
      Assert.Equal($"https://void.test/{org.Slug}/{game.Slug}/serve/{slug}/", Assert.Json.String(result2b["url"]));

      deploy2 = Assert.Present(test.App.Share.GetDeploy(deploy2.Id));
      branch2 = Assert.Present(test.App.Share.GetBranch(branch2.Id));

      Assert.Equal(org.Id, branch2.OrganizationId);
      Assert.Equal(game.Id, branch2.GameId);
      Assert.Equal(deploy2.Id, branch2.ActiveDeployId);
      Assert.Equal(deploy2.Id, branch2.LatestDeployId);
      Assert.Equal(slug, branch2.Slug);
      Assert.Equal(dt2c, branch2.UpdatedOn);
      Assert.Equal(dt1a, branch2.CreatedOn);

      Assert.Equal(org.Id, deploy2.OrganizationId);
      Assert.Equal(game.Id, deploy2.GameId);
      Assert.Equal(branch1.Id, deploy2.BranchId);
      Assert.Equal(Share.DeployState.Ready, deploy2.State);
      Assert.Equal(2, deploy2.Number);
      Assert.Equal($"{expectedPath}/2", deploy2.Path);
      Assert.Equal(user.Id, deploy2.DeployedBy);
      Assert.Equal(dt2c, deploy2.DeployedOn);
      Assert.Equal(dt2c, deploy2.UpdatedOn);
      Assert.Equal(dt2a, deploy2.CreatedOn);

      Assert.Domain.Files([
        Content.ContentPath(FIRST_BLAKE3),
        Content.ContentPath(SECOND_BLAKE3),
        Content.ContentPath(THIRD_BLAKE3),
        $"{expectedPath}/1/{Share.ManifestFile}",
        $"{expectedPath}/1/{FIRST_ASSET_PATH}",
        $"{expectedPath}/1/{SECOND_ASSET_PATH}",
        $"{expectedPath}/2/{Share.ManifestFile}",
        $"{expectedPath}/2/{SECOND_ASSET_PATH}",
        $"{expectedPath}/2/{THIRD_ASSET_PATH}",
      ], test);

      var job = Assert.Domain.Enqueued<MoveToTrashMinion, MoveToTrashMinion.Data>(test);
      Assert.Equal(deploy1.Path, job.Path);
      Assert.Equal("deploy has been replaced", job.Reason);

      Assert.Domain.NoMoreJobsEnqueued(test);
    }
  }

  //===============================================================================================
  // TEST DEPLOY DEFAULTS
  //===============================================================================================

  [Fact]
  public async Task TestDeployDefaults()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");

      var user = test.Factory.LoadUser("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.CreateGame(org);
      var manifest = new Share.DeployAsset[] { };

      var response = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy", manifest);
      Assert.Http.Result(Http.StatusCode.Accepted, response);

      var deployId = long.Parse(Assert.Http.HasHeader(Http.Header.XDeployId, response));
      var deploy = Assert.Present(test.App.Share.GetDeploy(deployId));
      var branch = Assert.Present(test.App.Share.GetBranch(deploy.BranchId));

      Assert.Equal("random-identifier-1", branch.Slug);
      Assert.False(branch.HasPassword);

      response = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy", manifest, new Dictionary<string, string>{
        { Http.Header.XDeployLabel, "Hello World" },
        { Http.Header.XDeployPassword, "you shall not pass" },
      });

      Assert.Http.Result(Http.StatusCode.Accepted, response);

      deployId = long.Parse(Assert.Http.HasHeader(Http.Header.XDeployId, response));
      deploy = Assert.Present(test.App.Share.GetDeploy(deployId));
      branch = Assert.Present(test.App.Share.GetBranch(deploy.BranchId));

      Assert.Equal("hello-world", branch.Slug);
      Assert.True(branch.HasPassword);
      Assert.Equal("you shall not pass", branch.DecryptPassword(test.Encryptor));
    }
  }

  //===============================================================================================
  // TEST INVALID DEPLOY
  //===============================================================================================

  [Fact]
  public async Task TestUnknownDeploy()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");

      var user = test.Factory.LoadUser("active");
      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.CreateGame(org);
      var deployId = Identify("unknown");
      var manifest = new Share.DeployAsset[] { };
      using var stream = FIRST_CONTENT.AsStream();

      var response = await test.Post($"/api/{org.Slug}/{game.Slug}/deploy/{deployId}/upload/{FIRST_ASSET_PATH}", stream);
      Assert.Http.NotFound(response);

      response = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy/{deployId}/activate", manifest);
      Assert.Http.NotFound(response);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestMismatchedDeploy()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");

      var user = test.Factory.LoadUser("active");
      var org = test.Factory.LoadOrganization("atari");
      var game1 = test.Factory.CreateGame(org);
      var game2 = test.Factory.CreateGame(org);
      var branch = test.Factory.CreateBranch(game2);
      var deploy = test.Factory.CreateDeploy(branch, user);
      var manifest = new Share.DeployAsset[] { };
      using var stream = FIRST_CONTENT.AsStream();

      Assert.NotEqual(game1.Id, deploy.GameId); // preconditions

      var response = await test.Post($"/api/{org.Slug}/{game1.Slug}/deploy/{deploy.Id}/upload/{FIRST_ASSET_PATH}", stream);
      Assert.Http.NotFound(response);

      response = await test.PostJSON($"/api/{org.Slug}/{game1.Slug}/deploy/{deploy.Id}/activate", manifest);
      Assert.Http.NotFound(response);
    }
  }

  //===============================================================================================
  // TEST DEPLOY AUTHZ
  //===============================================================================================

  [Fact]
  public async Task TestUnauthorized()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("outsider");

      var (user, org, game, branch, deploy) = Prepare(test);
      var slug = Fake.Slug();
      var manifest = new Share.DeployAsset[] { };
      using var stream = FIRST_CONTENT.AsStream();

      var response = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy/{slug}", manifest);
      Assert.Http.NotFound(response);

      response = await test.Post($"/api/{org.Slug}/{game.Slug}/deploy/{deploy.Id}/upload/{FIRST_ASSET_PATH}", stream);
      Assert.Http.NotFound(response);

      response = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy/{deploy.Id}/activate", manifest);
      Assert.Http.NotFound(response);

      Assert.Domain.NoFilesSaved(test);
      Assert.Domain.NoJobsEnqueued(test);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();

      var (user, org, game, branch, deploy) = Prepare(test);
      var slug = Fake.Slug();
      var manifest = new Share.DeployAsset[] { };
      using var stream = FIRST_CONTENT.AsStream();

      var response = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy/{slug}", manifest);
      Assert.Http.Unauthorized(response);

      response = await test.Post($"/api/{org.Slug}/{game.Slug}/deploy/{deploy.Id}/upload/{FIRST_ASSET_PATH}", stream);
      Assert.Http.Unauthorized(response);

      response = await test.PostJSON($"/api/{org.Slug}/{game.Slug}/deploy/{deploy.Id}/activate", manifest);
      Assert.Http.Unauthorized(response);

      Assert.Domain.NoFilesSaved(test);
      Assert.Domain.NoJobsEnqueued(test);
    }
  }

  //===============================================================================================
  // PRIVATE TEST HELPERS
  //===============================================================================================

  private (Account.User, Account.Organization, Account.Game, Share.Branch, Share.Deploy) Prepare(DomainTest test)
  {
    var user = test.Factory.LoadUser("active");
    var org = test.Factory.LoadOrganization("atari");
    var game = test.Factory.CreateGame(org);
    var branch = test.Factory.CreateBranch(game);
    var deploy = test.Factory.CreateDeploy(branch, user);
    return (user, org, game, branch, deploy);
  }

  //-----------------------------------------------------------------------------------------------
}