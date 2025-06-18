namespace Void.Platform.Domain;

public partial class Share
{
  //-----------------------------------------------------------------------------------------------

  public record Branch
  {
    //=============================================================================================
    // PROPERTIES
    //=============================================================================================

    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long GameId { get; set; }
    public required string Slug { get; set; }
    public Boolean IsPinned { get; set; }
    public long? ActiveDeployId { get; set; }
    public long? LatestDeployId { get; set; }
    public Instant CreatedOn { get; set; }
    public Instant UpdatedOn { get; set; }

    //=============================================================================================
    // ASSOCIATIONS
    //=============================================================================================

    [DatabaseIgnore] public Account.Organization? Organization { get; set; }
    [DatabaseIgnore] public Account.Game? Game { get; set; }
    [DatabaseIgnore] public Share.Deploy? ActiveDeploy { get; set; }
    [DatabaseIgnore] public Share.Deploy? LatestDeploy { get; set; }

    //=============================================================================================
    // PASSWORDS
    //=============================================================================================

    public string? EncryptedPassword { get; set; }
    private string? DecryptedPassword { get; set; }

    public Branch()
    {
    }

    public Branch(string? password, Crypto.Encryptor encryptor)
    {
      SetPassword(password, encryptor);
    }

    public bool HasPassword
    {
      get
      {
        return EncryptedPassword is not null;
      }
    }

    [DatabaseIgnore]
    public string? Password
    {
      get
      {
        if (EncryptedPassword is null)
        {
          return null;
        }
        else if (DecryptedPassword is not null)
        {
          return DecryptedPassword;
        }
        else
        {
          throw new Exception("must call DecryptPassword() first");
        }
      }
    }

    public string? DecryptPassword(Crypto.Encryptor encryptor)
    {
      return DecryptedPassword = encryptor.Decrypt(EncryptedPassword);
    }

    public void SetPassword(string? password, Crypto.Encryptor encryptor)
    {
      DecryptedPassword = password;
      EncryptedPassword = encryptor.Encrypt(password);
    }
  }

  //===============================================================================================
  // DATABASE FIELDS
  //===============================================================================================

  public const string BranchFields = @"
    branches.id               as Id,
    branches.organization_id  as OrganizationId,
    branches.game_id          as GameId,
    branches.slug             as Slug,
    branches.password         as EncryptedPassword,
    branches.pinned           as IsPinned,
    branches.active_deploy_id as ActiveDeployId,
    branches.latest_deploy_id as LatestDeployId,
    branches.created_on       as CreatedOn,
    branches.updated_on       as UpdatedOn
  ";

  //===============================================================================================
  // GET BRANCHES
  //===============================================================================================

