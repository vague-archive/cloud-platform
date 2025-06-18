namespace Void.Platform.Domain;

public partial class SysAdmin : SubDomain
{
  //-----------------------------------------------------------------------------------------------

  public SysAdmin(Application app) : base(app)
  {
  }

  //-----------------------------------------------------------------------------------------------

  public List<Account.Organization> AllOrganizations()
  {
    return Db.Query<Account.Organization>(@"
      SELECT id,
             name,
             slug,
             created_on as CreatedOn,
             updated_on as UpdatedOn
      FROM organizations
      ORDER BY name
    ");
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<List<Share.Branch>> RecentBranches(Duration duration)
  {
    var since = Clock.Now.Minus(duration);
    return (await App.Db.SplitQueryAsync<Account.Organization, Account.Game, Share.Branch, Share.Deploy, Share.Deploy, Account.User, Share.Branch>($"""
      SELECT
        {Account.OrganizationFields},
        'game' as Game,
        {Account.GameFields},
        'branch' as Branch,
        {Share.BranchFields},
        'active' as ActiveDeploy,
        {Share.DeployFieldsFor("active")},
        'latest' as LatestDeploy,
        {Share.DeployFieldsFor("latest")},
        'user' as LatestDeployedBy,
        {Account.UserFields}
      FROM branches
      INNER JOIN deploys as latest on latest.id = branches.latest_deploy_id
      INNER JOIN deploys as active on active.id = branches.active_deploy_id
      INNER JOIN games on games.id = branches.game_id
      INNER JOIN organizations on organizations.id = branches.organization_id
      INNER JOIN users on users.id = latest.deployed_by
      WHERE latest.deployed_on >= @Since
   ORDER BY latest.deployed_on DESC
      LIMIT 50
   """, new { Since = since }, (org, game, branch, active, latest, user) =>
    {
      branch.Organization = org;
      branch.Game = game;
      branch.ActiveDeploy = active;
      branch.LatestDeploy = latest;
      branch.LatestDeploy.DeployedByUser = user;
      return branch;
    }, splitOn: "Game, Branch, ActiveDeploy, LatestDeploy, LatestDeployedBy")).ToList();
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<List<Share.Branch>> ExpiredBranches(Duration duration)
  {
    var since = Clock.Now.Minus(duration);
    return (await App.Db.SplitQueryAsync<Account.Organization, Account.Game, Account.User, Share.Branch, Share.Deploy, Share.Branch>($"""
      SELECT
        {Account.OrganizationFields},
        'game' as Game,
        {Account.GameFields},
        'user' as User,
        {Account.UserFields},
        'branch' as Branch,
        {Share.BranchFields},
        'deploy' as Deploy,
        {Share.DeployFields}
      FROM branches
      INNER JOIN deploys on deploys.id = branches.active_deploy_id
      INNER JOIN games on games.id = branches.game_id
      INNER JOIN organizations on organizations.id = branches.organization_id
      INNER JOIN users on users.id = deploys.deployed_by
      WHERE deploys.deployed_on <= @Since
        AND branches.pinned = false
   ORDER BY deploys.deployed_on DESC
   """, new { Since = since }, (org, game, user, branch, deploy) =>
    {
      branch.Organization = org;
      branch.Game = game;
      branch.ActiveDeploy = deploy;
      branch.ActiveDeploy.DeployedByUser = user;
      return branch;
    }, splitOn: "Game, User, Branch, Deploy")).ToList();
  }

  //-----------------------------------------------------------------------------------------------
}