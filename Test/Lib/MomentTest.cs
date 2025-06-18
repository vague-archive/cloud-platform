namespace Void.Platform.Lib;

public class MomentTest : TestCase
{
  [Fact]
  public void TestMomentFrom()
  {
    Assert.Equal("2024-07-09T00:00:00Z", Moment.From(2024, 7, 9).ToIso8601());
    Assert.Equal("2024-07-09T01:02:03Z", Moment.From(2024, 7, 9, 1, 2, 3).ToIso8601());
    Assert.Equal("2024-07-09T01:02:03.123Z", Moment.From(2024, 7, 9, 1, 2, 3, 123).ToIso8601());
    Assert.Equal("2024-07-09T01:02:03.123Z", Moment.FromEpoch(1720486923.123).ToIso8601());
    Assert.Equal("2024-07-09T01:02:03.123Z", Moment.FromEpoch("1720486923.123").ToIso8601());
  }

  [Fact]
  public void TestInstantToFromIsoString()
  {
    var instant = Instant.FromUtc(2021, 7, 9, 12, 30, 45).PlusNanoseconds(123456789);
    Assert.Equal(instant, Moment.FromIso8601("2021-07-09T12:30:45.123456789Z"));
    Assert.Equal("2021-07-09T12:30:45.123456789Z", instant.ToIso8601());
  }

  [Fact]
  public void TestInstantToRfc9110()
  {
    var instant = Instant.FromUtc(2021, 7, 9, 12, 30, 45).PlusNanoseconds(123456789);
    Assert.Equal("Fri, 09 Jul 2021 12:30:45 GMT", instant.ToRfc9110());
  }

  [Fact]
  public void TestInstantTruncate()
  {
    var instant = Instant.FromUtc(2021, 1, 2, 10, 30, 45).PlusNanoseconds(123456789);
    Assert.Equal("2021-01-02T10:30:45.123456789Z", instant.TruncateToNanoseconds().ToIso8601());
    Assert.Equal("2021-01-02T10:30:45.123456Z", instant.TruncateToMicroseconds().ToIso8601());
    Assert.Equal("2021-01-02T10:30:45.123Z", instant.TruncateToMilliseconds().ToIso8601());
    Assert.Equal("2021-01-02T10:30:45Z", instant.TruncateToSeconds().ToIso8601());
  }
}