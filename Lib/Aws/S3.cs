namespace Void.Platform.Lib.Aws;

using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3;
using System.Net;

//=================================================================================================
// S3 OBJECT
//=================================================================================================

public record S3Object
{
  public required string Key { get; init; }
  public required string ContentType { get; init; }
  public required long ContentLength { get; init; }
  public required Instant LastModified { get; init; }
  public required string ETag { get; init; }
  public Stream? Content { get; init; }

  public static S3Object From(GetObjectResponse response)
  {
    return new S3Object
    {
      Key = response.Key,
      ContentType = response.Headers[Http.Header.ContentType],
      ContentLength = response.ContentLength,
      LastModified = Instant.FromDateTimeOffset(response.LastModified),
      ETag = response.ETag,
      Content = response.ResponseStream,
    };
  }

  public static S3Object From(string key, GetObjectMetadataResponse response)
  {
    return new S3Object
    {
      Key = key,
      ContentType = response.Headers[Http.Header.ContentType],
      ContentLength = response.ContentLength,
      LastModified = Instant.FromDateTimeOffset(response.LastModified),
      ETag = response.ETag,
      Content = null,
    };
  }

  public static S3Object From(Amazon.S3.Model.S3Object value)
  {
    return new S3Object
    {
      Key = value.Key,
      ContentType = Http.DeriveContentType(value.Key),
      ContentLength = value.Size,
      LastModified = Instant.FromDateTimeOffset(value.LastModified),
      ETag = value.ETag,
      Content = null,
    };
  }
}

//=================================================================================================
// S3 SERVICE INTERFACE
//=================================================================================================

public record S3Statistics
{
  public long FileCount { get; set; }
  public long ByteCount { get; set; }
}

public interface IS3
{
  Task<S3Object?> GetObject(string bucket, string key);
  Task<S3Object?> GetObjectMetadata(string bucket, string key);
  Task<bool> Upload(string bucket, string key, Stream stream, string? contentType = null);
  Task<List<S3Object>> ListObjects(string bucket, string? prefix = null);
  Task<List<string>> DeleteObjects(string bucket, string prefix);
}

//=================================================================================================
// S3 SERVICE IMPLEMENTATION
//=================================================================================================

public class S3 : IS3
{
  public const int DeleteObjectsBatchSize = 1000;
  public const int ExtractArchiveBatchSize = 50;

  private IClock Clock { get; init; }
  private ILogger Logger { get; init; }
  private AmazonS3Client Client { get; init; }

  public S3(IClock clock, ILogger logger, Config? config = null)
  {
    Clock = clock;
    Logger = logger;
    if (config?.Credentials is null)
    {
      Client = new AmazonS3Client(new AmazonS3Config
      {
        RegionEndpoint = config?.Region,
        HttpClientFactory = config?.HttpClientFactory
      });
    }
    else
    {
      Client = new AmazonS3Client(config.Credentials, new AmazonS3Config
      {
        RegionEndpoint = config.Region,
        HttpClientFactory = config.HttpClientFactory
      });
    }
  }

  //===============================================================================================
  // GET OBJECT
  //===============================================================================================

  public async Task<S3Object?> GetObject(string bucket, string key)
  {
    try
    {
      return S3Object.From(await Client.GetObjectAsync(new GetObjectRequest
      {
        BucketName = bucket,
        Key = NormalizeKey(key),
      }));
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
      return null;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<S3Object?> GetObjectMetadata(string bucket, string key)
  {
    try
    {
      key = NormalizeKey(key);
      return S3Object.From(key, await Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
      {
        BucketName = bucket,
        Key = key,
      }));
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
      return null;
    }
  }

  //===============================================================================================
  // UPLOAD OBJECT
  //===============================================================================================

  public async Task<bool> Upload(string bucket, string key, Stream stream, string? contentType = null)
  {
    var start = Clock.Now;
    try
    {
      var transfer = new TransferUtility(Client);
      key = NormalizeKey(key);
      contentType = contentType ?? Http.DeriveContentType(key);
      await transfer.UploadAsync(new TransferUtilityUploadRequest
      {
        BucketName = bucket,
        Key = key,
        InputStream = stream,
        AutoCloseStream = false,
        ContentType = contentType,
      });
      Logger.Information("[S3] uploaded {key} to {bucket} in {duration}", key, bucket, Format.Duration(Clock.Now - start));
      return true;
    }
    catch (Exception ex)
    {
      Logger.Error("[S3] failed to upload {key} to {bucket} in {duration} - {message}", key, bucket, Format.Duration(Clock.Now - start), ex.Message);
      throw;
    }
  }

  //===============================================================================================
  // LIST OBJECTS
  //===============================================================================================

  public async Task<List<S3Object>> ListObjects(string bucket, string? prefix = null)
  {
    var result = new List<S3Object>();
    string? continuationToken = null;
    do
    {
      var list = await Client.ListObjectsV2Async(new ListObjectsV2Request
      {
        BucketName = bucket,
        Prefix = prefix,
        ContinuationToken = continuationToken,
      });
      result.AddRange(list.S3Objects.Select(o => S3Object.From(o)));
      continuationToken = list.IsTruncated ? list.NextContinuationToken : null;
    } while (continuationToken != null);
    return result;
  }

  //===============================================================================================
  // DELETE OBJECTS
  //===============================================================================================

  public async Task<List<string>> DeleteObjects(string bucket, string prefix)
  {
    var keys = (await ListObjects(bucket, prefix)).Select(o => o.Key);
    foreach (var batch in keys.Chunk(DeleteObjectsBatchSize))
    {
      Logger.Warning("[S3] deleting {count} objects", batch.Count());
      foreach (var key in batch)
        Logger.Warning("[S3] deleting {key}", key);
      await Client.DeleteObjectsAsync(new DeleteObjectsRequest
      {
        BucketName = bucket,
        Objects = batch.Select(key => new KeyVersion { Key = key }).ToList()
      });
    }
    return keys.ToList();
  }

  //===============================================================================================
  // STATIC HELPER METHODS
  //===============================================================================================

  public static string NormalizeKey(string key)
  {
    // filenames with () parens cause the following error
    //  "ERROR AwsServiceError: SignatureDoesNotMatch: The request signature we calculated does not match the signature you provided. Check your key and signing
    // so we explicitly encode them here
    //
    // see "characters that might require special handling"
    // - https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-keys.html
    key = key
      .Replace("(", "%28") //  replace ( with %28
      .Replace(")", "%29"); // replace ) with %29

    // also cleanup any "./", "../", or leading "/"
    return Path.GetFullPath(key, "/").TrimStart('/');
  }

  //-----------------------------------------------------------------------------------------------
}