namespace Void.Platform.Domain;

using System.Text.RegularExpressions;

public partial class Downloads : SubDomain
{
  //-----------------------------------------------------------------------------------------------

  public Downloads(Application app) : base(app)
  {
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<Result<List<GitHub.Release>>> GetReleases(string repo, bool refresh = false)
  {
    if (App.GitHubDisabled)
      return Result.Fail("github integration is disabled");

    if (!IsSafeRepo(repo))
      return Result.Fail($"{repo} repository is not allowed");

    var cacheKey = CacheKey.ForDownloads(repo);
    if (refresh)
      await Cache.RemoveAsync(cacheKey);

    var releases = await Cache.GetOrSetAsync<List<GitHub.Release>>(cacheKey,
      async (_, _) =>
      {
        var httpClient = HttpClientFactory.CreateClient();
        var github = new GitHub(httpClient, App.Config.GitHubApiToken!);
        var start = Clock.Now;
        Logger.Warning($"[CACHE] {cacheKey} MISSED - downloading from {github.GetReleasesUrl(repo)} (server: {App.Config.ServerName})");
        var releases = await github.GetReleases(repo);
        Logger.Warning($"[CACHE] {cacheKey} SET    - downloaded from {github.GetReleasesUrl(repo)} in {Format.Duration(Clock.Now - start)} (server: {App.Config.ServerName})");
        return releases;
      },
      options: new CacheEntryOptions(Duration.FromDays(1)));

    return Result.Ok(releases);
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<Result<GitHub.ReleaseAssetDownload>> Download(string repo, long assetId)
  {
    var result = await GetReleases(repo);
    if (result.Failed)
      return Result.Fail(result.Error);

    var asset = GitHub.FindAsset(result.Value, assetId);
    if (asset is null)
      return Result.Fail($"{repo} repository could not find asset {assetId}");

    if (!IsDownloadable(asset.Name))
      return Result.Fail($"{asset.Name} is not downloadable");

    var httpClient = HttpClientFactory.CreateClient();
    var github = new GitHub(httpClient, App.Config.GitHubApiToken!);

    return await github.Download(asset);
  }

  //-----------------------------------------------------------------------------------------------

  private static string[] safeRepo = [
    "editor",
  ];

  public static bool IsSafeRepo(string repo)
  {
    return safeRepo.Contains(repo.Trim().ToLower());
  }

  public static bool IsDownloadable(string fileName)
  {
    return false
      || Regex.IsMatch(fileName, "fiasco.*zip", RegexOptions.IgnoreCase)
      || Regex.IsMatch(fileName, "fiasco.*exe", RegexOptions.IgnoreCase);
  }

  //-----------------------------------------------------------------------------------------------
}