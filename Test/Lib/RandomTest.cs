namespace Void.Platform.Lib;

public class RandomTest : TestCase
{
  [Fact]
  public void TestIdentifier()
  {
    var random = new Random();
    var id1 = random.Identifier();
    var id2 = random.Identifier();
    var id3 = random.Identifier();

    Assert.NotEqual(id1, id2);
    Assert.NotEqual(id1, id3);
    Assert.NotEqual(id2, id3);

    Assert.Equal(26, id1.Length); // looks like a Ulid
    Assert.Equal(26, id2.Length); // looks like a Ulid
    Assert.Equal(26, id3.Length); // looks like a Ulid
  }

  [Fact]
  public void TestInteger()
  {
    var random = new Random(42); // KNOWN SEED
    Assert.Equal(6, random.Integer());
    Assert.Equal(1, random.Integer());
    Assert.Equal(1, random.Integer());
    Assert.Equal(5, random.Integer());
    Assert.Equal(1, random.Integer());
    Assert.Equal(2, random.Integer());
    Assert.Equal(7, random.Integer());
    Assert.Equal(5, random.Integer());
    Assert.Equal(1, random.Integer());
    Assert.Equal(7, random.Integer());
    Assert.Equal(2, random.Integer());
    Assert.Equal(2, random.Integer());
    Assert.Equal(5, random.Integer());
    Assert.Equal(3, random.Integer());
    Assert.Equal(3, random.Integer());
    Assert.Equal(2, random.Integer());
    Assert.Equal(5, random.Integer());
    Assert.Equal(0, random.Integer());
    Assert.Equal(8, random.Integer());
    Assert.Equal(5, random.Integer());
    Assert.Equal(3, random.Integer());
    Assert.Equal(1, random.Integer());
    Assert.Equal(0, random.Integer());
    Assert.Equal(7, random.Integer());
    Assert.Equal(8, random.Integer());
    Assert.Equal(5, random.Integer());
    Assert.Equal(0, random.Integer());
    Assert.Equal(7, random.Integer());
    Assert.Equal(1, random.Integer());
    Assert.Equal(9, random.Integer());
    Assert.Equal(6, random.Integer());
    Assert.Equal(5, random.Integer());
    Assert.Equal(1, random.Integer());
    Assert.Equal(0, random.Integer());
    Assert.Equal(3, random.Integer());
  }

}