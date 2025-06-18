namespace Void.Platform.Lib;

public static class International
{
  public const string DefaultTimeZone = "America/Los_Angeles";
  public const string DefaultLocale = "en-US";

  public static DateTimeZone TimeZone(string zone)
  {
    var tz = DateTimeZoneProviders.Tzdb[zone];
    if (tz is null)
      throw new ArgumentNullException($"missing timezone {zone}");
    return tz;
  }

  public static string[] TimeZoneIds
  {
    get
    {
      return timezones;
    }
  }

  public static string[] Locales
  {
    get
    {
      return locals;
    }
  }

  private static string[] timezones = DateTimeZoneProviders.Tzdb.Ids.ToArray();
  private static string[] locals = ["en-GB", "en-US"];
}