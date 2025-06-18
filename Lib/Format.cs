namespace Void.Platform.Lib;

using Humanizer;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Text.RegularExpressions;

//=================================================================================================
// ENUMS
//=================================================================================================

public enum DateStyle
{
  Short,
  Medium,
  Long,
  DayOfMonth,
}

public enum DurationStyle
{
  Short,
  Long,
}

//=================================================================================================
// STATIC METHODS ARE AVAILABLE VIA A DEFAULT FORMATTER INSTANCE
//=================================================================================================

public static class Format
{
  private static Formatter Default = new Formatter();

  public static string Date(Instant instant, DateStyle style = DateStyle.Medium) => Default.Date(instant, style);
  public static string Time(Instant instant) => Default.Time(instant);
  public static string RecentDateTime(Instant instant, Instant now) => Default.RecentDateTime(instant, now);
  public static string Duration(Duration duration, DurationStyle style = DurationStyle.Long, bool truncate = false) => Default.Duration(duration, style, truncate);
  public static string Duration(long ms, DurationStyle style = DurationStyle.Long, bool truncate = false) => Default.Duration(ms, style, truncate);
  public static string Round(double value, int ndp = 2) => Default.Round(value, ndp);
  public static string Bytes(long value) => Default.Bytes(value);
  public static string Slugify(string value) => Default.Slugify(value);
  public static string Pluralize(string noun) => Default.Pluralize(noun);
  public static string Pluralize<T>(string noun, IEnumerable<T> items, bool showQuantity = true) => Default.Pluralize(noun, items, showQuantity);
  public static string Pluralize(string noun, long quantity, bool showQuantity = true) => Default.Pluralize(noun, quantity, showQuantity);
  public static string Label(GitHub.ReleasePlatform platform) => Default.Label(platform);
  public static string Enum<T>(T value) where T : Enum => Default.Enum(value);

  public static IHtmlContent RecentDateTimeHtml(Instant instant, Instant now) => Default.RecentDateTimeHtml(instant, now);
}

//=================================================================================================
// THE FORMATTER CLASS ITSELF
//=================================================================================================

public class Formatter
{
  //-----------------------------------------------------------------------------------------------

  public CultureInfo Culture { get; init; }
  public DateTimeZone TimeZone { get; init; }
  public string Locale { get; init; }

  public Formatter(
    string timeZone = International.DefaultTimeZone,
    string locale = International.DefaultLocale)
  {
    Culture = new CultureInfo(locale);
    TimeZone = International.TimeZone(timeZone);
    Locale = locale;
  }

  //-----------------------------------------------------------------------------------------------

  public string Date(Instant instant, DateStyle style = DateStyle.Medium)
  {
    var pattern = DatePattern(style);
    return instant
      .InZone(TimeZone)
      .ToDateTimeUnspecified()
      .ToString(pattern, Culture);
  }

  public string Time(Instant instant)
  {
    return instant
      .InZone(TimeZone)
      .ToDateTimeUnspecified()
      .ToString("t", Culture);
  }

  //-----------------------------------------------------------------------------------------------

  public IHtmlContent RecentDateTimeHtml(Instant instant, Instant now)
  {
    var str = RecentDateTime(instant, now);
    return new HtmlString($"""
    <span title="{Date(instant, DateStyle.DayOfMonth)} {Time(instant)}">{str}</span>
    """);
  }

  public string RecentDateTime(Instant instant, Instant now)
  {
    var instantDate = instant.InZone(TimeZone).Date;
    var nowDate = now.InZone(TimeZone).Date;
    if (instantDate == nowDate)
    {
      var since = now - instant;
      if (since.TotalSeconds < 60)
        return "just now";
      else
        return $"{Format.Duration(since, truncate: true)} ago";
    }
    else if (instantDate.PlusDays(1) == nowDate)
    {
      return "yesterday";
    }
    else if (instantDate.PlusDays(2) == nowDate)
    {
      return "2 days ago";
    }
    else
    {
      return $"{Date(instant, DateStyle.DayOfMonth)}, {Time(instant)}";
    }
  }

  //-----------------------------------------------------------------------------------------------

  public string Duration(Duration duration, DurationStyle style = DurationStyle.Long, bool truncate = false)
  {
    return Duration((long) duration.TotalMilliseconds, style, truncate);
  }

