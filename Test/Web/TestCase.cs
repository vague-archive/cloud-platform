namespace Void.Platform.Test;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Net.Http;
using Void.Platform.Web;

//=================================================================================================
//
// Summary:
//   context for web layer INTEGRATION tests
//     - sets up a TestWebApplication instance (our web app server)
//     - sets up an HttpClient instance (to make requests to our web app server)
//     - ensures web app domain is using the transactional database connection
//     - ensures auth cookies are persisted across requests
//     - ensures a known CSRF token can be used for POST requests
//     - provides integration test helpers like Login(), Get(), etc
//
//=================================================================================================

public class WebIntegrationTest : DomainTest, IDisposable
{
  public readonly TestWebApplication WebApp;
  public readonly HttpClient Client;

  public WebIntegrationTest(TestCase test) : base(test)
  {
    WebApp = new TestWebApplication(this);
    Client = WebApp.CreateClient(new WebApplicationFactoryClientOptions
    {
      HandleCookies = true,
      AllowAutoRedirect = false,
      BaseAddress = new Uri("https://localhost") // IMPORTANT: must be https for secure cookies to persist across requests
    });
  }

  private bool disposed = false;
  public override void Dispose(bool disposing)
  {
    if (disposed)
      return;

    if (disposing)
    {
      Client.Dispose();
      WebApp.Dispose();
    }
    disposed = true;
    base.Dispose(disposing);
  }

  ~WebIntegrationTest()
  {
    Dispose(false);
  }

  //===============================================================================================
  // PREPARE HTTP REQUEST CONTENT
  //===============================================================================================

  public FormUrlEncodedContent BuildForm(Dictionary<string, string> values, bool withCsrf = true)
  {
    if (withCsrf)
    {
      values.Add(Config.CsrfFieldName, KnownAntiForgery.Token);
    }
    return new FormUrlEncodedContent(values);
  }

  //===============================================================================================
  // PERFORM HTTP REQUESTS
  //===============================================================================================

  public async Task<HttpResponseMessage> Get(string path, bool redirect = false)
  {
    var response = await Client.GetAsync(path, CancelToken);
    return await MaybeAutoRedirect(response, redirect);
  }

  public async Task<HttpResponseMessage> Get(Uri path, bool redirect = false)
  {
    var response = await Client.GetAsync(path, CancelToken);
    return await MaybeAutoRedirect(response, redirect);
  }

  public async Task<HttpResponseMessage> Options(string path, bool redirect = false)
  {
    var request = BuildRequest(HttpMethod.Options, path);
    var response = await Client.SendAsync(request, CancelToken);
    return await MaybeAutoRedirect(response, redirect);
  }

  public async Task<HttpResponseMessage> Post(string path, HttpContent? content = null, Dictionary<string, string>? headers = null, bool redirect = false)
  {
    var request = BuildRequest(HttpMethod.Post, path, content, headers);
    var response = await Client.SendAsync(request, CancelToken);
    return await MaybeAutoRedirect(response, redirect);
  }

  public async Task<HttpResponseMessage> Post(string path, FormUrlEncodedContent content, bool redirect = false)
  {
    var response = await Client.PostAsync(path, content, CancelToken);
    return await MaybeAutoRedirect(response, redirect);
  }

  public async Task<HttpResponseMessage> Post(string path, Stream content, Dictionary<string, string>? headers = null, bool redirect = false)
  {
    using (var httpContent = new StreamContent(content))
    {
      return await Post(path, httpContent, headers, redirect);
    }
  }

  public async Task<HttpResponseMessage> PostJSON<T>(string path, T content, Dictionary<string, string>? headers = null, bool redirect = false)
  {
    return await Post(path, new StringContent(Json.Serialize(content), System.Text.Encoding.UTF8, "application/json"), headers, redirect);
  }

  public async Task<HttpResponseMessage> HxPost(string path, HttpContent? content = null, Dictionary<string, string>? headers = null, bool redirect = false)
  {
    var request = BuildRequest(HttpMethod.Post, path, content, headers);
    request.Headers.Add(Http.Header.HxRequest, "true");
    var response = await Client.SendAsync(request, CancelToken);
    return await MaybeAutoRedirect(response, redirect);
  }

  private HttpRequestMessage BuildRequest(HttpMethod method, string path, HttpContent? content = null, Dictionary<string, string>? headers = null)
  {
    var request = new HttpRequestMessage(method, path)
    {
      Content = content
    };
    request.Headers.Add(Http.Header.CSRFToken, KnownAntiForgery.Token);
    if (headers is not null)
    {
      foreach (var pair in headers)
      {
        request.Headers.Add(pair.Key, pair.Value);
      }
    }
    return request;
  }

  //-----------------------------------------------------------------------------------------------

  private async Task<HttpResponseMessage> MaybeAutoRedirect(HttpResponseMessage response, bool redirect)
  {
    if (redirect)
      return await AutoRedirect(response);
    else
      return response;
  }

  public async Task<HttpResponseMessage> AutoRedirect(HttpResponseMessage response)
  {
    while (IsRedirect(response, out var location))
    {
      response = await Get(location);
    }
    return response;
  }

