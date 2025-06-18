namespace Void.Platform.Domain;

public class MoveToTrashMinion : IMinion<MoveToTrashMinion.Data>
{
  //-----------------------------------------------------------------------------------------------

  public record Data
  {
    public required string Path { get; init; }
    public required string Reason { get; init; }
  }

  //-----------------------------------------------------------------------------------------------

  public static void Enqueue(IMinions minions, string path, string reason)
  {
    minions.Enqueue<MoveToTrashMinion, Data>(new Data
    {
      Path = path,
      Reason = reason,
    }, new MinionOptions
    {
      Identity = $"trash:{path}",
    });
  }

  //-----------------------------------------------------------------------------------------------

  private ICache Cache { get; init; }
  private IMailer Mailer { get; init; }

  public MoveToTrashMinion(ICache cache, IMailer mailer)
  {
    Cache = cache;
    Mailer = mailer;
  }

  //-----------------------------------------------------------------------------------------------

  public async Task Execute(Data data, IMinionContext ctx)
  {
    var path = data.Path;
    var reason = data.Reason;
    var safe = RemoveDirectoryMinion.SafeToDelete(path);
    var cacheKey = safe ? CacheKey.SafeTrash(path) : CacheKey.UnsafeTrash(path);
    await Cache.SetAsync(cacheKey, reason, options: new CacheEntryOptions(Duration.FromDays(365)));
    await Mailer.MechanicalTurk($"{reason} - rmdir {path} ({(safe ? "safe" : "UNSAFE")})");
  }

  //-----------------------------------------------------------------------------------------------
}