namespace Void.Platform.Domain;

public class RemoveDirectoryMinionTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestEnqueue()
  {
    var minions = BuildTestMinions();

    RemoveDirectoryMinion.Enqueue(minions, "path/to/deploy", "cache-key");

    var minion = Assert.Domain.Enqueued(minions);
    var data = Assert.IsType<RemoveDirectoryMinion.Data>(minion.Data);
    var options = Assert.Present(minion.Options);

    Assert.Equal(typeof(RemoveDirectoryMinion), minion.JobType);
    Assert.Equal(typeof(RemoveDirectoryMinion.Data), minion.DataType);
    Assert.Equal("path/to/deploy", data.Path);
    Assert.Equal("cache-key", data.Key);
    Assert.Equal("rmdir:path/to/deploy", options.Identity);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestExecuteOnSafePath()
  {
    var path = "share/org/game/label/1";
    var key = "cache-key";
    var store = BuildTestFileStore();
    var cache = BuildTestCache();
    var ctx = Substitute.For<IMinionContext>();
    var minion = new RemoveDirectoryMinion(store, cache);
    var data = new RemoveDirectoryMinion.Data
    {
      Path = path,
      Key = key,
    };

    await cache.SetAsync(key, "cache-value", token: CancelToken);
    await store.SaveTestFile($"{path}/index.html", "file-content");

    Assert.Domain.CachePresent(cache, key);      // preconditions
    Assert.Domain.DirectoryPresent(store, path); // preconditions

    await minion.Execute(data, ctx);

    Assert.Domain.CacheAbsent(cache, key);
    Assert.Domain.DirectoryAbsent(store, path);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestExecuteOnUnSafePath()
  {
    var path = "share/org/game/label";
    var key = "cache-key";
    var store = BuildTestFileStore();
    var cache = BuildTestCache();
    var ctx = Substitute.For<IMinionContext>();
    var minion = new RemoveDirectoryMinion(store, cache);
    var data = new RemoveDirectoryMinion.Data
    {
      Path = path,
      Key = key,
    };

    await cache.SetAsync(key, "cache-value", token: CancelToken);
    await store.SaveTestFile($"{path}/index.html", "file-content");

    Assert.Domain.CachePresent(cache, key);      // preconditions
    Assert.Domain.DirectoryPresent(store, path); // preconditions

    var error = await Assert.ThrowsAsync<RuntimeAssertion>(() => minion.Execute(data, ctx));
    Assert.Equal("share/org/game/label is not safe to delete", error.Message);

    Assert.Domain.CachePresent(cache, key);      // postconditions
    Assert.Domain.DirectoryPresent(store, path); // postconditions
  }

  //-----------------------------------------------------------------------------------------------
}