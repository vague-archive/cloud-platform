namespace Void.Platform.Domain;

public partial class Share
{
  //-----------------------------------------------------------------------------------------------

  public enum DeployState
  {
    Deploying,
    Ready,
    Failed,
  }

  //-----------------------------------------------------------------------------------------------

  public record Deploy
  {
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long GameId { get; set; }
    public long BranchId { get; set; }
    public required string Path { get; set; }
    public DeployState State { get; set; }
    public int Number { get; set; }
    public string? Error { get; set; }
    public Instant? DeployingOn { get; set; }
    public long DeployedBy { get; set; }
    public Instant DeployedOn { get; set; }
    public Instant? FailedOn { get; set; }
    public Instant CreatedOn { get; set; }
    public Instant UpdatedOn { get; set; }
    public Instant? DeletedOn { get; set; }
    public string? DeletedReason { get; set; }

    [DatabaseIgnore] public Account.User? DeployedByUser { get; set; }
    [DatabaseIgnore] public Account.Organization? Organization { get; set; }
    [DatabaseIgnore] public Account.Game? Game { get; set; }
    [DatabaseIgnore] public Share.Branch? Branch { get; set; }

    public bool IsDeploying { get { return State == DeployState.Deploying; } }
    public bool IsReady { get { return State == DeployState.Ready; } }
    public bool HasFailed { get { return State == DeployState.Failed; } }
  }

  //-----------------------------------------------------------------------------------------------

  public class DeployAsset
  {
    public required string Path { get; init; }
    public required string Blake3 { get; init; }
    public long ContentLength { get; init; }
  }

  //-----------------------------------------------------------------------------------------------

  public readonly static string DeployFields = DeployFieldsFor();

  public static string DeployFieldsFor(string prefix = "deploys")
  {
    return $"""
    {prefix}.id              as Id,
    {prefix}.organization_id as OrganizationId,
    {prefix}.game_id         as GameId,
    {prefix}.branch_id       as BranchId,
    {prefix}.path            as Path,
    {prefix}.state           as State,
    {prefix}.number          as Number,
    {prefix}.error           as Error,
    {prefix}.deploying_on    as DeployingOn,
    {prefix}.deployed_by     as DeployedBy,
    {prefix}.deployed_on     as DeployedOn,
    {prefix}.failed_on       as FailedOn,
    {prefix}.created_on      as CreatedOn,
    {prefix}.updated_on      as UpdatedOn,
    {prefix}.deleted_on      as DeletedOn,
    {prefix}.deleted_reason  as DeletedReason
    """;
  }

  //-----------------------------------------------------------------------------------------------

  public static string DeployPath(Account.Game game) =>
    DeployPath(game.OrganizationId, game.Id);

  public static string DeployPath(Branch branch) =>
    DeployPath(branch.OrganizationId, branch.GameId, branch.Slug);

  public static string DeployPath(Branch branch, int number) =>
    DeployPath(branch.OrganizationId, branch.GameId, branch.Slug, number);

  //-----------------------------------------------------------------------------------------------

  private static string DeployPath(long orgId, long gameId) =>
    Path.Combine("share", orgId.ToString(), gameId.ToString());

  private static string DeployPath(long orgId, long gameId, string slug) =>
    Path.Combine(DeployPath(orgId, gameId), slug);

  private static string DeployPath(long orgId, long gameId, string slug, int number) =>
    Path.Combine(DeployPath(orgId, gameId), slug, number.ToString());

  //===============================================================================================
  // GET DEPLOY
  //===============================================================================================

  public Deploy? GetDeploy(long id)
  {
    return Db.QuerySingleOrDefault<Deploy>(@$"
      SELECT {DeployFields}
      FROM deploys
      WHERE id = @Id
    ", new { Id = id });
  }

  //===============================================================================================
  // DELETE DEPLOY
  //===============================================================================================

  public void DeleteDeploy(Deploy deploy, string reason)
  {
    deploy.DeletedOn = Now;
    deploy.DeletedReason = reason;
    var numRows = Db.Execute("""
      UPDATE deploys
         SET deleted_on = @DeletedOn,
             deleted_reason = @DeletedReason
       WHERE id = @Id
    """, deploy);
    RuntimeAssert.True(numRows == 1);
    MoveToTrashMinion.Enqueue(Minions, deploy.Path, reason);
  }

  //-----------------------------------------------------------------------------------------------
}