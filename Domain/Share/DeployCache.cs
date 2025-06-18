namespace Void.Platform.Domain;

public partial class Share
{
  //-----------------------------------------------------------------------------------------------

  public record CachedDeployInfo
  {
    public Account.GamePurpose Purpose { get; init; }
    public required string FilePath { get; init; }

    public static CachedDeployInfo New(Account.Game game, Deploy deploy)
    {
      return new CachedDeployInfo
      {
        Purpose = game.Purpose,
        FilePath = deploy.Path,
      };
    }
  }

  //-----------------------------------------------------------------------------------------------

  private CacheEntryOptions CachedDeployEntryOptions =
    new CacheEntryOptions(Duration.FromHours(1));

  public async Task<CachedDeployInfo?> GetCachedDeployInfo(string orgSlug, string gameSlug, string branchSlug)
  {
    var cacheKey = CacheKey.ForGameServe(orgSlug, gameSlug, branchSlug);
    return await Cache.GetOrSetAsync<CachedDeployInfo?>(cacheKey, (_, _) =>
    {
      var result = LoadDeployInfo(orgSlug, gameSlug, branchSlug);
      if (result is not null)
        Logger.Warning($"[CACHE] {cacheKey} MISSED - set to {result.FilePath} (server: {App.Config.ServerName})");
      else
        Logger.Warning($"[CACHE] {cacheKey} MISSED - and not found (server: {App.Config.ServerName})");
      return Task.FromResult<CachedDeployInfo?>(result);
    }, options: CachedDeployEntryOptions);
  }

  public async Task<CachedDeployInfo> SetCachedDeployInfo(Account.Organization org, Account.Game game, Share.Branch branch, Share.Deploy deploy)
  {
    var cacheKey = CacheKey.ForGameServe(org.Slug, game.Slug, branch.Slug);
    var info = CachedDeployInfo.New(game, deploy);
    await Cache.SetAsync(cacheKey, info, options: CachedDeployEntryOptions);
    Logger.Warning($"[CACHE] {cacheKey} SET - to {info.FilePath} (server: {App.Config.ServerName})");
    return info;
  }

  private CachedDeployInfo? LoadDeployInfo(string orgSlug, string gameSlug, string branchSlug)
  {
    var org = App.Account.GetOrganization(orgSlug);
    if (org is null)
      return null;
    var game = App.Account.GetGame(org, gameSlug);
    if (game is null)
      return null;
    var branch = App.Share.GetBranch(game, branchSlug);
    if (branch is null)
      return null;
    var deploy = branch.ActiveDeploy;
    if (deploy is null)
      return null;
    return CachedDeployInfo.New(game, deploy);
  }

  //-----------------------------------------------------------------------------------------------
}