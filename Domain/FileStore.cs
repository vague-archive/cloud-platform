namespace Void.Platform.Domain;

using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO;

//=================================================================================================
// IFileStore interface
//=================================================================================================

public record FileStoreStat
{
  public required string Name { get; init; }
  public required long Size { get; init; }
  public Instant LastModifiedOn { get; init; }
}

public interface IFileStore
{
  bool HasRemote { get; }
  string Root { get; }
  string Bucket { get; }

  Task<List<FileStoreStat>> List();
  Task<List<FileStoreStat>> ListRemote();

  Task<FileStoreStat?> Stat(string path);
  Task<Stream?> Load(string path);
  Task Save(string path, Stream content);
  Task RemoveDirectory(string path);
  Task Download(string path);
  Task Upload(string path);
}

//=================================================================================================
// FileStore implementation
//=================================================================================================

public class FileStore : IFileStore
{
  //-----------------------------------------------------------------------------------------------

  public FileStore(string path, IClock clock, IRandom random, ILogger logger)
  {
    HasRemote = false;
    Root = Path.GetFullPath(path);
    Clock = clock;
    Random = random;
    Logger = logger;
    Directory.CreateDirectory(Root);
  }

  public FileStore(string path, string bucket, Aws.IS3 s3, IClock clock, IRandom random, ILogger logger) : this(path, clock, random, logger)
  {
    HasRemote = true;
    Bucket = bucket;
    S3 = s3;
  }

  //-----------------------------------------------------------------------------------------------

  public string Root { get; init; }
  public string Bucket { get; init; } = "";
  public bool HasRemote { get; init; }
  protected IClock Clock { get; init; }
  protected IRandom Random { get; init; }
  protected ILogger Logger { get; init; }

  //-----------------------------------------------------------------------------------------------

  private Aws.IS3? _s3;
  public Aws.IS3 S3
  {
    get
    {
      RuntimeAssert.Present(_s3);
      return _s3;
    }
    set
    {
      _s3 = value;
    }
  }

  //===============================================================================================
  // STAT
  //===============================================================================================

  public async Task<FileStoreStat?> Stat(string path)
  {
    var stat = await LocalStat(path);
    if (stat is null && HasRemote)
    {
      await Download(path);
      stat = await LocalStat(path);
    }
    return stat;
  }

  private Task<FileStoreStat?> LocalStat(string path)
  {
    var filePath = Path.Combine(Root, path);
    if (File.Exists(filePath))
    {
      var fileInfo = new FileInfo(filePath);
      var result = new FileStoreStat
      {
        Name = path,
        Size = fileInfo.Length,
        LastModifiedOn = Instant.FromDateTimeUtc(fileInfo.LastWriteTimeUtc)
      };
      return Task.FromResult<FileStoreStat?>(result);
    }
    else
    {
      return Task.FromResult<FileStoreStat?>(null);
    }
  }

  //===============================================================================================
  // LOAD
  //===============================================================================================

  public async Task<Stream?> Load(string path)
  {
    var stream = await LocalLoad(path);
    if (stream is null && HasRemote)
    {
      await Download(path);
      stream = await LocalLoad(path);
    }
    return stream;
  }

  private Task<Stream?> LocalLoad(string path)
  {
    try
    {
      var filePath = Path.Combine(Root, path);
      var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      return Task.FromResult<Stream?>(stream);
    }
    catch (FileNotFoundException)
    {
      return Task.FromResult<Stream?>(null);
    }
    catch (DirectoryNotFoundException)
    {
      return Task.FromResult<Stream?>(null);
    }
  }

  private async Task<Stream?> RemoteLoad(string path)
  {
    var result = await S3.GetObject(Bucket, path);
    return result?.Content;
  }

  //===============================================================================================
  // SAVE
  //===============================================================================================

  public async Task Save(string path, Stream content)
  {
    await LocalSave(path, content);
    if (HasRemote)
    {
      var stream = await LocalLoad(path);
      RuntimeAssert.Present(stream);
      await RemoteSave(path, stream);
    }
  }

  //---------------------------------------------------------------------------------------------

