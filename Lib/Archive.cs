namespace Void.Platform.Lib;

using System.Formats.Tar;
using System.IO.Compression;

public static class Archive
{
  public record Entry
  {
    public required string Name { get; init; }
    public required long Length { get; init; }
    public required Stream Content { get; init; }
  }

  public static async IAsyncEnumerable<Entry> Extract(Stream archive)
  {
    using var gzipStream = new GZipStream(archive, CompressionMode.Decompress);
    using var tarReader = new TarReader(gzipStream);
    while (await tarReader.GetNextEntryAsync() is { } entry)
    {
      if (entry.EntryType == TarEntryType.RegularFile && entry.DataStream is not null)
      {
        yield return new Entry
        {
          Name = Path.GetFullPath(entry.Name, "/").TrimStart('/'),
          Length = entry.Length,
          Content = entry.DataStream,
        };

        // just in case consumer did not consume the full entry
        await entry.DataStream.CopyToAsync(Stream.Null);
      }
    }
  }
}