namespace Void.Platform.Lib;

using NodaTime;
using NodaTime.Text;

public enum TimePrecision
{
  Nanoseconds = 9,
  Microseconds = 6,
  Milliseconds = 3,
  Seconds = 0,
}

//=================================================================================================
//
// Our Date and Time helper methods are implemented using the `Moment` class (below) but
// are primarily exposed through extension methods on the underlying NodaTime.Instant
//
//=================================================================================================

public static class InstantExtensions
{
  public static string ToIso8601(this Instant instant)
  {
    return Moment.ToIso8601(instant);
  }

  public static string ToRfc9110(this Instant instant)
  {
    return instant.ToDateTimeUtc().ToString("R");
  }

  public static Instant FromIso8601(this string value)
  {
    return Moment.FromIso8601(value);
  }

  public static Instant Truncate(this Instant instant, TimePrecision precision)
  {
    return Moment.Truncate(instant, precision);
  }

  public static Instant TruncateToNanoseconds(this Instant instant)
  {
    return Moment.Truncate(instant, TimePrecision.Nanoseconds);
  }

  public static Instant TruncateToMicroseconds(this Instant instant)
  {
    return Moment.Truncate(instant, TimePrecision.Microseconds);
  }

  public static Instant TruncateToMilliseconds(this Instant instant)
  {
    return Moment.Truncate(instant, TimePrecision.Milliseconds);
  }

  public static Instant TruncateToSeconds(this Instant instant)
  {
    return Moment.Truncate(instant, TimePrecision.Seconds);
  }

}

//=================================================================================================

public static class Moment
{
  public static Instant From(int year, int month, int day)
  {
    return Instant.FromUtc(year, month, day, 0, 0, 0);
  }

  public static Instant From(int year, int month, int day, int hour, int min, int sec, int milliseconds = 0)
  {
    return Instant.FromUtc(year, month, day, hour, min, sec).PlusNanoseconds(milliseconds * 1000 * 1000);
  }

  public static Instant FromEpoch(string epoch)
  {
    return FromEpoch(double.Parse(epoch));
  }

  public static Instant FromEpoch(double seconds)
  {
    return Instant.FromUnixTimeTicks((long) (seconds * NodaConstants.TicksPerSecond));
  }

  public static string ToIso8601(Instant instant)
  {
    return InstantPattern.ExtendedIso.Format(instant);
  }

  public static bool TryFromIso8601(string value, out Instant instant)
  {
    var result = InstantPattern.ExtendedIso.Parse(value);
    if (result.Success)
    {
      instant = result.Value;
      return true;
    }
    else
    {
      instant = default(Instant);
      return false;
    }
  }

  public static Instant FromIso8601(string value)
  {
    if (TryFromIso8601(value, out var instant))
    {
      return instant;
    }
    else
    {
      throw new Exception($"invalid ISO8601 format {value}");
    }
  }

  public static ZonedDateTime Local(int year, int month, int day, int hour, int min, int sec, string timezone, int milliseconds = 0)
  {
    var local = new LocalDateTime(year, month, day, hour, min, sec, milliseconds);
    var tz = International.TimeZone(timezone);
    return tz.AtStrictly(local);
  }

  public static Instant Truncate(Instant instant, TimePrecision precision)
  {
    if (precision == TimePrecision.Nanoseconds)
      return instant;

    double factor = Math.Pow(10, 7 - (int) precision); // 1 tick = 10^-7 seconds
    long tickFactor = (long) Math.Round(factor);
    long truncatedTicks = (instant.ToUnixTimeTicks() / tickFactor) * tickFactor;
    return Instant.FromUnixTimeTicks(truncatedTicks);
  }
}