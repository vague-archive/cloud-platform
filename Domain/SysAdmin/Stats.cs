namespace Void.Platform.Domain;

public partial class SysAdmin
{
  //===============================================================================================
  // DATABASE STATS
  //===============================================================================================

  public static readonly string DatabaseStatsCacheKey = CacheKey.ForSysAdminDatabaseStats();
  public static readonly Duration DatabaseStatsCacheDuration = Duration.FromHours(1); // quick to recalculate, we can do it often

  //-----------------------------------------------------------------------------------------------

  public record DatabaseStats
  {
    public int Organizations { get; init; }
    public int Users { get; init; }
    public int Tokens { get; init; }
    public int Games { get; init; }
    public int Branches { get; init; }
    public int Deploys { get; init; }
    public int Tools { get; init; }
    public required Instant CalculatedOn { get; set; }
  }

  //-----------------------------------------------------------------------------------------------

  public async Task InvalidateDatabaseStats()
  {
    await Cache.RemoveAsync(DatabaseStatsCacheKey);
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<DatabaseStats> RefreshDatabaseStats()
  {
    await InvalidateDatabaseStats();
    return await GetDatabaseStats();
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<DatabaseStats> GetDatabaseStats()
  {
    return await Cache.GetOrSetAsync<DatabaseStats>(DatabaseStatsCacheKey, (_, _) =>
    {
      var start = Clock.Now;
      Logger.Warning("[SYSADMIN] RECALCULATING DATABASE STATS");
      var stats = ReCalculateDatabaseStats();
      Logger.Warning("[SYSADMIN] RECALCULATED DATABASE STATS IN {duration}", Format.Duration(Clock.Now - start));
      return Task.FromResult(stats);
    },
    options: new CacheEntryOptions(DatabaseStatsCacheDuration));
  }

  //-----------------------------------------------------------------------------------------------

  private DatabaseStats ReCalculateDatabaseStats()
  {
    var stats = Db.QuerySingle<DatabaseStats>(@"
      SELECT
        (SELECT COUNT(id) FROM organizations) as Organizations,
        (SELECT COUNT(id) FROM users) as Users,
        (SELECT COUNT(id) FROM tokens) as Tokens,
        (SELECT COUNT(id) FROM games WHERE purpose = 'game') as Games,
        (SELECT COUNT(id) FROM games WHERE purpose = 'tool') as Tools,
        (SELECT COUNT(id) FROM branches) as Branches,
        (SELECT COUNT(id) FROM deploys) as Deploys
    ");
    stats.CalculatedOn = Now;
    return stats;
  }

  //===============================================================================================
  // FILE STATS
  //===============================================================================================

  public static readonly string FileStatsCacheKey = CacheKey.ForSysAdminFileStats();
  public static readonly Duration FileStatsCacheDuration = Duration.FromDays(7); // very slow to recalculate, best done on demand

  public record FileStats
  {
    public bool HasRemote { get; init; }
    public long LocalFileCount { get; init; }
    public long LocalByteCount { get; init; }
    public long RemoteFileCount { get; init; }
    public long RemoteByteCount { get; init; }
    public bool HasMissingLocal { get; init; }
    public bool HasMissingRemote { get; init; }
    public required List<string> MissingLocal { get; init; }
    public required List<string> MissingRemote { get; init; }
    public required Instant CalculatedOn { get; set; }
  }

  public async Task InvalidateFileStats()
  {
    await Cache.RemoveAsync(FileStatsCacheKey);
  }

  public async Task<FileStats?> GetFileStats()
  {
    var result = await Cache.TryGetAsync<FileStats>(FileStatsCacheKey);
    return result.HasValue ? result.Value : null;
  }

  public async Task<FileStats> RefreshFileStats()
  {
    var start = Clock.Now;
    Logger.Warning("[SYSADMIN] RECALCULATING FILE STATS");
    var stats = await ReCalculateFileStats();
    Logger.Warning("[SYSADMIN] RECALCULATED FILE STATS IN {duration}", Format.Duration(Clock.Now - start));
    await Cache.SetAsync(FileStatsCacheKey, stats, new CacheEntryOptions(FileStatsCacheDuration));
    return stats;
  }

  //-----------------------------------------------------------------------------------------------

  private async Task<FileStats> ReCalculateFileStats()
  {
    var localTask = ListLocalFileStore();
    var remoteTask = FileStore.HasRemote ? ListRemoteFileStore() : Task.FromResult(new List<FileStoreStat>());

    await Task.WhenAll([
      localTask,
      remoteTask,
    ]);

    var local = localTask.Result;
    var remote = remoteTask.Result;

    var localFiles = localTask.Result.Select(f => f.Name).ToHashSet();
    var remoteFiles = remoteTask.Result.Select(f => f.Name).ToHashSet();

    var missingLocal = remoteFiles.Except(localFiles).ToList();
    var missingRemote = localFiles.Except(remoteFiles).ToList();

    return new FileStats
    {
      HasRemote = FileStore.HasRemote,
      LocalFileCount = local.Count,
      LocalByteCount = local.Select(f => f.Size).Sum(),
      RemoteFileCount = remote.Count,
      RemoteByteCount = remote.Select(f => f.Size).Sum(),
      HasMissingLocal = missingLocal.Count > 0,
      HasMissingRemote = missingRemote.Count > 0,
      MissingLocal = missingLocal,
      MissingRemote = missingRemote,
      CalculatedOn = Now,
    };
  }

  private async Task<List<FileStoreStat>> ListLocalFileStore()
  {
    var start = Clock.Now;
    Logger.Warning("[SYSADMIN] RE-LISTING LOCAL FILE STORE");
    var list = await FileStore.List();
    Logger.Warning("[SYSADMIN] RE-LISTED LOCAL FILE STORE IN {duration}", Format.Duration(Clock.Now - start));
    return list;
  }

  private async Task<List<FileStoreStat>> ListRemoteFileStore()
  {
    var start = Clock.Now;
    Logger.Warning("[SYSADMIN] RE-LISTING REMOTE FILE STORE");
    var list = await FileStore.ListRemote();
    Logger.Warning("[SYSADMIN] RE-LISTED REMOTE FILE STORE IN {duration}", Format.Duration(Clock.Now - start));
    return list;
  }

  //-----------------------------------------------------------------------------------------------
}