namespace Void.Platform.Lib;

public class FormatTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestFormatDateAndTime()
  {
    var dt = Moment.FromIso8601("2024-02-28T02:34:56.123Z");

    var pacific = new Formatter(timeZone: "US/Pacific");
    var eastern = new Formatter(timeZone: "US/Eastern");
    var london = new Formatter(timeZone: "Europe/London", locale: "en-GB");
    var paris = new Formatter(timeZone: "Europe/Paris", locale: "fr-FR");
    var tokyo = new Formatter(timeZone: "Asia/Tokyo", locale: "ja-JP");

    Assert.Equal("Feb 27, 2024", Format.Date(dt));
    Assert.Equal("Feb 27, 2024", pacific.Date(dt));
    Assert.Equal("Feb 27, 2024", eastern.Date(dt));
    Assert.Equal("28 Feb 2024", london.Date(dt));
    Assert.Equal("28 févr. 2024", paris.Date(dt));
    Assert.Equal("2024年2月28日", tokyo.Date(dt));

    Assert.Equal("2/27/2024", Format.Date(dt, style: DateStyle.Short));
    Assert.Equal("2/27/2024", pacific.Date(dt, style: DateStyle.Short));
    Assert.Equal("2/27/2024", eastern.Date(dt, style: DateStyle.Short));
    Assert.Equal("28/02/2024", london.Date(dt, style: DateStyle.Short));
    Assert.Equal("28/02/2024", paris.Date(dt, style: DateStyle.Short));
    Assert.Equal("2024/02/28", tokyo.Date(dt, style: DateStyle.Short));

    Assert.Equal("Tuesday, February 27, 2024", Format.Date(dt, style: DateStyle.Long));
    Assert.Equal("Tuesday, February 27, 2024", pacific.Date(dt, style: DateStyle.Long));
    Assert.Equal("Tuesday, February 27, 2024", eastern.Date(dt, style: DateStyle.Long));
    Assert.Equal("Wednesday 28 February 2024", london.Date(dt, style: DateStyle.Long));
    Assert.Equal("mercredi 28 février 2024", paris.Date(dt, style: DateStyle.Long));
    Assert.Equal("2024年2月28日水曜日", tokyo.Date(dt, style: DateStyle.Long));

    Assert.Equal("Feb 27", Format.Date(dt, style: DateStyle.DayOfMonth));
    Assert.Equal("Feb 27", pacific.Date(dt, style: DateStyle.DayOfMonth));
    Assert.Equal("Feb 27", eastern.Date(dt, style: DateStyle.DayOfMonth));
    Assert.Equal("28 Feb", london.Date(dt, style: DateStyle.DayOfMonth));
    Assert.Equal("28 févr.", paris.Date(dt, style: DateStyle.DayOfMonth));
    Assert.Equal("年2月28日", tokyo.Date(dt, style: DateStyle.DayOfMonth));

    Assert.Equal("6:34 PM", Format.Time(dt)); // WARNING - the whitespace character here is wierd
    Assert.Equal("6:34 PM", pacific.Time(dt)); // WARNING - the whitespace character here is wierd
    Assert.Equal("9:34 PM", eastern.Time(dt)); // WARNING - the whitespace character here is wierd
    Assert.Equal("02:34", london.Time(dt));
    Assert.Equal("03:34", paris.Time(dt));
    Assert.Equal("11:34", tokyo.Time(dt));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestRecentDateTime()
  {
    var timezone = "America/Los_Angeles";
    var local = Moment.Local(2025, 4, 21, 10, 0, 0, timezone: timezone);
    var now = local.ToInstant();
    var fmt = new Formatter(timezone);

    Assert.Equal("10:00 AM", fmt.Time(now)); // it's 10am local time in Los Angeles
    Assert.Equal("Apr 21, 2025", fmt.Date(now));

    Assert.Equal("just now", Format.RecentDateTime(now.Minus(Duration.FromSeconds(1)), now));
    Assert.Equal("just now", Format.RecentDateTime(now.Minus(Duration.FromSeconds(10)), now));
    Assert.Equal("just now", Format.RecentDateTime(now.Minus(Duration.FromSeconds(30)), now));
    Assert.Equal("just now", Format.RecentDateTime(now.Minus(Duration.FromSeconds(59)), now));
    Assert.Equal("1 minute ago", Format.RecentDateTime(now.Minus(Duration.FromSeconds(60)), now));
    Assert.Equal("1 minute ago", Format.RecentDateTime(now.Minus(Duration.FromSeconds(61)), now));
    Assert.Equal("1 minute ago", Format.RecentDateTime(now.Minus(Duration.FromSeconds(62)), now));
    Assert.Equal("1 minute ago", Format.RecentDateTime(now.Minus(Duration.FromMinutes(1)), now));
    Assert.Equal("2 minutes ago", Format.RecentDateTime(now.Minus(Duration.FromMinutes(2)), now));
    Assert.Equal("10 minutes ago", Format.RecentDateTime(now.Minus(Duration.FromMinutes(10)), now));
    Assert.Equal("30 minutes ago", Format.RecentDateTime(now.Minus(Duration.FromMinutes(30)), now));
    Assert.Equal("1 hour ago", Format.RecentDateTime(now.Minus(Duration.FromMinutes(60)), now));
    Assert.Equal("1 hour ago", Format.RecentDateTime(now.Minus(Duration.FromMinutes(61)), now));
    Assert.Equal("1 hour ago", Format.RecentDateTime(now.Minus(Duration.FromMinutes(62)), now));
    Assert.Equal("1 hour ago", Format.RecentDateTime(now.Minus(Duration.FromHours(1)), now));
    Assert.Equal("2 hours ago", Format.RecentDateTime(now.Minus(Duration.FromHours(2)), now));
    Assert.Equal("3 hours ago", Format.RecentDateTime(now.Minus(Duration.FromHours(3)), now));
    Assert.Equal("4 hours ago", Format.RecentDateTime(now.Minus(Duration.FromHours(4)), now));
    Assert.Equal("5 hours ago", Format.RecentDateTime(now.Minus(Duration.FromHours(5)), now));
    Assert.Equal("6 hours ago", Format.RecentDateTime(now.Minus(Duration.FromHours(6)), now));
    Assert.Equal("7 hours ago", Format.RecentDateTime(now.Minus(Duration.FromHours(7)), now));
    Assert.Equal("8 hours ago", Format.RecentDateTime(now.Minus(Duration.FromHours(8)), now));
    Assert.Equal("9 hours ago", Format.RecentDateTime(now.Minus(Duration.FromHours(9)), now));
    Assert.Equal("10 hours ago", Format.RecentDateTime(now.Minus(Duration.FromHours(10)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(11)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(12)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(13)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(14)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(15)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(16)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(17)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(18)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(19)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(20)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(21)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(22)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(23)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(24)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(25)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(26)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(27)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(28)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(29)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(30)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(31)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(32)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(33)), now));
    Assert.Equal("yesterday", Format.RecentDateTime(now.Minus(Duration.FromHours(34)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(35)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(36)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(37)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(38)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(39)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(40)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(41)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(42)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(43)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(44)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(45)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(46)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(47)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(48)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(49)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(50)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(51)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(52)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(53)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(54)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(55)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(56)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(57)), now));
    Assert.Equal("2 days ago", Format.RecentDateTime(now.Minus(Duration.FromHours(58)), now));
    Assert.Equal("Apr 18, 11:00 PM", Format.RecentDateTime(now.Minus(Duration.FromHours(59)), now));
    Assert.Equal("Apr 18, 10:00 PM", Format.RecentDateTime(now.Minus(Duration.FromHours(60)), now));
    Assert.Equal("Apr 18, 9:00 PM", Format.RecentDateTime(now.Minus(Duration.FromHours(61)), now));
    Assert.Equal("Apr 18, 8:00 PM", Format.RecentDateTime(now.Minus(Duration.FromHours(62)), now));
    Assert.Equal("Apr 18, 7:00 PM", Format.RecentDateTime(now.Minus(Duration.FromHours(63)), now));
    Assert.Equal("Apr 18, 6:00 PM", Format.RecentDateTime(now.Minus(Duration.FromHours(64)), now));

    Assert.Equal("""
      <span title="Apr 21 5:00 AM">5 hours ago</span>
      """, Format.RecentDateTimeHtml(now.Minus(Duration.FromHours(5)), now).ToString());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestSlugify()
  {
    Assert.Equal("jake", Format.Slugify("Jake"));
    Assert.Equal("jake-gordon", Format.Slugify("Jake Gordon"));
    Assert.Equal("jake-gordon", Format.Slugify("    Jake     Gordon    "));
    Assert.Equal("jake-gordon", Format.Slugify("jake-gordon"));
    Assert.Equal("jake-gordon", Format.Slugify("jake_gordon"));
    Assert.Equal("jake-gordon", Format.Slugify("jake.gordon"));
    Assert.Equal("jake-gordon", Format.Slugify("jake------gordon"));
    Assert.Equal("jake-simon-gordon", Format.Slugify("jake (simon) gordon"));
    Assert.Equal("jake-gordon", Format.Slugify("jake/gordon"));
    Assert.Equal("jake-gordon", Format.Slugify("jake?gordon"));
    Assert.Equal("jake-gordon", Format.Slugify("jake(gordon)"));
    Assert.Equal("jake-gordon", Format.Slugify("jake !@#$%^&*()<>[]{};:,+=\"'~ gordon"));
    Assert.Equal("jake-123-gordon", Format.Slugify("jake 123 gordon"));
    Assert.Equal("jakes-team", Format.Slugify("Jake's Team"));
    Assert.Equal("joséphine-élodie", Format.Slugify("Joséphine. Élodie"));
    Assert.Equal("", Format.Slugify(""));
    Assert.Equal("", Format.Slugify("    "));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestPlurals()
  {
    Assert.Equal("people", Format.Pluralize("person"));
    Assert.Equal("0 people", Format.Pluralize("person", 0));
    Assert.Equal("1 person", Format.Pluralize("person", 1));
    Assert.Equal("2 people", Format.Pluralize("person", 2));
    Assert.Equal("0 people", Format.Pluralize("person", 0, showQuantity: true));
    Assert.Equal("1 person", Format.Pluralize("person", 1, showQuantity: true));
    Assert.Equal("2 people", Format.Pluralize("person", 2, showQuantity: true));
    Assert.Equal("people", Format.Pluralize("person", 0, showQuantity: false));
    Assert.Equal("person", Format.Pluralize("person", 1, showQuantity: false));
    Assert.Equal("people", Format.Pluralize("person", 2, showQuantity: false));

    Assert.Equal("team members", Format.Pluralize("team member"));

    Assert.Equal("0 things", Format.Pluralize("thing", new int[] { }));
    Assert.Equal("1 thing", Format.Pluralize("thing", new int[] { 1 }));
    Assert.Equal("2 things", Format.Pluralize("thing", new int[] { 1, 2 }));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestFormatDuration()
  {
    const long SECOND = 1000;
    const long MINUTE = 60 * SECOND;
    const long HOUR = 60 * MINUTE;
    const long DAY = 24 * HOUR;

    Assert.Equal("0 seconds", Format.Duration(0));
    Assert.Equal("0 seconds", Format.Duration(1));
    Assert.Equal("0 seconds", Format.Duration(2));
    Assert.Equal("0 seconds", Format.Duration(49));
    Assert.Equal("0 seconds", Format.Duration(50));
    Assert.Equal("0 seconds", Format.Duration(98));
    Assert.Equal("0 seconds", Format.Duration(99));
    Assert.Equal("0.1 seconds", Format.Duration(100));
    Assert.Equal("0.1 seconds", Format.Duration(123));
    Assert.Equal("0.1 seconds", Format.Duration(199));
    Assert.Equal("0.2 seconds", Format.Duration(200));
    Assert.Equal("0.2 seconds", Format.Duration(234));
    Assert.Equal("0.2 seconds", Format.Duration(299));
    Assert.Equal("0.3 seconds", Format.Duration(333));
    Assert.Equal("0.4 seconds", Format.Duration(444));
    Assert.Equal("0.5 seconds", Format.Duration(555));
    Assert.Equal("0.6 seconds", Format.Duration(666));
    Assert.Equal("0.7 seconds", Format.Duration(777));
    Assert.Equal("0.8 seconds", Format.Duration(888));
    Assert.Equal("0.9 seconds", Format.Duration(999));
    Assert.Equal("1 second", Format.Duration(1000));
    Assert.Equal("1 second", Format.Duration(1001));
    Assert.Equal("1 second", Format.Duration(1099));
    Assert.Equal("1.1 seconds", Format.Duration(1100));
    Assert.Equal("9.9 seconds", Format.Duration(9999));
    Assert.Equal("10 seconds", Format.Duration(10000));
    Assert.Equal("11 seconds", Format.Duration(11111));
    Assert.Equal("22 seconds", Format.Duration(22222));
    Assert.Equal("33 seconds", Format.Duration(33333));
    Assert.Equal("44 seconds", Format.Duration(44444));
    Assert.Equal("55 seconds", Format.Duration(55555));
    Assert.Equal("59 seconds", Format.Duration(59999));
    Assert.Equal("1 minute", Format.Duration(60000));
    Assert.Equal("1 minute", Format.Duration(60001));
    Assert.Equal("1 minute", Format.Duration(60999));
    Assert.Equal("1 minute, 1 second", Format.Duration(MINUTE + SECOND));
    Assert.Equal("1 minute, 2 seconds", Format.Duration(MINUTE + 2 * SECOND));
    Assert.Equal("1 minute, 59 seconds", Format.Duration(MINUTE + 59 * SECOND));
    Assert.Equal("2 minutes", Format.Duration(MINUTE * 2));
    Assert.Equal("59 minutes", Format.Duration(HOUR - MINUTE));
    Assert.Equal("1 hour", Format.Duration(HOUR));
    Assert.Equal("1 hour, 1 minute", Format.Duration(HOUR + MINUTE));
    Assert.Equal("1 hour, 10 minutes", Format.Duration(HOUR + 10 * MINUTE));
    Assert.Equal("2 hours", Format.Duration(HOUR * 2));
    Assert.Equal("23 hours, 59 minutes", Format.Duration(DAY - MINUTE));
    Assert.Equal("1 day", Format.Duration(DAY));
    Assert.Equal("1 day", Format.Duration(DAY + MINUTE));
    Assert.Equal("1 day", Format.Duration(DAY + HOUR));
    Assert.Equal("2 days", Format.Duration(DAY * 2));
    Assert.Equal("3 days", Format.Duration(DAY * 3));
    Assert.Equal("4 days", Format.Duration(DAY * 4));

    Assert.Equal("1 minute", Format.Duration(MINUTE + SECOND, truncate: true));
    Assert.Equal("1 minute", Format.Duration(MINUTE + 2 * SECOND, truncate: true));
    Assert.Equal("1 minute", Format.Duration(MINUTE + 59 * SECOND, truncate: true));
    Assert.Equal("1 hour", Format.Duration(HOUR + MINUTE, truncate: true));
    Assert.Equal("1 hour", Format.Duration(HOUR + 10 * MINUTE, truncate: true));
    Assert.Equal("23 hours", Format.Duration(DAY - MINUTE, truncate: true));

    Assert.Equal("0 seconds", Format.Duration(0, DurationStyle.Short));
    Assert.Equal("0 seconds", Format.Duration(1, DurationStyle.Short));
    Assert.Equal("0 seconds", Format.Duration(2, DurationStyle.Short));
    Assert.Equal("0 seconds", Format.Duration(98, DurationStyle.Short));
    Assert.Equal("0 seconds", Format.Duration(99, DurationStyle.Short));
    Assert.Equal("0.1s", Format.Duration(100, DurationStyle.Short));
    Assert.Equal("0.1s", Format.Duration(123, DurationStyle.Short));
    Assert.Equal("0.1s", Format.Duration(199, DurationStyle.Short));
    Assert.Equal("0.2s", Format.Duration(200, DurationStyle.Short));
    Assert.Equal("0.2s", Format.Duration(234, DurationStyle.Short));
    Assert.Equal("0.2s", Format.Duration(299, DurationStyle.Short));
    Assert.Equal("0.3s", Format.Duration(333, DurationStyle.Short));
    Assert.Equal("0.4s", Format.Duration(444, DurationStyle.Short));
    Assert.Equal("0.5s", Format.Duration(555, DurationStyle.Short));
    Assert.Equal("0.6s", Format.Duration(666, DurationStyle.Short));
    Assert.Equal("0.7s", Format.Duration(777, DurationStyle.Short));
    Assert.Equal("0.8s", Format.Duration(888, DurationStyle.Short));
    Assert.Equal("0.9s", Format.Duration(999, DurationStyle.Short));
    Assert.Equal("1s", Format.Duration(1000, DurationStyle.Short));
    Assert.Equal("1s", Format.Duration(1001, DurationStyle.Short));
    Assert.Equal("1s", Format.Duration(1099, DurationStyle.Short));
    Assert.Equal("1.1s", Format.Duration(1100, DurationStyle.Short));
    Assert.Equal("9.9s", Format.Duration(9999, DurationStyle.Short));
    Assert.Equal("10s", Format.Duration(10000, DurationStyle.Short));
    Assert.Equal("11s", Format.Duration(11111, DurationStyle.Short));
    Assert.Equal("22s", Format.Duration(22222, DurationStyle.Short));
    Assert.Equal("33s", Format.Duration(33333, DurationStyle.Short));
    Assert.Equal("44s", Format.Duration(44444, DurationStyle.Short));
    Assert.Equal("55s", Format.Duration(55555, DurationStyle.Short));
    Assert.Equal("59s", Format.Duration(59999, DurationStyle.Short));
    Assert.Equal("1m", Format.Duration(60000, DurationStyle.Short));
    Assert.Equal("1m", Format.Duration(60001, DurationStyle.Short));
    Assert.Equal("1m", Format.Duration(60999, DurationStyle.Short));
    Assert.Equal("1m 1s", Format.Duration(MINUTE + SECOND, DurationStyle.Short));
    Assert.Equal("1m 2s", Format.Duration(MINUTE + 2 * SECOND, DurationStyle.Short));
    Assert.Equal("1m 59s", Format.Duration(MINUTE + 59 * SECOND, DurationStyle.Short));
    Assert.Equal("2m", Format.Duration(MINUTE * 2, DurationStyle.Short));
    Assert.Equal("59m", Format.Duration(HOUR - MINUTE, DurationStyle.Short));
    Assert.Equal("1h", Format.Duration(HOUR, DurationStyle.Short));
    Assert.Equal("1h 1m", Format.Duration(HOUR + MINUTE, DurationStyle.Short));
    Assert.Equal("1h 10m", Format.Duration(HOUR + 10 * MINUTE, DurationStyle.Short));
    Assert.Equal("2h", Format.Duration(HOUR * 2, DurationStyle.Short));
    Assert.Equal("23h 59m", Format.Duration(DAY - MINUTE, DurationStyle.Short));
    Assert.Equal("1d", Format.Duration(DAY, DurationStyle.Short));
    Assert.Equal("1d", Format.Duration(DAY + MINUTE, DurationStyle.Short));
    Assert.Equal("1d", Format.Duration(DAY + HOUR, DurationStyle.Short));
    Assert.Equal("2d", Format.Duration(DAY * 2, DurationStyle.Short));
    Assert.Equal("3d", Format.Duration(DAY * 3, DurationStyle.Short));
    Assert.Equal("4d", Format.Duration(DAY * 4, DurationStyle.Short));

    Assert.Equal("1m", Format.Duration(MINUTE + SECOND, DurationStyle.Short, truncate: true));
    Assert.Equal("1m", Format.Duration(MINUTE + 2 * SECOND, DurationStyle.Short, truncate: true));
    Assert.Equal("1m", Format.Duration(MINUTE + 59 * SECOND, DurationStyle.Short, truncate: true));
    Assert.Equal("1h", Format.Duration(HOUR + MINUTE, DurationStyle.Short, truncate: true));
    Assert.Equal("1h", Format.Duration(HOUR + 10 * MINUTE, DurationStyle.Short, truncate: true));
    Assert.Equal("23h", Format.Duration(DAY - MINUTE, DurationStyle.Short, truncate: true));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestByteSize()
  {
    Assert.Equal("0 bytes", Format.Bytes(0));
    Assert.Equal("1 byte", Format.Bytes(1));
    Assert.Equal("2 bytes", Format.Bytes(2));
    Assert.Equal("8 bytes", Format.Bytes(8));
    Assert.Equal("999 bytes", Format.Bytes(999));
    Assert.Equal("1 KB", Format.Bytes(1000));
    Assert.Equal("1 KB", Format.Bytes(1001));
    Assert.Equal("1.01 KB", Format.Bytes(1010));
    Assert.Equal("1.1 KB", Format.Bytes(1100));
    Assert.Equal("1.11 KB", Format.Bytes(1111));
    Assert.Equal("1.12 KB", Format.Bytes(1119));
    Assert.Equal("2 KB", Format.Bytes(2000));
    Assert.Equal("3 KB", Format.Bytes(3000));
    Assert.Equal("4 KB", Format.Bytes(4000));
    Assert.Equal("5 KB", Format.Bytes(5000));
    Assert.Equal("6 KB", Format.Bytes(6000));
    Assert.Equal("7 KB", Format.Bytes(7000));
    Assert.Equal("8 KB", Format.Bytes(8000));
    Assert.Equal("9 KB", Format.Bytes(9000));
    Assert.Equal("10 KB", Format.Bytes(10000));
    Assert.Equal("100 KB", Format.Bytes(100000));
    Assert.Equal("1 MB", Format.Bytes(1000000));
    Assert.Equal("10 MB", Format.Bytes(10000000));
    Assert.Equal("100 MB", Format.Bytes(100000000));
    Assert.Equal("1 GB", Format.Bytes(1000000000));
    Assert.Equal("10 GB", Format.Bytes(10000000000));
    Assert.Equal("100 GB", Format.Bytes(100000000000));
    Assert.Equal("1 TB", Format.Bytes(1000000000000));
    Assert.Equal("10 TB", Format.Bytes(10000000000000));
    Assert.Equal("100 TB", Format.Bytes(100000000000000));
    Assert.Equal("1 PB", Format.Bytes(1000000000000000));
    Assert.Equal("1.23 PB", Format.Bytes(1234567890000000));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestRound()
  {
    Assert.Equal("1234", Format.Round(1234, 0));
    Assert.Equal("1234", Format.Round(1234, 1));
    Assert.Equal("1234", Format.Round(1234, 2));

    Assert.Equal("42", Format.Round(42.234567, 0));
    Assert.Equal("42.2", Format.Round(42.234567, 1));
    Assert.Equal("42.23", Format.Round(42.234567, 2));
    Assert.Equal("42.235", Format.Round(42.234567, 3));
    Assert.Equal("42.2346", Format.Round(42.234567, 4));

    Assert.Equal("0", Format.Round(0.234567, 0));
    Assert.Equal("0.2", Format.Round(0.234567, 1));
    Assert.Equal("0.23", Format.Round(0.234567, 2));
    Assert.Equal("0.235", Format.Round(0.234567, 3));
    Assert.Equal("0.2346", Format.Round(0.234567, 4));

    Assert.Equal("1", Format.Round(1.00001, 2));
    Assert.Equal("1.1", Format.Round(1.10001, 2));

    Assert.Equal("1000", Format.Round(1000, 0));
    Assert.Equal("1000", Format.Round(1000, 1));
    Assert.Equal("1000", Format.Round(1000, 2));

    Assert.Equal("1000", Format.Round(1000.001, 2));
    Assert.Equal("1000", Format.Round(1000.0001, 2));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestFormatGitHubReleasePlatform()
  {
    Assert.Equal("Mac (M-Series)", Format.Label(GitHub.ReleasePlatform.AppleArm));
    Assert.Equal("Mac (Intel)", Format.Label(GitHub.ReleasePlatform.AppleIntel));
    Assert.Equal("Windows", Format.Label(GitHub.ReleasePlatform.Windows));
    Assert.Equal("Linux (M-Series)", Format.Label(GitHub.ReleasePlatform.LinuxArm));
    Assert.Equal("Linux (Intel)", Format.Label(GitHub.ReleasePlatform.LinuxIntel));
    Assert.Equal("Unknown", Format.Label(GitHub.ReleasePlatform.Unknown));
  }

  //-----------------------------------------------------------------------------------------------

  enum ExampleEnum
  {
    Value,
    OtherValue,
  }

  [Fact]
  public void TestFormatEnum()
  {
    Assert.Equal("value", Format.Enum(ExampleEnum.Value));
    Assert.Equal("other value", Format.Enum(ExampleEnum.OtherValue));
  }

  //-----------------------------------------------------------------------------------------------
}