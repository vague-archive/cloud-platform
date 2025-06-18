namespace Void.Platform.Domain;

public class FileStoreTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  public const string Bucket = "custom-bucket";
  public const string Path1 = "path/to/first.txt";
  public const string Path2 = "path/to/second.txt";
  public const string UnknownFile = "path/to/unknown.txt";
  public const string UnknownDirectory = "unknown/directory";
  public const string ExampleContent1 = "First Example";
  public const string ExampleContent2 = "Second Example";
  public const string ETag = "random";

  //===============================================================================================
  // TEST LOCAL ONLY
  //===============================================================================================

  [Fact]
  public async Task TestSaveAndLoadAndStat()
  {
    using (var test = BuildTestFileStore())
    {
      var store = new FileStore(test.Root, Clock, Random, Logger);

      Assert.Null(await store.Stat(Path1));
      Assert.Null(await store.Stat(Path2));

      using (var stream = ExampleContent1.AsStream())
      {
        await store.Save(Path1, stream);
      }

      using (var stream = ExampleContent2.AsStream())
      {
        await store.Save(Path2, stream);
      }

      var stat1 = await store.Stat(Path1);
      var stat2 = await store.Stat(Path2);
      Assert.Present(stat1);
      Assert.Present(stat2);
      Assert.CloseEnough(Clock.RealNow, stat1.LastModifiedOn);
      Assert.CloseEnough(Clock.RealNow, stat2.LastModifiedOn);

      using (var stream = await store.Load(Path1))
      {
        Assert.Present(stream);
        Assert.Equal(ExampleContent1, await stream.ReadAsString());
      }

      using (var stream = await store.Load(Path2))
      {
        Assert.Present(stream);
        Assert.Equal(ExampleContent2, await stream.ReadAsString());
      }

      Assert.Null(await store.Load(UnknownFile));
      Assert.Null(await store.Load(UnknownDirectory));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestList()
  {
    using (var test = BuildTestFileStore())
    {
      await test.SaveTestFile("dir1/file1.txt", "a");
      await test.SaveTestFile("dir1/file2.txt", "bb");
      await test.SaveTestFile("dir2/file3.txt", "ccc");
      await test.SaveTestFile("dir2/file4.txt", "dddd");
      await test.SaveTestFile("dir3/file5.txt", "eeeee");
      await test.SaveTestFile("dir3/file6.txt", "ffffff");

      var store = new FileStore(test.Root, Clock, Random, Logger);

      var list = await store.List();
      Assert.Equal(6, list.Count);

      list = list.OrderBy(e => e.Name).ToList(); // IFileStore.List is NOT sorted (for performance, so sort it for test purposes

      Assert.Equal("dir1/file1.txt", list[0].Name);
      Assert.Equal("dir1/file2.txt", list[1].Name);
      Assert.Equal("dir2/file3.txt", list[2].Name);
      Assert.Equal("dir2/file4.txt", list[3].Name);
      Assert.Equal("dir3/file5.txt", list[4].Name);
      Assert.Equal("dir3/file6.txt", list[5].Name);

      Assert.Equal(1, list[0].Size);
      Assert.Equal(2, list[1].Size);
      Assert.Equal(3, list[2].Size);
      Assert.Equal(4, list[3].Size);
      Assert.Equal(5, list[4].Size);
      Assert.Equal(6, list[5].Size);

      Assert.CloseEnough(Clock.Now, list[0].LastModifiedOn);
      Assert.CloseEnough(Clock.Now, list[1].LastModifiedOn);
      Assert.CloseEnough(Clock.Now, list[2].LastModifiedOn);
      Assert.CloseEnough(Clock.Now, list[3].LastModifiedOn);
      Assert.CloseEnough(Clock.Now, list[4].LastModifiedOn);
      Assert.CloseEnough(Clock.Now, list[5].LastModifiedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestRemoveDirectory()
  {
    using (var test = BuildTestFileStore())
    {
      await test.SaveTestFile("dir1/file1.txt", "first");
      await test.SaveTestFile("dir1/file2.txt", "first");
      await test.SaveTestFile("dir2/file3.txt", "first");
      await test.SaveTestFile("dir2/file4.txt", "first");
      await test.SaveTestFile("dir3/file5.txt", "first");
      await test.SaveTestFile("dir3/file6.txt", "first");

      Assert.Domain.Files([
        "dir1/file1.txt",
        "dir1/file2.txt",
        "dir2/file3.txt",
        "dir2/file4.txt",
        "dir3/file5.txt",
        "dir3/file6.txt",
      ], test);

      var store = new FileStore(test.Root, Clock, Random, Logger);

      await store.RemoveDirectory("dir2");
      Assert.Domain.Files([
        "dir1/file1.txt",
        "dir1/file2.txt",
        "dir3/file5.txt",
        "dir3/file6.txt",
      ], test);

      await store.RemoveDirectory("dir3");
      Assert.Domain.Files([
        "dir1/file1.txt",
        "dir1/file2.txt",
      ], test);

      await store.RemoveDirectory("dir1");
      Assert.Domain.Files([], test);
    }
  }

  //===============================================================================================
  // TEST ADDITIONAL BEHAVIOR WHEN S3 REMOTE IS PRESENT
  //===============================================================================================

  [Fact]
  public async Task TestDownloadWhenMissingOnStat()
  {
    using (var test = BuildTestFileStore())
    {
      var s3 = Substitute.For<Aws.IS3>();
      var store = new FileStore(test.Root, Bucket, s3, Clock, Random, Logger);

      Assert.Domain.Files([], test);

      s3.GetObject(Bucket, Path1).Returns(Task.FromResult<Aws.S3Object?>(new Aws.S3Object
      {
        Key = Path1,
        ContentType = Http.ContentType.Text,
        ContentLength = ExampleContent1.Length,
        LastModified = Clock.Now,
        ETag = ETag,
        Content = ExampleContent1.AsStream()
      }));

      s3.GetObject(Bucket, Path2).Returns(Task.FromResult<Aws.S3Object?>(new Aws.S3Object
      {
        Key = Path2,
        ContentType = Http.ContentType.Text,
        ContentLength = ExampleContent1.Length,
        LastModified = Clock.Now,
        ETag = ETag,
        Content = ExampleContent2.AsStream()
      }));

      var stat1 = await store.Stat(Path1);
      var stat2 = await store.Stat(Path2);

      Assert.Present(stat1);
      Assert.Present(stat2);

      Assert.CloseEnough(Clock.Now, stat1.LastModifiedOn);
      Assert.CloseEnough(Clock.Now, stat1.LastModifiedOn);

      Assert.Domain.Files([
        Path1,
        Path2,
      ], test);

      Assert.Equal(ExampleContent1, await test.Read(Path1));
      Assert.Equal(ExampleContent2, await test.Read(Path2));

      await s3.Received().GetObject(Bucket, Path1);
      await s3.Received().GetObject(Bucket, Path2);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDownloadWhenMissing()
  {
    using (var test = BuildTestFileStore())
    {
      var s3 = Substitute.For<Aws.IS3>();
      var store = new FileStore(test.Root, Bucket, s3, Clock, Random, Logger);

      s3.GetObject(Bucket, Path1).Returns(Task.FromResult<Aws.S3Object?>(new Aws.S3Object
      {
        Key = Path1,
        ContentType = Http.ContentType.Text,
        ContentLength = ExampleContent1.Length,
        LastModified = Clock.Now,
        ETag = ETag,
        Content = ExampleContent1.AsStream()
      }));

      s3.GetObject(Bucket, Path2).Returns(Task.FromResult<Aws.S3Object?>(new Aws.S3Object
      {
        Key = Path2,
        ContentType = Http.ContentType.Text,
        ContentLength = ExampleContent1.Length,
        LastModified = Clock.Now,
        ETag = ETag,
        Content = ExampleContent2.AsStream()
      }));

      var content1 = await store.Load(Path1);
      var content2 = await store.Load(Path2);

      Assert.Present(content1);
      Assert.Present(content2);

      Assert.Equal(ExampleContent1, await content1.ReadAsString());
      Assert.Equal(ExampleContent2, await content2.ReadAsString());

      Assert.Equal(ExampleContent1, await test.Read(Path1));
      Assert.Equal(ExampleContent2, await test.Read(Path2));

      await s3.Received().GetObject(Bucket, Path1);
      await s3.Received().GetObject(Bucket, Path2);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestSaveRemote()
  {
    using (var test = BuildTestFileStore())
    {
      var s3 = Substitute.For<Aws.IS3>();
      var store = new FileStore(test.Root, Bucket, s3, Clock, Random, Logger);

      using (var stream = ExampleContent1.AsStream())
      {
        await store.Save(Path1, stream);
      }

      Assert.Equal(ExampleContent1, await test.Read(Path1));

      await s3.Received().Upload(Bucket, Path1, Arg.Any<FileStream>());
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestListRemote()
  {
    using (var test = BuildTestFileStore())
    {
      var dt1 = Moment.From(2025, 1, 1);
      var dt2 = Moment.From(2025, 2, 2);

      var s3 = Substitute.For<Aws.IS3>();
      var store = new FileStore(test.Root, Bucket, s3, Clock, Random, Logger);

      s3.ListObjects(Bucket).Returns(Task.FromResult<List<Aws.S3Object>>(new List<Aws.S3Object>
      {
        new Aws.S3Object
        {
          Key = Path1,
          ContentType = Http.ContentType.Text,
          ContentLength = ExampleContent1.Length,
          LastModified = dt1,
          ETag = ETag,
        },
        new Aws.S3Object
        {
          Key = Path2,
          ContentType = Http.ContentType.Text,
          ContentLength = ExampleContent2.Length,
          LastModified = dt2,
          ETag = ETag,
        }
      }));

      var list = await store.ListRemote();
      Assert.Equal(2, list.Count);
      Assert.Equal(Path1, list[0].Name);
      Assert.Equal(Path2, list[1].Name);
      Assert.Equal(ExampleContent1.Length, list[0].Size);
      Assert.Equal(ExampleContent2.Length, list[1].Size);
      Assert.Equal(dt1, list[0].LastModifiedOn);
      Assert.Equal(dt2, list[1].LastModifiedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestRemoteRemoveDirectory()
  {
    using (var test = BuildTestFileStore())
    {
      await test.SaveTestFile("dir1/file1.txt", "first");
      await test.SaveTestFile("dir1/file2.txt", "first");
      await test.SaveTestFile("dir2/file3.txt", "first");
      await test.SaveTestFile("dir2/file4.txt", "first");
      await test.SaveTestFile("dir3/file5.txt", "first");
      await test.SaveTestFile("dir3/file6.txt", "first");

      Assert.Domain.Files([
        "dir1/file1.txt",
        "dir1/file2.txt",
        "dir2/file3.txt",
        "dir2/file4.txt",
        "dir3/file5.txt",
        "dir3/file6.txt",
      ], test);

      var s3 = Substitute.For<Aws.IS3>();
      var store = new FileStore(test.Root, Bucket, s3, Clock, Random, Logger);

      await store.RemoveDirectory("dir2");
      Assert.Domain.Files([
        "dir1/file1.txt",
        "dir1/file2.txt",
        "dir3/file5.txt",
        "dir3/file6.txt",
      ], test);

      await store.RemoveDirectory("dir3");
      Assert.Domain.Files([
        "dir1/file1.txt",
        "dir1/file2.txt",
      ], test);

      await store.RemoveDirectory("dir1");
      Assert.Domain.Files([], test);

      await s3.Received().DeleteObjects(Bucket, "dir2");
      await s3.Received().DeleteObjects(Bucket, "dir3");
      await s3.Received().DeleteObjects(Bucket, "dir1");
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestDownload()
  {
    using (var test = BuildTestFileStore())
    {
      var s3 = Substitute.For<Aws.IS3>();
      var store = new FileStore(test.Root, Bucket, s3, Clock, Random, Logger);

      Assert.Null(await store.Stat(Path1));

      s3.GetObject(Bucket, Path1).Returns(Task.FromResult<Aws.S3Object?>(new Aws.S3Object
      {
        Key = Path1,
        ContentType = Http.ContentType.Text,
        ContentLength = ExampleContent1.Length,
        LastModified = Clock.Now,
        ETag = ETag,
        Content = ExampleContent1.AsStream()
      }));

      await store.Download(Path1);

      var stat = await store.Stat(Path1);
      Assert.Present(stat);
      Assert.CloseEnough(Clock.RealNow, stat.LastModifiedOn);
      using (var stream = await store.Load(Path1))
      {
        Assert.Present(stream);
        Assert.Equal(ExampleContent1, await stream.ReadAsString());
      }

      await s3.Received().GetObject(Bucket, Path1);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUpload()
  {
    using (var test = BuildTestFileStore())
    {
      var s3 = Substitute.For<Aws.IS3>();
      var store = new FileStore(test.Root, Bucket, s3, Clock, Random, Logger);

      await test.SaveTestFile(Path1, ExampleContent1);
      await store.Upload(Path1);

      await s3.Received().Upload(Bucket, Path1, Arg.Any<FileStream>());
    }
  }

  //===============================================================================================
  // TEST MISC
  //===============================================================================================

  [Fact]
  public void TestRootGetsExpanded()
  {
    using (var test = BuildTestFileStore())
    {
      var store = new FileStore("foo/../bar/../example", Clock, Random, Logger);
      Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), "example"), store.Root);
    }
  }

  //-----------------------------------------------------------------------------------------------
}