namespace Void.Platform.Lib.Aws;

using System.Net;
using System.Text;
using System.Xml.Linq;

public class S3Test : TestCase
{
  //-----------------------------------------------------------------------------------------------

  private const string BUCKET = "my-bucket";
  private const string REGION = "us-west-2";

  //-----------------------------------------------------------------------------------------------

  private S3 BuildS3(MockHttpMessageHandler bypass)
  {
    return AwsTest.S3(this, bypass, REGION);
  }

  //===============================================================================================
  // TEST GET OBJECT
  //===============================================================================================

  [Fact]
  public async Task TestGetObject()
  {
    var content = "Hello World";
    var eTag = Http.ETag(content);
    var lastModified = Clock.Now;
    var key = "path/to/hello.txt";

    using var bypass = new MockHttpMessageHandler();
    bypass.When(HttpMethod.Get, $"https://{BUCKET}.s3.{REGION}.amazonaws.com/{key}")
      .Respond(req =>
      {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(content);
        response.Content.Headers.LastModified = lastModified.ToDateTimeOffset();
        response.Headers.Add(Http.Header.ETag, eTag);
        return response;
      });

    var s3 = BuildS3(bypass);
    var result = await s3.GetObject(BUCKET, key);

    Assert.Present(result);
    Assert.Present(result.Content);
    Assert.Equal(Http.ContentType.TextUtf8, result.ContentType);
    Assert.Equal(content.Length, result.ContentLength);
    Assert.Equal(lastModified.TruncateToSeconds(), result.LastModified);
    Assert.Equal(eTag, result.ETag);
    Assert.Equal(content, await result.Content.ReadAsString());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetObjectMetadata()
  {
    var content = "Hello World";
    var eTag = Http.ETag(content);
    var lastModified = Clock.Now;
    var key = "path/to/hello.txt";

    using var bypass = new MockHttpMessageHandler();
    bypass.When(HttpMethod.Head, $"https://{BUCKET}.s3.{REGION}.amazonaws.com/{key}")
      .Respond(req =>
      {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(content);
        response.Content.Headers.LastModified = lastModified.ToDateTimeOffset();
        response.Headers.Add(Http.Header.ETag, eTag);
        return response;
      });

    var s3 = BuildS3(bypass);
    var result = await s3.GetObjectMetadata(BUCKET, key);

    Assert.Present(result);
    Assert.Absent(result.Content);
    Assert.Equal(Http.ContentType.TextUtf8, result.ContentType);
    Assert.Equal(content.Length, result.ContentLength);
    Assert.Equal(lastModified.TruncateToSeconds(), result.LastModified);
    Assert.Equal(eTag, result.ETag);
  }

  //===============================================================================================
  // TEST UPLOAD OBJECT
  //===============================================================================================

  [Fact]
  public async Task TestUpload()
  {
    var key = "path/to/hello.txt";
    var contentType = Http.ContentType.Text;

    using var bypass = new MockHttpMessageHandler();
    bypass.When(HttpMethod.Put, $"https://{BUCKET}.s3.{REGION}.amazonaws.com/{key}")
      .Respond(req =>
      {
        RuntimeAssert.Present(req.Content);
        using var reader = new StreamReader(req.Content.ReadAsStreamAsync(CancelToken).Result, Encoding.UTF8);
        string uploadedContent = reader.ReadToEnd();
        Assert.StartsWith("B;chunk-signature=", uploadedContent);
        Assert.Equal(contentType, req.Content.Headers.ContentType?.MediaType);
        return new HttpResponseMessage(HttpStatusCode.OK);
      });

    using Stream stream = "Hello World".AsStream();

    var s3 = BuildS3(bypass);
    var result = await s3.Upload(BUCKET, key, stream, contentType);
    Assert.True(result);
  }

  //===============================================================================================
  // TEST LIST KEYS
  //===============================================================================================

  [Fact]
  public async Task TestListObjects()
  {
    var prefix = "path/to/foo";
    var page2 = "PAGE2";

    using var bypass = new MockHttpMessageHandler();
    bypass.When(HttpMethod.Get, $"https://{BUCKET}.s3.{REGION}.amazonaws.com")
      .Respond(req =>
      {
        Assert.Present(req.RequestUri);
        var qp = Http.Params(req.RequestUri);
        var continuationToken = qp.Get("continuation-token");
        Assert.Equal(prefix, qp.Get("prefix"));
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        if (continuationToken == page2)
        {
          response.Content = new StringContent(@$"
            <ListBucketResult>
              <Prefix>{prefix}</Prefix>
              <IsTruncated>false</IsTruncated>
              <Contents><Key>{prefix}/fourth</Key></Contents>
              <Contents><Key>{prefix}/fifth</Key></Contents>
            </ListBucketResult>
          ");
        }
        else
        {
          response.Content = new StringContent(@$"
            <ListBucketResult>
              <Prefix>{prefix}</Prefix>
              <IsTruncated>true</IsTruncated>
              <NextContinuationToken>{page2}</NextContinuationToken>
              <Contents><Key>{prefix}/first</Key></Contents>
              <Contents><Key>{prefix}/second</Key></Contents>
              <Contents><Key>{prefix}/third</Key></Contents>
            </ListBucketResult>
          ");
        }
        return response;
      });

    var s3 = BuildS3(bypass);
    var result = await s3.ListObjects(BUCKET, prefix);

    Assert.Equal(5, result.Count);
    Assert.Equal($"{prefix}/first", result[0].Key);
    Assert.Equal($"{prefix}/second", result[1].Key);
    Assert.Equal($"{prefix}/third", result[2].Key);
    Assert.Equal($"{prefix}/fourth", result[3].Key);
    Assert.Equal($"{prefix}/fifth", result[4].Key);
  }

  //===============================================================================================
  // TEST DELETE OBJECTS
  //===============================================================================================

  [Fact]
  public async Task TestDeleteObjects()
  {
    var prefix = "path/to/foo";

    using var bypass = new MockHttpMessageHandler();
    bypass.When(HttpMethod.Get, $"https://{BUCKET}.s3.{REGION}.amazonaws.com")
      .Respond(req =>
      {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(@$"
          <ListBucketResult>
            <Prefix>{prefix}</Prefix>
            <Contents><Key>{prefix}/first</Key></Contents>
            <Contents><Key>{prefix}/second</Key></Contents>
            <Contents><Key>{prefix}/third</Key></Contents>
          </ListBucketResult>
        ");
        return response;
      });

    bypass.When(HttpMethod.Post, $"https://{BUCKET}.s3.{REGION}.amazonaws.com")
      .Respond(async req =>
      {
        RuntimeAssert.Present(req.Content);
        var body = await req.Content.ReadAsStringAsync(CancelToken);
        var keys = XDocument.Parse(body)
          .Descendants("Object")
          .Select(o => o.Element("Key")?.Value)
          .Where(k => k is not null)
          .Select(k => $"<Deleted><Key>{k}</Key></Deleted>");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent($"<DeleteResult>{String.Join("", keys)}</DeleteResult>");
        return response;
      });

    var s3 = BuildS3(bypass);
    var result = await s3.DeleteObjects(BUCKET, prefix);
    Assert.Equal(3, result.Count);
    Assert.Equal($"{prefix}/first", result[0]);
    Assert.Equal($"{prefix}/second", result[1]);
    Assert.Equal($"{prefix}/third", result[2]);
  }

  //===============================================================================================
  // TEST STATIC HELPER METHODS
  //===============================================================================================

  [Fact]
  public void TestNormalizeKey()
  {
    Assert.Equal("path/to/foo.png", S3.NormalizeKey("path/to/foo.png"));
    Assert.Equal("path/to/foo bar.png", S3.NormalizeKey("path/to/foo bar.png"));
    Assert.Equal("path/to/foo bar %281%29.png", S3.NormalizeKey("path/to/foo bar (1).png"));
    Assert.Equal("path/to/foo.png", S3.NormalizeKey("path/./to/innapropriate/../foo.png"));
  }

  //-----------------------------------------------------------------------------------------------
}