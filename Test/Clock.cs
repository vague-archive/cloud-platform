namespace Void.Platform.Test;

public class TestClock : IClock
{
  private Instant now;

  public TestClock(Instant? now = null)
  {
    this.now = now ?? RealNow;
  }

  public Instant Now
  {
    get
    {
      return now;
    }
  }

  public Instant RealNow
  {
    get
    {
      return Clock.System.Now.TruncateToMilliseconds();
    }
  }

  public void Freeze(Instant on)
  {
    now = on;
  }

  public void Freeze(string on)
  {
    now = Moment.FromIso8601(on);
  }

  public void Freeze(int year, int month, int day, string timeZone = International.DefaultTimeZone)
  {
    Freeze(new LocalDateTime(year, month, day, 0, 0, 0), timeZone);
  }

  public void Freeze(ZonedDateTime on)
  {
    Freeze(on.ToInstant());
  }

  public void Freeze(LocalDateTime on)
  {
    Freeze(on.InUtc().ToInstant());
  }

  public void Freeze(LocalDateTime on, string tz)
  {
    Freeze(on.InZoneStrictly(DateTimeZoneProviders.Tzdb[tz]));
  }
}