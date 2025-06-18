namespace Void.Platform.Test;

using Microsoft.AspNetCore.Http;

public class TestRequestDelegate
{
  public int Count { get; private set; } = 0;

  public bool HasNotBeenCalled { get { return Count == 0; } }
  public bool HasBeenCalled { get { return Count == 1; } }
  public bool HasBeenCalledTwice { get { return Count == 2; } }
  public bool HasBeenCalledThreeTimes { get { return Count == 3; } }
  public bool HasBeenCalledFourTimes { get { return Count == 4; } }
  public bool HasBeenCalledFiveTimes { get { return Count == 5; } }

  public void Reset()
  {
    Count = 0;
  }

  public RequestDelegate Delegate => async context =>
  {
    Count++;
    await Task.CompletedTask;
  };
}