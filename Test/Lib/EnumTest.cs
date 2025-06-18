namespace Void.Platform.Lib;

public class EnumTest : TestCase
{
  enum Mood
  {
    Happy,
    Sad,
    Bored,
    PlayingBalatro,
  }

  [Fact]
  public void TestStringToEnum()
  {
    Assert.Equal(Mood.Happy, "Happy".ToEnum<Mood>());
    Assert.Equal(Mood.Happy, "happy".ToEnum<Mood>());
    Assert.Equal(Mood.Happy, "HAPPY".ToEnum<Mood>());

    Assert.Equal(Mood.Happy, "Happy".ToEnum<Mood>(true));
    Assert.Equal(Mood.Happy, "happy".ToEnum<Mood>(true));
    Assert.Equal(Mood.Happy, "HAPPY".ToEnum<Mood>(true));
    Assert.Equal(Mood.Happy, "Happy".ToEnum<Mood>(false));
    Assert.Throws<ArgumentException>(() => "happy".ToEnum<Mood>(false));
    Assert.Throws<ArgumentException>(() => "HAPPY".ToEnum<Mood>(false));

    Assert.Equal(Mood.PlayingBalatro, "PlayingBalatro".ToEnum<Mood>());
    Assert.Equal(Mood.PlayingBalatro, "playingbalatro".ToEnum<Mood>());
    Assert.Equal(Mood.PlayingBalatro, "PLAYINGBALATRO".ToEnum<Mood>());
    Assert.Equal(Mood.PlayingBalatro, "PlayingBalatro".ToEnum<Mood>(true));
    Assert.Equal(Mood.PlayingBalatro, "playingbalatro".ToEnum<Mood>(true));
    Assert.Equal(Mood.PlayingBalatro, "PLAYINGBALATRO".ToEnum<Mood>(true));
    Assert.Equal(Mood.PlayingBalatro, "PlayingBalatro".ToEnum<Mood>(false));
    Assert.Throws<ArgumentException>(() => "playingbalatro".ToEnum<Mood>(false));
    Assert.Throws<ArgumentException>(() => "PLAYINGBALATRO".ToEnum<Mood>(false));

    Assert.Throws<ArgumentException>(() => "unknown".ToEnum<Mood>());
    Assert.Throws<ArgumentException>(() => "unknown".ToEnum<Mood>(true));
    Assert.Throws<ArgumentException>(() => "unknown".ToEnum<Mood>(false));

    IEnumerable<string> values = ["happy", "SAD", "PlayingBalatro"];

    Assert.Equal([
      Mood.Happy,
      Mood.Sad,
      Mood.PlayingBalatro,
    ], values.ToEnumList<Mood>());
  }
}