  public Branch? GetBranch(long id)
  {
    return WithDeploys(Db.QuerySingleOrDefault<Branch>(@$"
      SELECT {BranchFields}
      FROM branches
      WHERE id = @Id
    ", new { Id = id }));
  }

  //-----------------------------------------------------------------------------------------------

  public Branch? GetBranch(Account.Game game, string slug)
  {
    return WithDeploys(Db.QuerySingleOrDefault<Branch>(@$"
      SELECT {BranchFields}
        FROM branches
       WHERE game_id = @GameId
         AND slug = @Slug
    ", new
    {
      GameId = game.Id,
      Slug = slug,
    }));
  }

  //-----------------------------------------------------------------------------------------------

  public List<Branch> GetActiveBranchesForGame(Account.Game game)
  {
    return GetActiveBranchesForGame([game])[game.Id];
  }

  public Dictionary<long, List<Branch>> GetActiveBranchesForGame(IEnumerable<Account.Game> games)
  {
    var gameIds = games.Select(g => g.Id);
    var branches = WithDeploys(Db.Query<Branch>($"""
      SELECT {BranchFields}
        FROM branches
        JOIN deploys on deploys.id = branches.active_deploy_id
       WHERE branches.game_id IN @Ids
    ORDER BY branches.game_id, branches.pinned desc, deploys.deployed_on desc
    """, new { Ids = gameIds }));
    var index = new Dictionary<long, List<Branch>>();
    foreach (var game in games)
      index[game.Id] = branches.Where(b => b.GameId == game.Id).ToList();
    return index;
  }

  //-----------------------------------------------------------------------------------------------

  private Branch? WithDeploys(Branch? branch)
  {
    if (branch is null)
      return null;
    else
      return WithDeploys([branch]).First();
  }

  private IEnumerable<Branch> WithDeploys(IEnumerable<Branch> branches)
  {
    var deployIds = new HashSet<long>();
    foreach (var branch in branches)
    {
      if (branch.ActiveDeployId.HasValue)
        deployIds.Add(branch.ActiveDeployId.Value);
      if (branch.LatestDeployId.HasValue)
        deployIds.Add(branch.LatestDeployId.Value);
    }

    var deploys = Db.Query<Deploy>($"""
      SELECT {DeployFields}
        FROM deploys
       WHERE id IN @Ids
    """, new { Ids = deployIds });

    var users = Db.Query<Account.User>($"""
      SELECT {Account.UserFields}
        FROM users
       WHERE id IN @Ids
    """, new { Ids = deploys.Select(d => d.DeployedBy) });

    var userIndex = new Dictionary<long, Account.User>();
    foreach (var user in users)
      userIndex.Add(user.Id, user);

    var deployIndex = new Dictionary<long, Deploy>();
    foreach (var deploy in deploys)
      deployIndex.Add(deploy.Id, deploy);

    foreach (var deploy in deploys)
      deploy.DeployedByUser = userIndex[deploy.DeployedBy];

    foreach (var branch in branches)
    {
      if (branch.ActiveDeployId.HasValue)
        branch.ActiveDeploy = deployIndex[branch.ActiveDeployId.Value];
      if (branch.LatestDeployId.HasValue)
        branch.LatestDeploy = deployIndex[branch.LatestDeployId.Value];
    }

    return branches;
  }

  //===============================================================================================
  // PIN BRANCH
  //===============================================================================================

  public Result<Branch> PinBranch(Share.Branch branch, bool isPinned = true)
  {
    branch.IsPinned = isPinned;
    var numRows = Db.Execute(@"
      UPDATE branches
         SET pinned = @IsPinned
       WHERE id = @Id
    ", branch);
    RuntimeAssert.True(numRows == 1);
    return Result.Ok(branch);
  }

  //-----------------------------------------------------------------------------------------------

  public Result<Branch> UnpinBranch(Share.Branch branch)
  {
    return PinBranch(branch, false);
  }

  //===============================================================================================
  // SET BRANCH PASSWORD
  //===============================================================================================

  public Result<Branch> SetBranchPassword(Share.Branch branch, string? password)
  {
    branch.SetPassword(password, Encryptor);
    var numRows = Db.Execute(@"
      UPDATE branches
         SET password = @EncryptedPassword
       WHERE id = @Id
    ", branch);
    RuntimeAssert.True(numRows == 1);
    return Result.Ok(branch);
  }

  //-----------------------------------------------------------------------------------------------

  public Result<Branch> ClearBranchPassword(Share.Branch branch)
  {
    return SetBranchPassword(branch, null);
  }

  //===============================================================================================
  // DELETE BRANCH
  //===============================================================================================

  public void DeleteBranch(Share.Branch branch)
  {
    Db.Transaction(branch, () =>
    {
      // need to NULL out cyclic FK constraints first
      var numRows = Db.Execute("""
        UPDATE branches
           SET active_deploy_id = NULL,
               latest_deploy_id = NULL
         WHERE id = @Id
      """, branch);
      RuntimeAssert.True(numRows == 1);

      numRows = Db.Execute(@"
        DELETE FROM branches
              WHERE id = @Id
      ", branch);
      RuntimeAssert.True(numRows == 1);
      return true;

    });
    MoveToTrashMinion.Enqueue(Minions, Share.DeployPath(branch), "branch deleted");
  }

  //-----------------------------------------------------------------------------------------------
}