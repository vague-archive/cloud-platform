namespace Void.Platform.Web;

[HasPageViews]
[Route("/sysadmin")]
[Authorize(Policy.SysAdmin)]
public class SysAdminController : PageController
{
  //===============================================================================================
  // CONSTRUCTOR, DEPENDENCIES, CONSTANTS
  //===============================================================================================

  private static readonly Duration RecentBranchesSince = Duration.FromDays(7);
  private static readonly Duration ExpiredBranchesSince = Duration.FromDays(60);

  //-----------------------------------------------------------------------------------------------

  private Application App { get; init; }
  private Current Current { get; init; }
  private ILogger Logger { get; init; }
  private IMinions Minions { get; init; }

  //-----------------------------------------------------------------------------------------------

  public SysAdminController(Application app, Current current, ILogger logger, IMinions minions)
  {
    App = app;
    Current = current;
    Logger = logger;
    Minions = minions;
  }

  //===============================================================================================
  // MODELS
  //===============================================================================================

  public record StatsModel
  {
    public required SysAdmin.DatabaseStats DatabaseStats { get; init; }
    public required SysAdmin.FileStats? FileStats { get; init; }
  }

  public record PageModel
  {
    public required StatsModel Stats { get; init; }
    public required List<Account.Organization> Organizations { get; init; }
    public required List<Share.Branch> RecentBranches { get; init; }
    public required Duration RecentBranchesSince { get; init; }
    public required List<Share.Branch> ExpiredBranches { get; init; }
    public required Duration ExpiredBranchesSince { get; init; }
  }

  public record CacheKeysModel
  {
    public required List<string> SysAdminStats { get; init; }
    public required List<string> DownloadReleases { get; init; }
    public required List<(string, string)> GameServe { get; init; }
    public required List<string> FirewallBlocked { get; init; }
    public required List<(string, string)> SafeTrash { get; init; }
    public required List<(string, string)> UnsafeTrash { get; init; }
  }

  //===============================================================================================
  // INDEX PAGE
  //===============================================================================================

  [HttpGet]
  public async Task<IActionResult> Index()
  {
    return View("SysAdmin/Index", new PageModel
    {
      Stats = new StatsModel
      {
        DatabaseStats = await App.SysAdmin.GetDatabaseStats(),
        FileStats = await App.SysAdmin.GetFileStats(),
      },
      Organizations = App.SysAdmin.AllOrganizations(),
      RecentBranches = await App.SysAdmin.RecentBranches(RecentBranchesSince),
      RecentBranchesSince = RecentBranchesSince,
      ExpiredBranches = await App.SysAdmin.ExpiredBranches(ExpiredBranchesSince),
      ExpiredBranchesSince = ExpiredBranchesSince,
    });
  }

  //-----------------------------------------------------------------------------------------------

  [HttpPost("refresh-file-stats")]
  public async Task<IActionResult> RefreshFileStats()
  {
    return PartialView("SysAdmin/_Stats", new StatsModel
    {
      DatabaseStats = await App.SysAdmin.RefreshDatabaseStats(), // while we're here - since it's quick
      FileStats = await App.SysAdmin.RefreshFileStats(),
    });
  }

  //-----------------------------------------------------------------------------------------------

  [HttpPost("send-example-email")]
  public async Task<IActionResult> SendExampleEmail()
  {
    await App.Mailer.Deliver("example", Current.Principal.Email, new
    {
      message = "Hello World"
    });
    return Content("<h4 class='text-success-500'>an email has been sent</h4>");
  }

  private static string[] labels = [
    "AAA",
    "CRASH ONCE",
    "CCC",
    "CRASH TWICE",
    "DDD",
    "CRASH ALWAYS",
    "EEE",
    "FFF",
  ];

  [HttpPost("enqueue-example-job")]
  public IActionResult EnqueueExample()
  {
    var index = HttpContext.Session.GetInt32("nextLabel") ?? 0;
    if (index >= labels.Length)
      index = 0;
    var label = labels[index];
    ExampleMinion.Enqueue(Minions, label);
    HttpContext.Session.SetInt32("nextLabel", index + 1);
    return Ok();
  }

  //-----------------------------------------------------------------------------------------------

  [HttpDelete("expired-branch/{branchId}")]
  public async Task<IActionResult> DeleteExpiredBranch(long branchId)
  {
    var expired = await App.SysAdmin.ExpiredBranches(ExpiredBranchesSince);
    var branch = expired.Find(b => b.Id == branchId);
    if (branch is null)
      return NotFound();
    App.Share.DeleteBranch(branch);
    return Response.Htmx().Refresh();
  }

