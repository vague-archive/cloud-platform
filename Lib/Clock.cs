namespace Void.Platform.Lib;

//-------------------------------------------------------------------------------------------------

public interface IClock
{
  public Instant Now { get; }
}

//-------------------------------------------------------------------------------------------------

public class Clock : IClock
{
  private NodaTime.SystemClock noda;

  public Clock(NodaTime.SystemClock noda)
  {
    this.noda = noda;
  }

  public static Clock System = new Clock(NodaTime.SystemClock.Instance);

  public Instant Now
  {
    get
    {
      return noda.GetCurrentInstant();
    }
  }
}

//-------------------------------------------------------------------------------------------------