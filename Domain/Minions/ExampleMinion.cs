namespace Void.Platform.Domain;

public class ExampleMinion : IMinion<ExampleMinion.Data>
{
  //-----------------------------------------------------------------------------------------------

  public record Data
  {
    public required string Label { get; init; }
    public int Repeat { get; init; } = 5;
    public int Delay { get; init; } = 1000;
  }

  //-----------------------------------------------------------------------------------------------

  public static void Enqueue(IMinions minions, string label, int repeat = 5, int delay = 1000)
  {
    minions.Enqueue<ExampleMinion, Data>(new Data
    {
      Label = label,
      Repeat = repeat,
      Delay = delay,
    }, new MinionOptions
    {
      Identity = $"example:{label}",
      RetryLimit = 3,
      RetryDelay = 2, // seconds
    });
  }

  //-----------------------------------------------------------------------------------------------

  private ILogger Logger { get; init; }

  public ExampleMinion(ILogger logger)
  {
    Logger = logger;
  }

  //-----------------------------------------------------------------------------------------------

  public async Task Execute(Data data, IMinionContext ctx)
  {
    var count = data.Repeat;
    do
    {
      if (data.Label == "CRASH ONCE" && count == 2 && ctx.RetryCount < 1)
      {
        throw new Exception("this minion has crashed (but will not crash again)");
      }
      else if (data.Label == "CRASH TWICE" && count == 2 && ctx.RetryCount < 2)
      {
        throw new Exception("this minion has crashed (and will crash twice)");
      }
      else if (data.Label == "CRASH ALWAYS" && count == 2)
      {
        throw new Exception("this minion has crashed (and will always crash)");
      }
      History.Add($"{data.Label}: {count}");
      Logger.Information("[EXAMPLE] count {label} = {count}", data.Label, count);
      await Task.Delay(data.Delay);
    } while (--count > 0);
    await Task.CompletedTask;
  }

  public readonly List<string> History = new List<string>();

  //-----------------------------------------------------------------------------------------------
}