namespace Void.Platform.Web.Api;

public class ShareControllerTest : TestCase
{
  //===============================================================================================
  // TEST happy path
  //===============================================================================================

  [Fact]
  public async Task TestUpload()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");

      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");

      using var archive = await test.FileStore.CreateTestArchive("build.tgz", new (string path, string content)[]
      {
        ("assets/file1.txt", "first"),
        ("assets/file2.txt", "second"),
      });

      var response = await test.Post($"/api/{org.Slug}/{game.Slug}/share", archive);
      var content = Assert.Json.Object(response);
      var id = Assert.Json.Number(content["id"]);
      var slug = Assert.Json.String(content["slug"]);
      var url = Assert.Json.String(content["url"]);

      Assert.True(id > 0);
      Assert.Equal("random-identifier-1", slug);
      Assert.Equal($"https://void.test/{org.Slug}/{game.Slug}/serve/{slug}/", url);

      var deploy = test.Factory.LoadDeploy(id);
      Assert.Present(deploy);
      Assert.Equal(id, deploy.Id);
      Assert.Equal(Share.DeployState.Ready, deploy.State);
      Assert.Equal(1, deploy.Number);
      Assert.Equal($"share/{org.Id}/{game.Id}/{slug}/1", deploy.Path);

      var branch = test.Factory.LoadBranch(deploy.BranchId);
      Assert.Present(branch);
      Assert.Equal(slug, branch.Slug);
      Assert.False(branch.HasPassword);
      Assert.Equal(deploy.Id, branch.ActiveDeployId);
      Assert.Equal(deploy.Id, branch.LatestDeployId);

      Assert.Domain.NoJobsEnqueued(test);
      Assert.Domain.Files([
        "build.tgz",
        $"share/{org.Id}/{game.Id}/{slug}/1/assets/file1.txt",
        $"share/{org.Id}/{game.Id}/{slug}/1/assets/file2.txt",
      ], test);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUploadWithLabelAndPassword()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");

      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var label = "Example Label";

      using var archive = await test.FileStore.CreateTestArchive("build.tgz");

      var response = await test.Post($"/api/{org.Slug}/{game.Slug}/share", archive, new Dictionary<string, string>{
        { Http.Header.XDeployLabel, label },
        { Http.Header.XDeployPinned, "true" },
        { Http.Header.XDeployPassword, "you shall not pass" },
      });
      var content = Assert.Json.Object(response);
      var id = Assert.Json.Number(content["id"]);
      var slug = Assert.Json.String(content["slug"]);
      var url = Assert.Json.String(content["url"]);

      Assert.True(id > 0);
      Assert.Equal("example-label", slug);
      Assert.Equal($"https://void.test/{org.Slug}/{game.Slug}/serve/{slug}/", url);

      var deploy = test.Factory.LoadDeploy(id);
      Assert.Present(deploy);
      Assert.Equal(id, deploy.Id);
      Assert.Equal(Share.DeployState.Ready, deploy.State);
      Assert.Equal(1, deploy.Number);
      Assert.Equal($"share/{org.Id}/{game.Id}/{slug}/1", deploy.Path);

      var branch = test.Factory.LoadBranch(deploy.BranchId);
      Assert.Present(branch);
      Assert.Equal("example-label", branch.Slug);
      Assert.True(branch.HasPassword);
      Assert.Equal("you shall not pass", branch.DecryptPassword(test.Encryptor));

      Assert.Domain.NoJobsEnqueued(test);
      Assert.Domain.Files([
        "build.tgz",
        $"share/{org.Id}/{game.Id}/{slug}/1/index.html"
      ], test);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUploadWithLabelInRouteValue()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");

      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var label = "Hello World";

      using var archive = await test.FileStore.CreateTestArchive("build.tgz");

      var response = await test.Post($"/api/{org.Slug}/{game.Slug}/share/{label}", archive);
      var content = Assert.Json.Object(response);
      var id = Assert.Json.Number(content["id"]);
      var slug = Assert.Json.String(content["slug"]);
      var url = Assert.Json.String(content["url"]);

      Assert.True(id > 0);
      Assert.Equal(Format.Slugify(label), slug);
      Assert.Equal($"https://void.test/{org.Slug}/{game.Slug}/serve/{slug}/", url);

      var deploy = test.Factory.LoadDeploy(id);
      Assert.Present(deploy);
      Assert.Equal(id, deploy.Id);
      Assert.Equal(Share.DeployState.Ready, deploy.State);
      Assert.Equal(1, deploy.Number);
      Assert.Equal($"share/{org.Id}/{game.Id}/{slug}/1", deploy.Path);

      var branch = test.Factory.LoadBranch(deploy.BranchId);
      Assert.Present(branch);
      Assert.Equal(slug, branch.Slug);

