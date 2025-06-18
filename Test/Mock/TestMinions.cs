namespace Void.Platform.Test;

public class TestMinions : IMinions
{
  public record Entry(Type JobType, Type DataType, object Data, MinionOptions? Options);

  private readonly Queue<Entry> queue = new();

  public void Enqueue<M, D>(D data, MinionOptions? opts = null) where M : IMinion<D> where D : class
  {
    queue.Enqueue(new Entry(typeof(M), typeof(D), data, opts));
  }

  public Entry? Next()
  {
    if (queue.TryDequeue(out var entry))
      return entry;
    else
      return null;
  }

  //-----------------------------------------------------------------------------------------------

  public Entry AssertEnqueued()
  {
    return Assert.Present(Next());
  }

  public D AssertEnqueued<M, D>() where M : IMinion<D> where D : class
  {
    var entry = Assert.Present(Next());
    Assert.Equal(typeof(M), entry.JobType);
    Assert.Equal(typeof(D), entry.DataType);
    Assert.IsType<D>(entry.Data);
    return (D) entry.Data;
  }

  public void AssertEmpty()
  {
    Assert.Empty(queue);
    Assert.Absent(Next());
  }

  //-----------------------------------------------------------------------------------------------
}