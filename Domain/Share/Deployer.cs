namespace Void.Platform.Domain;

public partial class Share
{
  public const string ManifestFile = "void-deploy.json";
  public const int MaterializeConcurrency = 8;

  //===============================================================================================
  // FULL DEPLOY
  //===============================================================================================

  public record FullDeployCommand
  {
    public required Stream Archive { get; init; }
    public required Account.Organization Organization { get; init; }
    public required Account.Game Game { get; init; }
    public required Account.User DeployedBy { get; init; }
    public required string Slug { get; init; }
    public string? Password { get; init; } = null; // ONLY on INSERT, ignored on UPSERT

    public class Validator : AbstractValidator<FullDeployCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Archive).NotNull();
        RuleFor(cmd => cmd.Organization).NotNull();
        RuleFor(cmd => cmd.Game).NotNull();
        RuleFor(cmd => cmd.DeployedBy).NotNull();
        RuleFor(cmd => cmd.Slug)
          .NotEmpty().WithMessage(Validation.IsMissing)
          .MaximumLength(255).WithMessage(Validation.TooLong(255));
      }
    }
  }

  public async Task<Result<Deploy>> FullDeploy(FullDeployCommand cmd)
  {
    var deployer = new Deployer
    {
      Clock = Clock,
      Logger = Logger,
      App = App,
    };

    return await deployer.FullDeploy(cmd);
  }

  //===============================================================================================
  // INCREMENTAL DEPLOY
  //===============================================================================================

  public record IncrementalDeployCommand
  {
    public required DeployAsset[] Manifest { get; init; }
    public required Account.Organization Organization { get; init; }
    public required Account.Game Game { get; init; }
    public required Account.User DeployedBy { get; init; }
    public required string Slug { get; init; }
    public string? Password { get; init; } = null; // ONLY on INSERT, ignored on UPSERT

    public class Validator : AbstractValidator<IncrementalDeployCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Manifest).NotNull();
        RuleFor(cmd => cmd.Organization).NotNull();
        RuleFor(cmd => cmd.Game).NotNull();
        RuleFor(cmd => cmd.DeployedBy).NotNull();
        RuleFor(cmd => cmd.Slug)
          .NotEmpty().WithMessage(Validation.IsMissing)
          .MaximumLength(255).WithMessage(Validation.TooLong(255));
      }
    }
  }

  public async Task<Result<(Deploy, DeployAsset[])>> IncrementalDeploy(IncrementalDeployCommand cmd)
  {
    var deployer = new Deployer
    {
      Clock = Clock,
      Logger = Logger,
      App = App,
    };

    return await deployer.IncrementalDeploy(cmd);
  }

  //===============================================================================================
  // UPLOAD DEPLOY ASSET
  //===============================================================================================

  public record UploadDeployAssetCommand
  {
    public required Account.Organization Organization { get; init; }
    public required Account.Game Game { get; init; }
    public required Deploy Deploy { get; init; }
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }

    public class Validator : AbstractValidator<UploadDeployAssetCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Organization).NotNull();
        RuleFor(cmd => cmd.Game).NotNull();
        RuleFor(cmd => cmd.Deploy).NotNull();
        RuleFor(cmd => cmd.Content).NotNull();
        RuleFor(cmd => cmd.ContentType).NotEmpty();
      }
    }
  }

  public async Task<Result<(bool, Content.Object)>> UploadDeployAsset(UploadDeployAssetCommand cmd)
  {
    var deployer = new Deployer
    {
      Clock = Clock,
      Logger = Logger,
      App = App,
    };

    return await deployer.UploadDeployAsset(cmd);
  }

  //===============================================================================================
  // ACTIVATE INCREMENTAL DEPLOY
  //===============================================================================================

  public record ActivateIncrementalDeployCommand
  {
    public required Account.Organization Organization { get; init; }
    public required Account.Game Game { get; init; }
    public required Branch Branch { get; init; }
    public required Deploy Deploy { get; init; }
    public int Concurrency { get; init; }

    public class Validator : AbstractValidator<ActivateIncrementalDeployCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Organization).NotNull();
        RuleFor(cmd => cmd.Game).NotNull();
        RuleFor(cmd => cmd.Deploy).NotNull();
        RuleFor(cmd => cmd.Branch).NotNull();
        RuleFor(cmd => cmd).Must(cmd => cmd.Deploy.BranchId == cmd.Branch.Id);
        RuleFor(cmd => cmd).Must(cmd => cmd.Branch.GameId == cmd.Game.Id);
        RuleFor(cmd => cmd).Must(cmd => cmd.Game.OrganizationId == cmd.Organization.Id);
      }
    }
  }

  public async Task<Result<Deploy>> ActivateIncrementalDeploy(ActivateIncrementalDeployCommand cmd)
  {
    var deployer = new Deployer
    {
      Clock = Clock,
      Logger = Logger,
      App = App,
    };

    return await deployer.ActivateIncrementalDeploy(cmd);
  }

  //===============================================================================================
  // PRIVATE IMPLEMENTATION USES A SELF CONTAINED CLASS
  //===============================================================================================

  private class Deployer
  {
    //---------------------------------------------------------------------------------------------

    public required IClock Clock { get; init; }
    public required ILogger Logger { get; init; }
    public required Application App { get; init; }

    public IDatabase Db => App.Db;
    public IFileStore FileStore => App.FileStore;
    public Crypto.Encryptor Encryptor => App.Encryptor;

    //---------------------------------------------------------------------------------------------

    public async Task<Result<Deploy>> FullDeploy(FullDeployCommand cmd)
    {
      var validateResult = new FullDeployCommand.Validator().Validate(cmd);
      if (!validateResult.IsValid)
        return Validation.Fail(validateResult);

      var archive = cmd.Archive;
      var org = cmd.Organization;
      var game = cmd.Game;
      var deployedBy = cmd.DeployedBy;
      var password = cmd.Password;
      var slug = cmd.Slug;

      var start = Clock.Now;
      var result = Start(org, game, deployedBy, slug, password);
      if (result.Failed)
        return result;

      var deploy = result.Value;
      var branch = RuntimeAssert.Present(deploy.Branch);
      var identity = $"{org.Slug}/{game.Slug}/{branch.Slug}";
      var target = deploy.Path;

      try
      {
        Logger.Information("[DEPLOY] deploying {identity} to {target}", identity, target);
        await Upload(archive, deploy);
        await Activate(org, game, deploy);
        Logger.Information("[DEPLOY] deployed {identity} to {target} in {duration}", identity, target, Format.Duration(Clock.Now - start));
        return Result.Ok(deploy);
      }
      catch (Exception ex)
      {
        Logger.Report(ex, "[DEPLOY] failed to deploy {identity} to {target} in {duration}", identity, target, Format.Duration(Clock.Now - start));
        Fail(branch, deploy, ex.Message);
        return Result.Fail($"failed to deploy {identity} to {target}: {ex.Message}");
      }
    }

    //=============================================================================================
    // START INCREMENTAL DEPLOY
    //=============================================================================================

    public async Task<Result<(Deploy, DeployAsset[])>> IncrementalDeploy(IncrementalDeployCommand cmd)
    {
      var validateResult = new IncrementalDeployCommand.Validator().Validate(cmd);
      if (!validateResult.IsValid)
        return Validation.Fail(validateResult);

      var org = cmd.Organization;
      var game = cmd.Game;
      var deployedBy = cmd.DeployedBy;
      var password = cmd.Password;
      var slug = cmd.Slug;
      var manifest = cmd.Manifest;

      var result = Start(org, game, deployedBy, slug, password);
      if (result.Failed)
        return Result.Fail(result.Error);

      var deploy = result.Value;

      using (var stream = Json.Serialize(manifest).AsStream())
      {
        await FileStore.Save(Path.Join(deploy.Path, ManifestFile), stream);
      }

      var found = new HashSet<string>(await Db.QueryAsync<string>("""
        SELECT blake3
          FROM content_objects
         WHERE blake3 IN @Hashes
      """, new
      {
        Hashes = manifest.Select(e => e.Blake3)
      }));

      var missing = manifest.Where(e => !found.Contains(e.Blake3)).ToArray();

      return Result.Ok((deploy, missing));
    }

    //=============================================================================================
    // UPLOAD DEPLOY ASSET
    //=============================================================================================

    public async Task<Result<(bool, Content.Object)>> UploadDeployAsset(UploadDeployAssetCommand cmd)
    {
      var validateResult = new UploadDeployAssetCommand.Validator().Validate(cmd);
      if (!validateResult.IsValid)
        return Validation.Fail(validateResult);

      var org = cmd.Organization;
      var game = cmd.Game;
      var deploy = cmd.Deploy;
      var content = cmd.Content;
      var contentType = cmd.ContentType;

      if (deploy.HasFailed)
        return Result.Fail("cannot upload to deploy that has already failed");
      else if (deploy.IsReady)
        return Result.Fail("cannot upload to deploy that has already been activated");

      return await App.Content.Upload(content, contentType);
    }

    //=============================================================================================
    // ACTIVATE INCREMENTAL DEPLOY
    //=============================================================================================

    public async Task<Result<Deploy>> ActivateIncrementalDeploy(ActivateIncrementalDeployCommand cmd)
    {
      var validateResult = new ActivateIncrementalDeployCommand.Validator().Validate(cmd);
      if (!validateResult.IsValid)
        return Validation.Fail(validateResult);

      var org = cmd.Organization;
      var game = cmd.Game;
      var branch = cmd.Branch;
      var deploy = cmd.Deploy;
      var concurrency = cmd.Concurrency > 0 ? cmd.Concurrency : MaterializeConcurrency;
      var identity = $"{org.Slug}/{game.Slug}/{branch.Slug}";

      if (deploy.HasFailed)
        return Result.Fail("cannot activate deploy that has already failed");
      else if (deploy.IsReady)
        return Result.Fail("cannot activate deploy that has already been activated");

      var start = Clock.Now;
      try
      {
        Logger.Information("[INCREMENTAL-DEPLOY] activating {identity} to {path} (concurrency: {concurrency})", identity, deploy.Path, concurrency);
        var manifest = await LoadManifest(deploy);
        await Materialize(deploy, manifest, concurrency);
        await Activate(org, game, deploy);
        Logger.Information("[INCREMENTAL-DEPLOY] activated {identity} to {path} in {duration}", identity, deploy.Path, Format.Duration(Clock.Now - start));
        return Result.Ok(deploy);
      }
      catch (Exception ex)
      {
        Logger.Report(ex, "[INCREMENTAL-DEPLOY] failed to activate {identity} to {path} in {duration}", identity, deploy.Path, Format.Duration(Clock.Now - start));
        Fail(branch, deploy, ex.Message);
        return Result.Fail($"failed to activate {identity} to {deploy.Path}: {ex.Message}");
      }
    }

    //=============================================================================================
    // PRIVATE IMPLEMENTATION DETAILS
    //=============================================================================================

    private Result<Deploy> Start(Account.Organization org, Account.Game game, Account.User deployedBy, string slug, string? password)
    {
      return Db.Transaction(game, () =>
      {
        var branch = App.Share.GetBranch(game, slug);
        if (branch is null)
        {
          var branchResult = CreateBranch(org, game, slug, password);
          if (branchResult.Failed)
            return Result.Fail(branchResult.Error);
          branch = branchResult.Value;
        }

        var deploy = CreateDeploy(org, game, deployedBy, branch);
        TrackLatestDeploy(branch, deploy);

        return Result.Ok(deploy);
      });
    }

    //-----------------------------------------------------------------------------------------------

    private async Task Activate(Account.Organization org, Account.Game game, Deploy deploy)
    {
      RuntimeAssert.True(deploy.IsDeploying);
      await Db.Transaction(game, async () =>
      {
        RegisterSuccess(deploy);

        var branch = deploy.Branch = App.Share.GetBranch(deploy.BranchId); // MUST reload branch within transaction in case active deploy has changed externally (e.g. another concurrent deploy)
        if (branch is null)
        {
          App.Share.DeleteDeploy(deploy, "branch no longer exists");
        }
        else if (branch.ActiveDeploy is not null && branch.ActiveDeploy.Number > deploy.Number)
        {
          App.Share.DeleteDeploy(deploy, "deploy has been superceeded");
        }
        else
        {
          var previousDeploy = TrackActiveDeploy(branch, deploy);
          if (previousDeploy is not null)
            App.Share.DeleteDeploy(previousDeploy, "deploy has been replaced");
          await App.Share.SetCachedDeployInfo(org, game, branch, deploy);
        }
      });
    }

    //-----------------------------------------------------------------------------------------------

    private void Fail(Branch branch, Deploy deploy, string error)
    {
      RuntimeAssert.True(deploy.IsDeploying);
      Db.Transaction(branch, () =>
      {
        RegisterFailure(deploy, error);
        App.Share.DeleteDeploy(deploy, "deploy failed");
        return deploy;
      });
    }

    //-----------------------------------------------------------------------------------------------

    private async Task<DeployAsset[]> LoadManifest(Deploy deploy)
    {
      Share.DeployAsset[] manifest;
      using (var stream = await FileStore.Load(Path.Join(deploy.Path, ManifestFile)))
      {
        RuntimeAssert.Present(stream);
        var content = await stream.ReadAsString();
        manifest = Json.Deserialize<Share.DeployAsset[]>(content);
      }
      return manifest;
    }

    //-----------------------------------------------------------------------------------------------

    private async Task Materialize(Deploy deploy, DeployAsset[] manifest, int concurrency)
    {
      RuntimeAssert.True(concurrency > 0);
      var semaphore = new SemaphoreSlim(concurrency);
      var tasks = new List<Task>();

      foreach (var asset in manifest)
      {
        await semaphore.WaitAsync();
        tasks.Add(Task.Run(async () =>
        {
          try
          {
            var from = Content.ContentPath(asset.Blake3);
            var to = Path.Join(deploy.Path, asset.Path);
            var stream = await App.FileStore.Load(from);
            RuntimeAssert.Present(stream);
            await App.FileStore.Save(to, stream);
          }
          finally
          {
            semaphore.Release();
          }
        }));
      }

      await Task.WhenAll(tasks);
    }

    //-----------------------------------------------------------------------------------------------

    // we have to load the archive entries one at a time, but that doesn't mean we
    // have to save them to EFS/S3 one at a time, we can load them into memory and
    // then send them off to EFS/S3 asynchronously while we start loading the next
    // entrie(s), but we do need to limit the amount of concurrency. Ideally based
    // on total size of files, but for simplicity to start with lets just pick an
    // arbitrary number of concurrent file uploads

    const int UPLOAD_CONCURRENCY = 100;

    private async Task Upload(Stream archive, Deploy deploy)
    {
      SemaphoreSlim semaphore = new SemaphoreSlim(UPLOAD_CONCURRENCY);
      List<Task> uploads = new List<Task>();

      await foreach (var entry in Archive.Extract(archive))
      {
        var fileName = Path.Combine(deploy.Path, entry.Name);
        var buffer = await entry.Content.ReadIntoMemory(entry.Length);
        uploads.Add(Task.Run(async () =>
        {
          await semaphore.WaitAsync();
          try
          {
            await FileStore.Save(fileName, buffer);
          }
          finally
          {
            await buffer.DisposeAsync();
            semaphore.Release();
          }
        }));
      }
      await Task.WhenAll(uploads);
    }

    //-----------------------------------------------------------------------------------------------

    private Result<Branch> CreateBranch(Account.Organization org, Account.Game game, string slug, string? password)
    {
      try
      {
        var branch = new Branch
        {
          Id = -1,
          OrganizationId = org.Id,
          Organization = org,
          GameId = game.Id,
          Game = game,
          Slug = slug,
          EncryptedPassword = Encryptor.Encrypt(password),
          CreatedOn = Clock.Now,
          UpdatedOn = Clock.Now,
        };
        var branchId = Db.Insert("""
          INSERT INTO branches (
            organization_id,
            game_id,
            slug,
            password,
            pinned,
            created_on,
            updated_on
          ) VALUES (
            @OrganizationId,
            @GameId,
            @Slug,
            @EncryptedPassword,
            @IsPinned,
            @CreatedOn,
            @UpdatedOn
          )
        """, branch);
        branch.Id = branchId;
        return Result.Ok(branch);
      }
      catch (MySqlConnector.MySqlException ex) when (ex.ErrorCode == MySqlConnector.MySqlErrorCode.DuplicateKeyEntry)
      {
        if (ex.Message.Contains("branches.branches_game_slug_index"))
          return Validation.Fail("slug", "already taken for this game");
        else
          throw;
      }
    }

    //-----------------------------------------------------------------------------------------------

    private Deploy CreateDeploy(Account.Organization org, Account.Game game, Account.User deployedBy, Branch branch)
    {
      var number = (branch.LatestDeploy?.Number ?? 0) + 1;
      var deploy = new Deploy
      {
        Id = -1,
        OrganizationId = org.Id,
        Organization = org,
        GameId = game.Id,
        Game = game,
        BranchId = branch.Id,
        Branch = branch,
        Number = number,
        Path = Share.DeployPath(branch, number),
        State = DeployState.Deploying,
        DeployedBy = deployedBy.Id,
        DeployingOn = Clock.Now,
        DeployedOn = Clock.Now,
        CreatedOn = Clock.Now,
        UpdatedOn = Clock.Now,
      };
      var id = Db.Insert("""
        INSERT INTO deploys (
          organization_id,
          game_id,
          branch_id,
          number,
          path,
          state,
          deployed_by,
          deploying_on,
          deployed_on,
          created_on,
          updated_on
        ) VALUES (
          @OrganizationId,
          @GameId,
          @BranchId,
          @Number,
          @Path,
          @State,
          @DeployedBy,
          @DeployingOn,
          @DeployedOn,
          @CreatedOn,
          @UpdatedOn
        )
      """, deploy);
      deploy.Id = id;
      return deploy;
    }

    //-----------------------------------------------------------------------------------------------

    private void TrackLatestDeploy(Branch branch, Deploy deploy)
    {
      branch.LatestDeployId = deploy.Id;
      branch.LatestDeploy = deploy;
      branch.UpdatedOn = Clock.Now;
      Db.Execute(@"
        UPDATE branches
           SET latest_deploy_id=@LatestDeployId,
               updated_on=@UpdatedOn
         WHERE id=@Id
      ", branch);
    }

    private Deploy? TrackActiveDeploy(Branch branch, Deploy deploy)
    {
      var previous = branch.ActiveDeploy;

      branch.ActiveDeployId = deploy.Id;
      branch.ActiveDeploy = deploy;
      branch.UpdatedOn = Clock.Now;
      Db.Execute(@"
        UPDATE branches
           SET active_deploy_id=@ActiveDeployId,
               updated_on=@UpdatedOn
         WHERE id=@Id
      ", branch);

      return previous;
    }

    //-----------------------------------------------------------------------------------------------

    private void RegisterSuccess(Deploy deploy)
    {
      deploy.State = DeployState.Ready;
      deploy.Error = null;
      deploy.FailedOn = null;
      deploy.DeployingOn = null;
      deploy.DeployedOn = Clock.Now;
      deploy.UpdatedOn = Clock.Now;
      var numRows = Db.Execute(@$"
        UPDATE deploys
           SET state = @State,
               error = NULL,
               failed_on = NULL,
               deploying_on = NULL,
               deployed_on = @DeployedOn,
               updated_on = @UpdatedOn
         WHERE id = @Id
      ", deploy);
      RuntimeAssert.True(numRows == 1);
    }

    //-----------------------------------------------------------------------------------------------

    private void RegisterFailure(Deploy deploy, string error)
    {
      deploy.State = DeployState.Failed;
      deploy.Error = error;
      deploy.DeployingOn = null;
      deploy.DeployedOn = Clock.Now;
      deploy.FailedOn = Clock.Now;
      deploy.UpdatedOn = Clock.Now;
      var numRows = Db.Execute(@$"
        UPDATE deploys
           SET state = @State,
               error = @Error,
               deploying_on = @DeployingOn,
               deployed_on = @DeployedOn,
               failed_on = @FailedOn,
               updated_on = @UpdatedOn
         WHERE id = @Id
      ", deploy);
      RuntimeAssert.True(numRows == 1);
    }

    //-----------------------------------------------------------------------------------------------
  }
}