namespace Void.Platform.Domain;

public class ExampleMinionTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestEnqueue()
  {
    var minions = BuildTestMinions();

    ExampleMinion.Enqueue(minions, "test-label", repeat: 7, delay: 42);

    var minion = Assert.Domain.Enqueued(minions);
    var data = Assert.IsType<ExampleMinion.Data>(minion.Data);
    var options = Assert.Present(minion.Options);

    Assert.Equal(typeof(ExampleMinion), minion.JobType);
    Assert.Equal(typeof(ExampleMinion.Data), minion.DataType);
    Assert.Equal("test-label", data.Label);
    Assert.Equal(7, data.Repeat);
    Assert.Equal(42, data.Delay);
    Assert.Equal("example:test-label", options.Identity);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestEnqueueWithDefaults()
  {
    var minions = BuildTestMinions();

    ExampleMinion.Enqueue(minions, "first", repeat: 1);
    ExampleMinion.Enqueue(minions, "second", repeat: 2);
    ExampleMinion.Enqueue(minions, "other", delay: 123);

    var first = Assert.Domain.Enqueued<ExampleMinion, ExampleMinion.Data>(minions);
    var second = Assert.Domain.Enqueued<ExampleMinion, ExampleMinion.Data>(minions);
    var other = Assert.Domain.Enqueued<ExampleMinion, ExampleMinion.Data>(minions);

    Assert.Equal("first", first.Label);
    Assert.Equal("second", second.Label);
    Assert.Equal("other", other.Label);

    Assert.Equal(1, first.Repeat);
    Assert.Equal(2, second.Repeat);
    Assert.Equal(5, other.Repeat);

    Assert.Equal(1000, first.Delay);
    Assert.Equal(1000, second.Delay);
    Assert.Equal(123, other.Delay);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestExecute()
  {
    var ctx = Substitute.For<IMinionContext>();

    var minion = new ExampleMinion(Logger);
    var data = new ExampleMinion.Data
    {
      Label = "Hello World",
      Repeat = 3,
      Delay = 10,
    };

    await minion.Execute(data, ctx);

    Assert.Equal([
      "Hello World: 3",
      "Hello World: 2",
      "Hello World: 1",
    ], minion.History);
  }

  //-----------------------------------------------------------------------------------------------
}