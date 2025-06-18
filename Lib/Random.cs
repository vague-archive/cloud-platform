namespace Void.Platform.Lib;

public interface IRandom
{
  string Identifier();
  int Integer(int min = 0, int max = 10);
}

public class Random : IRandom
{
  public static Random Default = new Random();

  private System.Random rng;

  public Random()
  {
    rng = new System.Random();
  }

  public Random(int seed)
  {
    rng = new System.Random(seed);
  }

  public string Identifier()
  {
    return Ulid.NewUlid().ToString();
  }

  public int Integer(int min = 0, int max = 10)
  {
    return rng.Next(min, max);
  }
}