  private bool IsRedirect(HttpResponseMessage response, out string location)
  {
    if (302 == (int) response.StatusCode && response.Headers.Location is not null)
    {
      location = response.Headers.Location.ToString();
      return true;
    }
    else if (response.Headers.TryGetValues(Http.Header.HxRedirect, out var locations))
    {
      location = locations.First();
      return true;
    }
    else
    {
      location = "";
      return false;
    }
  }

  //===============================================================================================
  // AUTHENTICATE
  //===============================================================================================

  public void Anonymous()
  {
    Client.DefaultRequestHeaders.Remove(Http.Header.Authorization);
    Client.DefaultRequestHeaders.Remove(Http.Header.Cookie);
  }

  public Account.AuthenticatedUser Login(string id)
  {
    var user = App.Account.GetAuthenticatedUser(FixtureFactory.Identify(id));
    Assert.NotNull(user);
    Login(user);
    return user;
  }

  public void Login(Account.AuthenticatedUser user)
  {
    var authCookie = CreateAuthCookie(user);
    Client.DefaultRequestHeaders.Add("Cookie", $"{Config.AuthCookieName}={authCookie}");
  }

  public void AuthenticateViaToken(string tokenValue)
  {
    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Crypto.GenerateToken(tokenValue));
  }

  private string CreateAuthCookie(Account.AuthenticatedUser user)
  {
    var services = WebApp.Services;
    var principal = UserPrincipal.From(user, Config.CookieAuthenticationScheme);
    var authTicket = new AuthenticationTicket(principal, Config.CookieAuthenticationScheme);
    var dataProtector = services.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
                        Config.CookieAuthenticationScheme,
                         "v2");
    var ticketBytes = TicketSerializer.Default.Serialize(authTicket);
    var protectedTicket = dataProtector.Protect(ticketBytes);
    return Convert.ToBase64String(protectedTicket);
  }
}

//=================================================================================================
//
// Summary:
//   The subject under test (SUT) is our web application
//     - implements a WebApplicationFactory<Program> to run our full WebApplication stack
//     - replaces the IClock service with our FakeClock
//     - replaces the IRandom service with our TestRandom
//     - replaces the IDbConnection service with our transactional IDBConnection
//     - replaces the IFileStore service with our TestFileStore
//     - replaces the IMinions service with our TestMinions
//     - replaces the IHttpClientFactory service with a MockHttpClientFactory (below)
//     - replaces the IAntiforgery service with one that validates against a known CSRF TOKEN
//
//=================================================================================================

public class TestWebApplication : WebApplicationFactory<Program>
{
  private TestLogger Logger { get; init; }
  private TestClock Clock { get; init; }
  private TestRandom Random { get; init; }
  private IDatabase Db { get; init; }
  private ICache Cache { get; init; }
  private IMailer Mailer { get; init; }
  private TestFileStore FileStore { get; init; }
  private TestMinions Minions { get; init; }
  private MockHttpClientFactory HttpClientFactory { get; init; }

  public TestWebApplication(WebIntegrationTest test)
  {
    Logger = test.Logger;
    Clock = test.Clock;
    Random = test.Random;
    Db = test.Db;
    Cache = test.Cache;
    Mailer = test.Mailer;
    FileStore = test.FileStore;
    Minions = test.Minions;
    HttpClientFactory = test.HttpClientFactory;
  }

  public MockHttpMessageHandler HttpClient
  {
    get
    {
      return HttpClientFactory.Handler;
    }
  }

  protected override IHost CreateHost(IHostBuilder builder)
  {
    builder.ConfigureHostConfiguration(config =>
    {
      config.AddCommandLine(TestConfig.CommandLineArgs);
    });
    return base.CreateHost(builder);
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services
        .RemoveService<IClock>()
        .RemoveService<IRandom>()
        .RemoveService<ILogger>()
        .RemoveService<IDatabase>()
        .RemoveService<ICache>()
        .RemoveService<IDistributedCache>()
        .RemoveService<IMailer>()
        .RemoveService<IFileStore>()
        .RemoveService<IHttpClientFactory>()
        .RemoveService<IAntiforgery>()
        .RemoveVoidWorkers()
        .RemoveAll<IHostedService>(); // NO hosted services (e.g Quartz workers) when running tests

      services
        .AddSingleton<ILogger>(Logger)
        .AddSingleton<IClock>(Clock)
        .AddSingleton<IRandom>(Random)
        .AddSingleton<IDatabase>(Db)
        .AddSingleton<ICache>(Cache)
        .AddSingleton<IDistributedCache, MemoryDistributedCache>()
        .AddSingleton<IMailer>(Mailer)
        .AddSingleton<IFileStore>(FileStore)
        .AddSingleton<IMinions>(Minions)
        .AddSingleton<IHttpClientFactory>(HttpClientFactory)
        .AddSingleton<IAntiforgery, KnownAntiForgery>();
    });
    base.ConfigureWebHost(builder);
  }
}

//-------------------------------------------------------------------------------------------------