  //===============================================================================================
  // CACHE KICKER PAGE
  //===============================================================================================

  [HttpGet("cache")]
  public async Task<IActionResult> Cache()
  {
    return View("SysAdmin/Cache", await GetCacheKeys());
  }

  [HttpDelete("cache/{key}")]
  public async Task<IActionResult> CacheDelete(string key)
  {
    await App.Cache.RemoveAsync(key);
    return Response.Htmx().Refresh();
  }

  [HttpDelete("trash")]
  public async Task<IActionResult> TrashDelete(string key)
  {
    if (key is not null)
    {
      if (key.StartsWith("trash:safe:"))
      {
        var path = key.Replace("trash:safe:", "");
        RemoveDirectoryMinion.Enqueue(Minions, path, key);
        return Ok("<tr><td colspan=3 class='px-2 text-green'>rmdir job has been enqueued</td></tr>");
      }
      else if (key.StartsWith("trash:unsafe:"))
      {
        var path = key.Replace("trash:unsafe:", "");
        await App.Cache.RemoveAsync(key);
        return Ok("<tr><td colspan=3 class='px-2 text-red'>we trust you manually (and carefully) did this work.</td></tr>");
      }
    }
    return NotFound();
  }

  //===============================================================================================
  // FILESTORE DIFF PAGE
  //===============================================================================================

  [HttpGet("diff")]
  public async Task<IActionResult> Diff()
  {
    var stats = await App.SysAdmin.GetFileStats();
    if (stats is null)
      return Redirect(Url.SysAdminPage());
    return View("SysAdmin/Diff", await App.SysAdmin.GetFileStats());
  }

  //-----------------------------------------------------------------------------------------------

  [HttpPost("diff/download")]
  public async Task<IActionResult> DownloadMissingFile(string file)
  {
    await App.FileStore.Download(file);
    await App.SysAdmin.InvalidateFileStats();
    return Ok();
  }

  //-----------------------------------------------------------------------------------------------

  [HttpPost("diff/upload")]
  public async Task<IActionResult> UploadMissingFile(string file)
  {
    await App.FileStore.Upload(file);
    await App.SysAdmin.InvalidateFileStats();
    return Ok();
  }

  //===============================================================================================
  // MISC ACTIONS
  //===============================================================================================

  [HttpPost("crash")]
  public IActionResult Crash()
  {
    throw new Exception("uh oh"); // used in ProgramTest.cs
  }

  //===============================================================================================
  // PRIVATE IMPLEMENTATION
  //===============================================================================================

  private async Task<CacheKeysModel> GetCacheKeys()
  {
    var sysAdminStats = (await App.Cache.ListKeysAsync("sysadmin:stats")).Order().ToList();
    var downloadReleases = (await App.Cache.ListKeysAsync("download:releases")).Order().ToList();
    var firewallBlocked = (await App.Cache.ListKeysAsync("firewall:blocked")).Order().ToList();
    var gameServeKeys = (await App.Cache.ListKeysAsync("game:serve")).Order().ToList();
    var gameServe = await Task.WhenAll(gameServeKeys.Select(async key =>
    {
      var value = await App.Cache.TryGetAsync<Share.CachedDeployInfo>(key);
      return (key, value.HasValue ? value.Value.FilePath : "unknown");
    }));

    var safeTrashKeys = (await App.Cache.ListKeysAsync("trash:safe")).OrderDescending().ToList();
    var safeTrash = await Task.WhenAll(safeTrashKeys.Select(async key =>
    {
      var value = await App.Cache.TryGetAsync<string>(key);
      return (key, value.HasValue ? value.Value : "unknown");
    }));

    var unsafeTrashKeys = (await App.Cache.ListKeysAsync("trash:unsafe")).OrderDescending().ToList();
    var unsafeTrash = await Task.WhenAll(unsafeTrashKeys.Select(async key =>
    {
      var value = await App.Cache.TryGetAsync<string>(key);
      return (key, value.HasValue ? value.Value : "unknown");
    }));

    return new CacheKeysModel
    {
      SysAdminStats = sysAdminStats,
      DownloadReleases = downloadReleases,
      GameServe = gameServe.ToList(),
      FirewallBlocked = firewallBlocked,
      SafeTrash = safeTrash.ToList(),
      UnsafeTrash = unsafeTrash.ToList(),
    };
  }

  //-----------------------------------------------------------------------------------------------
}