      Assert.Domain.NoJobsEnqueued(test);
      Assert.Domain.Files([
        "build.tgz",
        $"share/{org.Id}/{game.Id}/{slug}/1/index.html"
      ], test);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUpsertWithExistingSlug()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");

      var org = test.Factory.LoadOrganization("atari");
      var game = test.Factory.LoadGame(org, "pong");
      var label = "example-label";

      using var archive1 = await test.FileStore.CreateTestArchive("archive1.tgz", new (string path, string content)[]
      {
        ("assets/file1.txt", "first"),
        ("assets/file2.txt", "second"),
      });

      using var archive2 = await test.FileStore.CreateTestArchive("archive2.tgz", new (string path, string content)[]
      {
        ("assets/file3.txt", "third"),
        ("assets/file4.txt", "fourth"),
      });

      var response = await test.Post($"/api/{org.Slug}/{game.Slug}/share/{label}", archive1);
      var content = Assert.Json.Object(response);
      var id = Assert.Json.Number(content["id"]);
      var slug = Assert.Json.String(content["slug"]);
      var url = Assert.Json.String(content["url"]);

      Assert.True(id > 0);
      Assert.Equal(Format.Slugify(label), slug);
      Assert.Equal($"https://void.test/{org.Slug}/{game.Slug}/serve/{slug}/", url);

      var deploy1 = test.Factory.LoadDeploy(id);
      Assert.Equal(Share.DeployState.Ready, deploy1.State);
      Assert.Equal(1, deploy1.Number);
      Assert.Equal($"share/{org.Id}/{game.Id}/{slug}/1", deploy1.Path);

      var branch = test.Factory.LoadBranch(deploy1.BranchId);
      Assert.Equal(slug, branch.Slug);

      Assert.Domain.NoJobsEnqueued(test);
      Assert.Domain.Files([
        "archive1.tgz",
        "archive2.tgz",
        $"share/{org.Id}/{game.Id}/{slug}/1/assets/file1.txt",
        $"share/{org.Id}/{game.Id}/{slug}/1/assets/file2.txt",
      ], test);

      response = await test.Post($"/api/{org.Slug}/{game.Slug}/share/{label}", archive2);
      content = Assert.Json.Object(response);
      id = Assert.Json.Number(content["id"]);
      slug = Assert.Json.String(content["slug"]);
      url = Assert.Json.String(content["url"]);

      var deploy2 = test.Factory.LoadDeploy(id);
      Assert.Equal(Share.DeployState.Ready, deploy2.State);
      Assert.Equal(2, deploy2.Number);
      Assert.Equal($"share/{org.Id}/{game.Id}/{slug}/2", deploy2.Path);
      Assert.Equal($"https://void.test/{org.Slug}/{game.Slug}/serve/{slug}/", url);

      Assert.Domain.Files([
        "archive1.tgz",
        "archive2.tgz",
        $"share/{org.Id}/{game.Id}/{slug}/1/assets/file1.txt",
        $"share/{org.Id}/{game.Id}/{slug}/1/assets/file2.txt",
        $"share/{org.Id}/{game.Id}/{slug}/2/assets/file3.txt",
        $"share/{org.Id}/{game.Id}/{slug}/2/assets/file4.txt",
      ], test);

      var job = Assert.Domain.Enqueued<MoveToTrashMinion, MoveToTrashMinion.Data>(test);
      Assert.Equal(deploy1.Path, job.Path);
      Assert.Equal($"deploy has been replaced", job.Reason);
    }
  }

  //===============================================================================================
  // TEST route/auth failures
  //===============================================================================================

  [Fact]
  public async Task TestUploadUnknownOrg()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");
      var org = "unknown";
      var game = "pong";
      var response = await test.Post($"/api/{org}/{game}/share");
      Assert.Http.NotFound(response);
      Assert.Domain.NoFilesSaved(test);
      Assert.Domain.NoJobsEnqueued(test);
    }
  }

  [Fact]
  public async Task TestUploadUnknownGame()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");
      var org = "atari";
      var game = "unknown";
      var response = await test.Post($"/api/{org}/{game}/share");
      Assert.Http.NotFound(response);
      Assert.Domain.NoFilesSaved(test);
      Assert.Domain.NoJobsEnqueued(test);
    }
  }

  [Fact]
  public async Task TestUploadUnauthorized()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("outsider");
      var org = "atari";
      var game = "pong";
      var response = await test.Post($"/api/{org}/{game}/share");
      Assert.Http.NotFound(response);
      Assert.Domain.NoFilesSaved(test);
      Assert.Domain.NoJobsEnqueued(test);
    }
  }

  [Fact]
  public async Task TestUploadAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var org = "atari";
      var game = "pong";
      var response = await test.Post($"/api/{org}/{game}/share");
      Assert.Http.Unauthorized(response);
      Assert.Domain.NoFilesSaved(test);
      Assert.Domain.NoJobsEnqueued(test);
    }
  }

  //-----------------------------------------------------------------------------------------------
}