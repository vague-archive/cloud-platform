namespace Void.Platform.Domain;

public class RemoveDirectoryMinion : IMinion<RemoveDirectoryMinion.Data>
{
  //-----------------------------------------------------------------------------------------------

  public record Data
  {
    public required string Path { get; init; }
    public required string Key { get; init; }
  }

  //-----------------------------------------------------------------------------------------------

  public static void Enqueue(IMinions minions, string path, string key)
  {
    minions.Enqueue<RemoveDirectoryMinion, Data>(new Data
    {
      Path = path,
      Key = key,
    }, new MinionOptions
    {
      Identity = $"rmdir:{path}",
      RetryLimit = 10, // retry up to 10 times (EFS has eventual consistency so "rm -rf" can fail)
      RetryDelay = 30, // seconds (doubles each retry)
    });
  }

  //-----------------------------------------------------------------------------------------------

  private IFileStore FileStore { get; init; }
  private ICache Cache { get; init; }

  //-----------------------------------------------------------------------------------------------

  public RemoveDirectoryMinion(IFileStore fileStore, ICache cache)
  {
    FileStore = fileStore;
    Cache = cache;
  }

  //-----------------------------------------------------------------------------------------------

  public async Task Execute(Data data, IMinionContext ctx)
  {
    var path = data.Path;
    var key = data.Key;
    RuntimeAssert.True(SafeToDelete(path), $"{path} is not safe to delete");
    await FileStore.RemoveDirectory(path);
    await Cache.RemoveAsync(key);
  }

  //-----------------------------------------------------------------------------------------------

  public static bool SafeToDelete(string path)
  {
    return path.Count(c => c == '/') == 4;
  }

  //-----------------------------------------------------------------------------------------------
}