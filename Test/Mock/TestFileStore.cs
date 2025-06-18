namespace Void.Platform.Test;

using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

public class TestFileStore : FileStore, IDisposable
{
  //-----------------------------------------------------------------------------------------------

  private TempDirectory Dir { get; init; }

  private TestFileStore(TempDirectory dir, IClock clock, IRandom random, ILogger logger) : base(dir.Path, clock, random, logger)
  {
    Dir = dir;
  }

  public static TestFileStore New(IClock clock, IRandom random, ILogger logger)
  {
    return new TestFileStore(new TempDirectory(), clock, random, logger);
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
      Dir.Dispose();
    }

    disposed = true;
  }

  //-----------------------------------------------------------------------------------------------

  public List<string> ListFileNames()
  {
    return Directory
      .EnumerateFiles(Root, "*", SearchOption.AllDirectories)
      .Select(p => p.Replace($"{Root}/", ""))
      .Order()
      .ToList();
  }

  public bool ContainsFile(string path)
  {
    return File.Exists(Path.Combine(Root, path));
  }

  public bool ContainsDirectory(string path)
  {
    return Directory.Exists(Path.Combine(Root, path));
  }

  public async Task<string?> Read(string file)
  {
    var stream = await Load(file);
    if (stream is null)
      return null;
    return await stream.ReadAsString();
  }

  //-----------------------------------------------------------------------------------------------

  public async Task SaveTestFile(string path, string content)
  {
    await Save(path, content.AsStream());
  }

  //-----------------------------------------------------------------------------------------------

  public (string path, string content)[] DefaultTestArchiveItems = new[]
  {
    ("index.html", "<h1>hello world</h1>"),
  };

  public async Task<Stream> CreateTestArchive(string filename, (string path, string content)[]? items = null)
  {
    items ??= DefaultTestArchiveItems;
    filename = Path.Combine(Root, filename);
    await SaveTestArchive(filename, items);
    return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
  }

  private static async Task SaveTestArchive(string filename, (string path, string content)[] items)
  {
    using var tarStream = new MemoryStream();
    using (var tarWriter = new TarWriter(tarStream, leaveOpen: true))
    {
      foreach (var (path, content) in items)
      {
        var data = Encoding.UTF8.GetBytes(content);
        var entry = new PaxTarEntry(TarEntryType.RegularFile, path)
        {
          DataStream = new MemoryStream(data)
        };
        entry.DataStream.SetLength(data.Length);
        await tarWriter.WriteEntryAsync(entry);
      }
    }

    tarStream.Seek(0, SeekOrigin.Begin);

    using var fileStream = File.Create(filename);
    using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
    await tarStream.CopyToAsync(gzipStream);
  }

  //-----------------------------------------------------------------------------------------------
}