namespace Void.Platform.Domain;

public class MoveToTrashMinionTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestEnqueue()
  {
    var minions = BuildTestMinions();

    MoveToTrashMinion.Enqueue(minions, "path/to/deploy/1", reason: "felt like it");

    var minion = Assert.Domain.Enqueued(minions);
    var data = Assert.IsType<MoveToTrashMinion.Data>(minion.Data);
    var options = Assert.Present(minion.Options);

    Assert.Equal(typeof(MoveToTrashMinion), minion.JobType);
    Assert.Equal(typeof(MoveToTrashMinion.Data), minion.DataType);
    Assert.Equal("path/to/deploy/1", data.Path);
    Assert.Equal("felt like it", data.Reason);
    Assert.Equal("trash:path/to/deploy/1", options.Identity);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestExecuteOnSafePath()
  {
    var path = "share/org/game/label/1";
    var reason = "deploy replaced";
    var cache = BuildTestCache();
    var mailer = BuildTestMailer();
    var ctx = Substitute.For<IMinionContext>();
    var minion = new MoveToTrashMinion(cache, mailer);
    var data = new MoveToTrashMinion.Data
    {
      Path = path,
      Reason = reason,
    };

    await minion.Execute(data, ctx);

    var cached = Assert.Domain.CachePresent<string>(cache, CacheKey.SafeTrash(path));
    Assert.Equal(reason, cached);

    var sent = Assert.Mailed(mailer, "mechanical-turk", "jake@void.dev");
    Assert.Equal("deploy replaced - rmdir share/org/game/label/1 (safe)", sent.Data["message"]);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestExecuteOnUnSafePath()
  {
    var path = "share/org/game/label";
    var reason = "deploy replaced";
    var cache = BuildTestCache();
    var mailer = BuildTestMailer();
    var ctx = Substitute.For<IMinionContext>();
    var minion = new MoveToTrashMinion(cache, mailer);
    var data = new MoveToTrashMinion.Data
    {
      Path = path,
      Reason = reason,
    };

    await minion.Execute(data, ctx);

    var cached = Assert.Domain.CachePresent<string>(cache, CacheKey.UnsafeTrash(path));
    Assert.Equal(reason, cached);

    var sent = Assert.Mailed(mailer, "mechanical-turk", "jake@void.dev");
    Assert.Equal("deploy replaced - rmdir share/org/game/label (UNSAFE)", sent.Data["message"]);
  }

  //-----------------------------------------------------------------------------------------------
}