namespace Void.Platform.Test;

public class TempDirectory : IDisposable
{
  //-----------------------------------------------------------------------------------------------

  public string Id { get; init; }
  public string Path { get; init; }

  public TempDirectory()
  {
    Id = Guid.NewGuid().ToString();
    Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "void-platform-test", Id);
    Directory.CreateDirectory(Path);
  }

  //-----------------------------------------------------------------------------------------------

  private bool disposed = false;
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  public virtual void Dispose(bool disposing)
  {
    if (disposed)
      return;

    if (disposing)
    {
      Directory.Delete(Path, true);
    }

    disposed = true;
  }

  //-----------------------------------------------------------------------------------------------
}