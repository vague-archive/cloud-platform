namespace Void.Platform.Test;

//=================================================================================================
//
// Summary:
//   A mock IHttpClientFactory that uses a MockHttpMessageHandler for handling HttpClient requests
//
//=================================================================================================

public class MockHttpClientFactory : IHttpClientFactory
{
  public readonly MockHttpMessageHandler Handler;

  public MockHttpClientFactory()
  {
    Handler = new MockHttpMessageHandler();
  }

  public HttpClient CreateClient(string name)
  {
    return Handler.ToHttpClient();
  }
}

//=================================================================================================
//
// Summary:
//   A mock Amazon.Runtime.HttpClientFactory that uses a MockHttpMessageHandler
//   for handling HttpClient requests to Amazon
//
//=================================================================================================

public class MockAmazonHttpClientFactory : Amazon.Runtime.HttpClientFactory
{
  private readonly HttpClient httpClient;
  public MockAmazonHttpClientFactory(HttpClient httpClient)
  {
    this.httpClient = httpClient;
  }
  public override HttpClient CreateHttpClient(Amazon.Runtime.IClientConfig clientConfig)
  {
    return httpClient;
  }
}

public static class MockAmazonHttpClientFactoryExtensions
{
  public static MockAmazonHttpClientFactory ToAmazonHttpClientFactory(this MockHttpMessageHandler handler)
  {
    return new MockAmazonHttpClientFactory(handler.ToHttpClient());
  }
}