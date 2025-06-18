namespace Void.Platform.Domain;

public class ContentObjectTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  readonly static string CONTENT = "Hello World";
  readonly static string BLAKE3 = Crypto.HexString(Crypto.Blake3(CONTENT));
  readonly static string MD5 = Crypto.HexString(Crypto.MD5(CONTENT));
  readonly static string SHA256 = Crypto.HexString(Crypto.Sha256(CONTENT));
  readonly static string PATH = Content.ContentPath(BLAKE3);

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestExpectedConstants()
  {
    Assert.Equal("41f8394111eb713a22165c46c90ab8f0fd9399c92028fd6d288944b23ff5bf76", BLAKE3);
    Assert.Equal("b10a8db164e0754105b7a99be72e3fe5", MD5);
    Assert.Equal("a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e", SHA256);
    Assert.Equal("content/41/f8/41f8394111eb713a22165c46c90ab8f0fd9399c92028fd6d288944b23ff5bf76", PATH);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestContentPath()
  {
    Assert.Equal(PATH, Content.ContentPath(BLAKE3));
    Assert.Equal("content/ab/cd/abcdefghijklmnopqrstuvwxyz", Content.ContentPath("abcdefghijklmnopqrstuvwxyz"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestFixtureFactory()
  {
    using (var test = new DomainTest(this))
    {
      var blob = test.Factory.CreateContentObject(CONTENT);

      Assert.Equal(PATH, blob.Path);
      Assert.Equal(BLAKE3, blob.Blake3);
      Assert.Equal(MD5, blob.MD5);
      Assert.Equal(SHA256, blob.Sha256);
      Assert.Equal(CONTENT.Length, blob.ContentLength);
      Assert.Equal(Http.ContentType.Text, blob.ContentType);
      Assert.Equal(Clock.Now, blob.CreatedOn);
      Assert.Equal(Clock.Now, blob.UpdatedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetObject()
  {
    using (var test = new DomainTest(this))
    {
      var blob1 = test.Factory.CreateContentObject("first");
      var blob2 = test.Factory.CreateContentObject("second");

      var reload1 = test.App.Content.GetObject(blob1.Id);
      var reload2 = test.App.Content.GetObject(blob2.Id);

      Assert.Present(reload1);
      Assert.Present(reload2);

      Assert.Domain.Equal(blob1, reload1);
      Assert.Domain.Equal(blob2, reload2);

      Assert.Absent(test.App.Content.GetObject(Identify("unknown")));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetObjectByBlake3()
  {
    using (var test = new DomainTest(this))
    {
      var blob1 = test.Factory.CreateContentObject("first");
      var blob2 = test.Factory.CreateContentObject("second");

      var reload1 = test.App.Content.GetObjectByBlake3(Crypto.HexString(Crypto.Blake3("first")));
      var reload2 = test.App.Content.GetObjectByBlake3(Crypto.HexString(Crypto.Blake3("second")));

      Assert.Present(reload1);
      Assert.Present(reload2);

      Assert.Domain.Equal(blob1, reload1);
      Assert.Domain.Equal(blob2, reload2);

      Assert.Absent(test.App.Content.GetObjectByBlake3(Crypto.HexString(Crypto.Blake3("unknown"))));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUpload()
  {
    using (var test = new DomainTest(this))
    {
      var localPath = "path/to/example.txt";
      await test.FileStore.SaveTestFile(localPath, CONTENT);

      Assert.Equal([
        localPath
      ], test.FileStore.ListFileNames());

      using var stream = await test.FileStore.Load(localPath);
      Assert.Present(stream);

      var result = await test.App.Content.Upload(stream, Http.ContentType.Text);
      var (exists, blob) = Assert.Succeeded(result);

      Assert.False(exists);
      Assert.Equal(PATH, blob.Path);
      Assert.Equal(BLAKE3, blob.Blake3);
      Assert.Equal(MD5, blob.MD5);
      Assert.Equal(SHA256, blob.Sha256);
      Assert.Equal(CONTENT.Length, blob.ContentLength);
      Assert.Equal(Http.ContentType.Text, blob.ContentType);
      Assert.Equal(Clock.Now, blob.CreatedOn);
      Assert.Equal(Clock.Now, blob.UpdatedOn);

      var reload = test.App.Content.GetObject(blob.Id);
      Assert.Present(reload);
      Assert.Domain.Equal(blob, reload);

      Assert.Equal([
        blob.Path,
        localPath,
      ], test.FileStore.ListFileNames());

      Assert.True(test.FileStore.ContainsFile(blob.Path));
      var content = await test.FileStore.Read(blob.Path);
      Assert.Equal(CONTENT, content);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUploadAlreadyExists()
  {
    using (var test = new DomainTest(this))
    {
      var existing = test.Factory.CreateContentObject(CONTENT);

      var localPath = "local/example.txt";
      await test.FileStore.SaveTestFile(localPath, CONTENT);
      await test.FileStore.SaveTestFile(existing.Path, CONTENT);

      Assert.Equal([
        existing.Path,
        localPath,
      ], test.FileStore.ListFileNames());

      using var stream = await test.FileStore.Load(localPath);
      Assert.Present(stream);

      var result = await test.App.Content.Upload(stream, Http.ContentType.Text);
      var (exists, blob) = Assert.Succeeded(result);

      Assert.True(exists);
      Assert.Domain.Equal(existing, blob);

      Assert.Equal([
        blob.Path,
        localPath,
      ], test.FileStore.ListFileNames());
    }
  }

  //===============================================================================================
  // TEST DB CONSTRAINTS
  //===============================================================================================

  [Fact]
  public void TestBlake3UniquenessConstraint()
  {
    using (var test = new DomainTest(this))
    {
      var blob1 = test.Factory.CreateContentObject("first", md5: "dummy1", sha256: "dummy1");
      var blob2 = test.Factory.CreateContentObject("second", md5: "dummy2", sha256: "dummy2");

      var ex = Assert.Throws<MySqlConnector.MySqlException>(() => test.Factory.CreateContentObject("first"));
      Assert.Equal($"Duplicate entry '{Crypto.HexString(Crypto.Blake3("first"))}' for key 'content_objects.blake3_index'", ex.Message);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestMD5UniquenessConstraint()
  {
    using (var test = new DomainTest(this))
    {
      var blob1 = test.Factory.CreateContentObject("first", blake3: "dummy1", sha256: "dummy1");
      var blob2 = test.Factory.CreateContentObject("second", blake3: "dummy2", sha256: "dummy2");

      var ex = Assert.Throws<MySqlConnector.MySqlException>(() => test.Factory.CreateContentObject("first"));
      Assert.Equal($"Duplicate entry '{Crypto.HexString(Crypto.MD5("first"))}' for key 'content_objects.md5_index'", ex.Message);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestSha256UniqunessConstraint()
  {
    using (var test = new DomainTest(this))
    {
      var blob1 = test.Factory.CreateContentObject("first", blake3: "dummy1", md5: "dummy1");
      var blob2 = test.Factory.CreateContentObject("second", blake3: "dummy2", md5: "dummy2");

      var ex = Assert.Throws<MySqlConnector.MySqlException>(() => test.Factory.CreateContentObject("first"));
      Assert.Equal($"Duplicate entry '{Crypto.HexString(Crypto.Sha256("first"))}' for key 'content_objects.sha256_index'", ex.Message);
    }
  }

  //-----------------------------------------------------------------------------------------------
}