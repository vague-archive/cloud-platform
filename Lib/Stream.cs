namespace Void.Platform.Lib;

using System.Text;

public static class StreamExtensions
{
  //-----------------------------------------------------------------------------------------------

  public static Stream AsStream(this string content)
  {
    return new MemoryStream(Encoding.UTF8.GetBytes(content));
  }

  public static async Task<MemoryStream> ReadIntoMemory(this Stream stream, long? capacity = null)
  {
    RuntimeAssert.True(capacity is null || capacity < int.MaxValue);
    var buffer = capacity.HasValue
      ? new MemoryStream(capacity: (int) capacity.Value)
      : new MemoryStream();
    await stream.CopyToAsync(buffer);
    buffer.Position = 0;
    return buffer; // caller must dispose
  }

  public static async Task<string> ReadAsString(this Stream stream, long? capacity = null)
  {
    // deliberately use a memory stream buffer to avoid over-reading (sub)streams that
    // come from a TarArchive, because CopyToAsync knows when to stop, while ReadToEndAsync
    // does not and will consume the entire TAR stream (not just the entry sub stream)
    using var buffer = await stream.ReadIntoMemory(capacity);

    // now we can safely read to the end
    using var reader = new StreamReader(buffer, Encoding.UTF8);
    return await reader.ReadToEndAsync();
  }

  //-----------------------------------------------------------------------------------------------
}