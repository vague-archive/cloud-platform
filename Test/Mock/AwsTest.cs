namespace Void.Platform.Test;

public static class AwsTest
{
  public const string DefaultRegion = "us-west-2";

  public static Aws.S3 S3(TestCase test, MockHttpMessageHandler bypass, string region = DefaultRegion)
  {
    return new Aws.S3(
      test.Clock,
      test.Logger,
      new Aws.Config
      {
        Region = Aws.Config.GetRegion(region),
        Credentials = Aws.Config.GetCredentials(test.Fake.AwsAccessKeyId, test.Fake.AwsSecretKey),
        HttpClientFactory = bypass.ToAmazonHttpClientFactory()
      }
    );
  }
}