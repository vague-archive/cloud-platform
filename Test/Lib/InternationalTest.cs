namespace Void.Platform.Lib;

public class InternationalTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestTimezone()
  {
    var paris = International.TimeZone("Europe/Paris");
    var tokyo = International.TimeZone("Asia/Tokyo");

    Assert.Equal(0, paris.MinOffset.ToTimeSpan().Hours);
    Assert.Equal(2, paris.MaxOffset.ToTimeSpan().Hours);
    Assert.Equal("Europe/Paris", paris.ToString());

    Assert.Equal(9, tokyo.MinOffset.ToTimeSpan().Hours);
    Assert.Equal(10, tokyo.MaxOffset.ToTimeSpan().Hours);
    Assert.Equal("Asia/Tokyo", tokyo.ToString());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestTimezoneIds()
  {
    Assert.Equal(596, International.TimeZoneIds.Length);
    Assert.Equal([
      "US/Alaska",
      "US/Aleutian",
      "US/Arizona",
      "US/Central",
      "US/East-Indiana",
      "US/Eastern",
      "US/Hawaii",
      "US/Indiana-Starke",
      "US/Michigan",
      "US/Mountain",
      "US/Pacific",
      "US/Samoa"
    ], International.TimeZoneIds.Where(tz => tz.StartsWith("US/")));
  }

  [Fact]
  public void TestLocales()
  {
    Assert.Equal([
      "en-GB",
      "en-US",
    ], International.Locales);
  }

  //-----------------------------------------------------------------------------------------------
}