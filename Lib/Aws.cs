namespace Void.Platform.Lib.Aws;

using Amazon.Runtime;
using Amazon;

public record Config
{
  public RegionEndpoint? Region { get; set; }
  public AWSCredentials? Credentials { get; set; }
  public HttpClientFactory? HttpClientFactory { get; set; }

  public static RegionEndpoint GetRegion(string region)
  {
    return RegionEndpoint.GetBySystemName(region);
  }

  public static AWSCredentials GetCredentials(string accessKeyId, string secretAccessKey)
  {
    return new BasicAWSCredentials(accessKeyId, secretAccessKey);
  }
}

public class Client
{
  public readonly S3 S3;

  public Client(IClock clock, ILogger logger, Config? config = null)
  {
    S3 = new S3(clock, logger, config);
  }
}