  public string Duration(long ms, DurationStyle style = DurationStyle.Long, bool truncate = false)
  {
    const long SECOND = 1000;
    const long TEN_SECONDS = 10 * SECOND;
    const long MINUTE = 60000;
    const long HOUR = 60 * MINUTE;
    const long DAY = 24 * HOUR;

    long days = 0;
    long hours = 0;
    long minutes = 0;
    double seconds = 0.0;

    if (ms < 100)
    {
      // close to zero == zero
    }
    else if (ms < TEN_SECONDS)
    {
      seconds = Math.Floor(ms / 100.0) / 10;
    }
    else if (ms < MINUTE)
    {
      seconds = Math.Floor(ms / 1000.0);
    }
    else if (ms < HOUR)
    {
      minutes = ms / MINUTE;
      seconds = (ms - (minutes * MINUTE)) / SECOND;
    }
    else if (ms < DAY)
    {
      hours = ms / HOUR;
      minutes = (ms - (hours * HOUR)) / MINUTE;
    }
    else
    {
      days = ms / DAY;
    }

    switch (style)
    {
      case DurationStyle.Long:
        if (days > 0)
          return Pluralize("day", days);
        else if (hours > 0 && minutes > 0 && !truncate)
          return $"{Pluralize("hour", hours)}, {Pluralize("minute", minutes)}";
        else if (hours > 0)
          return Pluralize("hour", hours);
        else if (minutes > 0 && seconds > 0 && !truncate)
          return $"{Pluralize("minutes", (int) minutes)}, {Pluralize("seconds", (int) seconds)}";
        else if (minutes > 0)
          return Pluralize("minutes", (int) minutes);
        else if (seconds == 0 || seconds == 1)
          return Pluralize("second", (int) seconds);
        else
          return $"{seconds} seconds";

      case DurationStyle.Short:
        if (days > 0)
          return $"{days}d";
        else if (hours > 0 && minutes > 0 && !truncate)
          return $"{hours}h {minutes}m";
        else if (hours > 0)
          return $"{hours}h";
        else if (minutes > 0 && seconds > 0 && !truncate)
          return $"{minutes}m {seconds}s";
        else if (minutes > 0)
          return $"{minutes}m";
        else if (seconds == 0)
          return "0 seconds";
        else
          return $"{seconds}s";

      default:
        throw new ArgumentOutOfRangeException();
    }

  }

  //-----------------------------------------------------------------------------------------------

  public string Round(double value, int ndp = 2)
  {
    return Math.Round(value, ndp).ToString();
  }

  //-----------------------------------------------------------------------------------------------

  private const double KB = 1000;
  private const double MB = KB * 1000;
  private const double GB = MB * 1000;
  private const double TB = GB * 1000;
  private const double PB = TB * 1000;

  public string Bytes(long value)
  {
    if (value < KB)
    {
      return Pluralize("byte", value);
    }
    else if (value < MB)
    {
      return $"{Round(value / KB, 2)} KB";
    }
    else if (value < GB)
    {
      return $"{Round(value / MB, 2)} MB";
    }
    else if (value < TB)
    {
      return $"{Round(value / GB, 2)} GB";
    }
    else if (value < PB)
    {
      return $"{Round(value / TB, 2)} TB";
    }
    else
    {
      return $"{Round(value / PB, 2)} PB";
    }
  }

  //-----------------------------------------------------------------------------------------------

  public string Slugify(string value) // WARNING: keep in sync with client side version in lib.ts
  {
    value = value.ToLower().Trim();
    value = Regex.Replace(value, "'s", "s");
    value = Regex.Replace(value, "[^A-Za-zÀ-ÖØ-öø-ÿ0-9-]", "-");
    value = Regex.Replace(value, "-+", "-");
    value = Regex.Replace(value, "-$", "");
    return value;
  }

  //-----------------------------------------------------------------------------------------------

  public string Pluralize(string noun)
  {
    return noun.Pluralize();
  }

  public string Pluralize<T>(string noun, IEnumerable<T> items, bool showQuantity = true)
  {
    return Pluralize(noun, items.Count(), showQuantity);
  }

  public string Pluralize(string noun, long quantity, bool showQuantity = true)
  {
    return noun.ToQuantity(quantity, showQuantity ? ShowQuantityAs.Numeric : ShowQuantityAs.None);
  }

  //-----------------------------------------------------------------------------------------------

  public string Label(GitHub.ReleasePlatform platform)
  {
    switch (platform)
    {
      case GitHub.ReleasePlatform.AppleArm:
        return "Mac (M-Series)";
      case GitHub.ReleasePlatform.AppleIntel:
        return "Mac (Intel)";
      case GitHub.ReleasePlatform.Windows:
        return "Windows";
      case GitHub.ReleasePlatform.LinuxArm:
        return "Linux (M-Series)";
      case GitHub.ReleasePlatform.LinuxIntel:
        return "Linux (Intel)";
      case GitHub.ReleasePlatform.Unknown:
        return "Unknown";
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public string Enum<T>(T value) where T : Enum
  {
    return value.ToString().Humanize().ToLower();
  }

  //===============================================================================================
  // PRIVATE IMPLEMENTATION HELPERS
  //===============================================================================================

  private string DatePattern(DateStyle style)
  {
    string pattern;
    switch (style)
    {
      case DateStyle.Short:
        return Culture.DateTimeFormat.ShortDatePattern;
      case DateStyle.Medium:
        pattern = Culture.DateTimeFormat.LongDatePattern;
        pattern = RemovePart(pattern, "dddd");
        pattern = ReplacePart(pattern, "MMMM", "MMM");
        return pattern;
      case DateStyle.Long:
        return Culture.DateTimeFormat.LongDatePattern;
      case DateStyle.DayOfMonth:
        pattern = Culture.DateTimeFormat.LongDatePattern;
        pattern = RemovePart(pattern, "dddd");
        pattern = ReplacePart(pattern, "MMMM", "MMM");
        pattern = RemoveEndPart(pattern, "yyyy");
        return pattern;
      default:
        throw new ArgumentException();
    }
  }

  private string RemovePart(string pattern, string part)
  {
    return Regex.Replace(pattern, $"{part}[,\\s]*", "");
  }

  private string RemoveEndPart(string pattern, string part)
  {
    return Regex.Replace(pattern, $"[,\\s]*{part}", "");
  }

  private string ReplacePart(string pattern, string part, string replace)
  {
    return Regex.Replace(pattern, part, replace);
  }

  //-----------------------------------------------------------------------------------------------
}