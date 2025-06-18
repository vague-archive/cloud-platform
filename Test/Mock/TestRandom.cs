namespace Void.Platform.Test;

public class TestRandom : Lib.Random, IRandom
{
  private int identifier = 1;
  public new string Identifier()
  {
    return $"random-identifier-{identifier++}";
  }
}