  private async Task LocalSave(string path, Stream content)
  {
    var start = Clock.Now;
    try
    {
      var fullPath = Path.Combine(Root, path);
      EnsureLocalDirectoryExists(fullPath);
      using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read))
      {
        await content.CopyToAsync(fileStream);
      }
      Logger.Information("[FILESTORE] saved {path} in {duration}", path, Format.Duration(Clock.Now - start));
    }
    catch (Exception ex)
    {
      Logger.Error("[FILESTORE] failed to save {path} in {duration} - {message}", path, Format.Duration(Clock.Now - start), ex.Message);
      throw;
    }
  }

  private async Task RemoteSave(string path, Stream content)
  {
    await S3.Upload(Bucket, path, content);
  }

  //===============================================================================================
  // REMOVE DIRECTORY
  //===============================================================================================

  public async Task RemoveDirectory(string path)
  {
    var start = Clock.Now;
    try
    {
      LocalRemoveDirectory(path); // wait for local success before removing remote (s3) copies
      if (HasRemote)
        await RemoteRemoveDirectory(path);
      Logger.Warning("[FILESTORE] removed directory {path} in {duration}", path, Format.Duration(Clock.Now - start));
    }
    catch (Exception ex)
    {
      Logger.Error("[FILESTORE] failed to remove directory {path} in {duration} - {message}", path, Format.Duration(Clock.Now - start), ex.Message);
      throw;
    }
  }

  //---------------------------------------------------------------------------------------------

  private void LocalRemoveDirectory(string path)
  {
    var fullPath = Path.Combine(Root, path);
    EfsFriendlyRemoveDirectory(fullPath);
  }

  private void EfsFriendlyRemoveDirectory(string fullPath)
  {
    var files = Directory.GetFiles(fullPath);
    foreach (var file in files)
    {
      File.Delete(file);
      Logger.Warning("[FILESTORE] removed file {path}", file);
    }

    var directories = Directory.GetDirectories(fullPath);
    foreach (var dir in directories)
    {
      EfsFriendlyRemoveDirectory(dir);
    }

    Directory.Delete(fullPath);
    Logger.Warning("[FILESTORE] removed local directory {path}", fullPath);
  }

  //---------------------------------------------------------------------------------------------

  private async Task RemoteRemoveDirectory(string path)
  {
    await S3.DeleteObjects(Bucket, prefix: path);
  }

  //=============================================================================================
  // LIST
  //=============================================================================================

  public async Task<List<FileStoreStat>> List()
  {
    var find = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = "find",
        ArgumentList = { ".", "-type", "f", "-printf", "%P %s %T@\n" },
        WorkingDirectory = Root,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
      }
    };

    find.Start();

    List<FileStoreStat> files = new List<FileStoreStat>();
    string? line;
    while ((line = await find.StandardOutput.ReadLineAsync()) != null)
    {
      int lastSpace = line.LastIndexOf(' '); // important - file names can contain spaces, so split on whitespace is not good
      RuntimeAssert.True(lastSpace > 0);
      var lastModified = Moment.FromEpoch(line[(lastSpace + 1)..]);
      line = line[..lastSpace];
      lastSpace = line.LastIndexOf(' ');
      RuntimeAssert.True(lastSpace > 0);
      string name = line[..lastSpace];
      string size = line[(lastSpace + 1)..];
      if (long.TryParse(size, out long bytes))
        files.Add(new FileStoreStat
        {
          Name = name,
          Size = bytes,
          LastModifiedOn = lastModified,
        });
    }

    await find.WaitForExitAsync();

    return files;
  }

  public async Task<List<FileStoreStat>> ListRemote()
  {
    if (HasRemote)
    {
      var objects = await S3.ListObjects(Bucket);
      return objects.Select(o => new FileStoreStat
      {
        Name = o.Key,
        Size = o.ContentLength,
        LastModifiedOn = o.LastModified,
      }).ToList();
    }
    return new List<FileStoreStat>();
  }

  //=============================================================================================
  // DOWNLOAD and UPLOAD (for fixing stuff when out of sync)
  //=============================================================================================

  public async Task Download(string path)
  {
    RuntimeAssert.True(HasRemote);
    Logger.Warning("[FILESTORE] cache miss on {path}, fetching from bucket {bucket}", path, Bucket);
    using (var stream = await RemoteLoad(path))
    {
      if (stream is not null)
      {
        await LocalSave(path, stream);
        Logger.Warning("[FILESTORE] cache restored for {path} from bucket {bucket}", path, Bucket);
      }
      else
      {
        Logger.Warning("[FILESTORE] cache missed for {path} from bucket {bucket}", path, Bucket);
      }
    }
  }

  public async Task Upload(string path)
  {
    RuntimeAssert.True(HasRemote);
    using (var stream = await LocalLoad(path))
    {
      RuntimeAssert.Present(stream);
      await RemoteSave(path, stream);
    }
  }

  //=============================================================================================
  // PRIVATE HELPERS
  //=============================================================================================

  private void EnsureLocalDirectoryExists(string path)
  {
    var dir = Path.GetDirectoryName(path);
    RuntimeAssert.Present(dir);
    Directory.CreateDirectory(dir);
  }
}

//=================================================================================================
// FileStore Service Provider
//=================================================================================================

public static class FileStoreExtensions
{
  public static IServiceCollection AddVoidFileStore(this IServiceCollection services, string path, string? bucket = null)
  {
    services.AddSingleton<IFileStore, FileStore>(sp =>
    {
      var logger = sp.GetRequiredService<ILogger>();
      var clock = sp.GetRequiredService<IClock>();
      var random = sp.GetRequiredService<IRandom>();
      if (bucket is null)
      {
        return new FileStore(path, clock, random, logger);
      }
      else
      {
        var s3 = sp.GetRequiredService<Aws.Client>().S3;
        return new FileStore(path, bucket, s3, clock, random, logger);
      }
    });
    return services;
  }
}

//-------------------------------------------------------------------------------------------------