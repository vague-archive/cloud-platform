namespace Void.Platform.Lib;

using System.Collections.Concurrent;

public class ArchiveTest : TestCase
{
  [Fact]
  public async Task TestExtract()
  {
    using (var test = BuildTestFileStore())
    {
      string path1 = "path/to/first.txt";
      string path2 = "path/to/second.txt";
      string path3 = "path/to/third.txt";
      string content1 = "First Example";
      string content2 = "Second Example";
      string content3 = "Third Example";

      using var archive = await test.CreateTestArchive("build.tgz", new (string path, string content)[]
      {
        (path1, content1),
        (path2, content2),
        (path3, content3),
      });

      IDictionary<string, string> files = new ConcurrentDictionary<string, string>();
      IList<string> events = new List<string>();

      await foreach (var entry in Archive.Extract(archive))
      {
        files.Add(entry.Name, await entry.Content.ReadAsString(entry.Length));
      }

      Assert.Equal([
        path1,
        path2,
        path3,
      ], files.Keys.Order().ToArray());

      Assert.Equal(content1, files[path1]);
      Assert.Equal(content2, files[path2]);
      Assert.Equal(content3, files[path3]);
    }
  }
}