namespace Void.Platform.Domain;

public partial class Content
{
  //-----------------------------------------------------------------------------------------------

  public record Object
  {
    public long Id { get; init; }
    public required string Path { get; init; }
    public required string Blake3 { get; init; }
    public required string MD5 { get; init; }
    public required string Sha256 { get; init; }
    public long ContentLength { get; init; }
    public required string ContentType { get; init; }
    public Instant CreatedOn { get; set; }
    public Instant UpdatedOn { get; set; }
  }

  //===============================================================================================
  // DATABASE FIELDS
  //===============================================================================================

  public const string ObjectFields = @"
    content_objects.id               as Id,
    content_objects.path             as Path,
    content_objects.blake3           as Blake3,
    content_objects.md5              as MD5,
    content_objects.sha256           as Sha256,
    content_objects.content_length   as ContentLength,
    content_objects.content_type     as ContentType,
    content_objects.created_on       as CreatedOn,
    content_objects.updated_on       as UpdatedOn
  ";

  //===============================================================================================
  // GET CONTENT OBJECTS
  //===============================================================================================

  public Object? GetObject(long id)
  {
    return Db.QuerySingleOrDefault<Object>(@$"
      SELECT {ObjectFields}
      FROM content_objects
      WHERE id = @Id
    ", new { Id = id });
  }

  public Object? GetObjectByBlake3(string blake3)
  {
    return Db.QuerySingleOrDefault<Object>(@$"
      SELECT {ObjectFields}
      FROM content_objects
      WHERE blake3 = @Blake3
    ", new { Blake3 = blake3 });
  }

  //===============================================================================================
  // UPLOAD CONTENT COMMAND
  //===============================================================================================

  public async Task<Result<(bool, Object)>> Upload(Stream content, string contentType)
  {
    using (var ms = await content.ReadIntoMemory())
    {
      var (exists, blob) = GetOrInsertObject(ms.ToArray(), contentType);
      if (exists)
        return Result.Ok((true, blob));
      await App.FileStore.Save(blob.Path, ms);
      return Result.Ok((false, blob));
    }
  }

  //-----------------------------------------------------------------------------------------------

  private (bool, Object) GetOrInsertObject(byte[] content, string contentType)
  {
    var blake3 = Crypto.HexString(Crypto.Blake3(content));
    var md5 = Crypto.HexString(Crypto.MD5(content));
    var sha256 = Crypto.HexString(Crypto.Sha256(content));
    var path = ContentPath(blake3);
    var contentLength = content.Length;

    return Db.Transaction(blake3, () =>
    {
      var blob = GetObjectByBlake3(blake3);
      if (blob is not null)
        return (true, blob);

      blob = new Object()
      {
        Path = path,
        Blake3 = blake3,
        MD5 = md5,
        Sha256 = sha256,
        ContentLength = contentLength,
        ContentType = contentType,
        CreatedOn = Now,
        UpdatedOn = Now,
      };

      var blobId = Db.Insert(@"
        INSERT INTO content_objects (
          path,
          blake3,
          md5,
          sha256,
          content_length,
          content_type,
          created_on,
          updated_on
        ) VALUES (
          @Path,
          @Blake3,
          @MD5,
          @Sha256,
          @ContentLength,
          @ContentType,
          @CreatedOn,
          @UpdatedOn
        )
      ", blob);

      return (false, blob with
      {
        Id = blobId
      });
    });
  }

  //===============================================================================================
  // MISC HELPER METHODS
  //===============================================================================================

  public static string ContentPath(string blake3)
  {
    RuntimeAssert.Present(blake3);
    RuntimeAssert.True(blake3.Length > 4);
    var prefix1 = blake3.Substring(0, 2);
    var prefix2 = blake3.Substring(2, 2);
    return $"content/{prefix1}/{prefix2}/{blake3}";
  }

  //-----------------------------------------------------------------------------------------------
}