namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class ProgramTest : TestCase
{
  //===============================================================================================
  // TEST integration tests run in a Test environment
  //===============================================================================================

  [Fact]
  public void TestEnvironmentName()
  {
    using (var test = new WebIntegrationTest(this))
    {
      using var scope = test.WebApp.Services.CreateScope();
      var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
      Assert.Equal(TestConfig.EnvironmentName, env.EnvironmentName);
    }
  }

  //===============================================================================================
  // TEST health check
  //===============================================================================================

  [Fact]
  public async Task TestHealthCheck()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Get("/ping");
      var content = Assert.Http.Ok(response);
      Assert.NotNull(content);
      Assert.Equal("Healthy", content);
    }
  }

  //===============================================================================================
  // TEST page layout
  //===============================================================================================

  [Fact]
  public async Task TestDefaultPageLayout()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var response = await test.Get("/profile");
      var doc = Assert.Html.Document(response);
      var head = Assert.Html.Select("head", doc);
      var body = Assert.Html.Select("body", doc);
      var main = Assert.Html.Select("main", body);
      var links = Assert.Html.SelectAll("link", head);
      var scripts = Assert.Html.SelectAll("script", head);

      Assert.Html.Title("Profile", head);
      Assert.Html.Meta("width=device-width, initial-scale=1.0", "viewport", head);
      Assert.Html.Meta(user.Id.ToString(), "userId", head);
      Assert.Html.Meta(user.Name, "userName", head);
      Assert.Html.Meta(user.Email, "userEmail", head);
      Assert.Html.Meta("TEST-CSRF-TOKEN", "csrf-token", head);

      Assert.Single(links);
      Assert.Equal(2, scripts.Count);

      Assert.StartsWith("/main.css", links[0].GetAttribute("href"));
      Assert.StartsWith("/main.js", scripts[0].GetAttribute("src"));
      Assert.StartsWith("/vendor.js", scripts[1].GetAttribute("src"));

      Assert.Equal("h-full flex flex-col overflow-hidden bg-graphpaper", body.ClassName);
      Assert.Equal("h-full flex-1 overflow-auto bg-gray-50", main.ClassName);
    }
  }

  //===============================================================================================
  // TEST different ways a razor page can respond
  //===============================================================================================

  [Fact]
  public async Task TestPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/profile");
      var doc = Assert.Html.Document(response);
      Assert.Html.Title("Profile", doc);
    }
  }

  [Fact]
  public async Task TestPageNotFound()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/no-such-page");
      var doc = Assert.Html.Document(response, expectedStatus: Http.StatusCode.NotFound);
      Assert.Html.Title("Page Not Found", doc);
    }
  }

  [Fact]
  public async Task TestPageCrash()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("sysadmin");
      var response = await test.Post("/sysadmin/crash");
      var doc = Assert.Html.Document(response, expectedStatus: Http.StatusCode.InternalServerError);
      Assert.Html.Title("Unexpected Error", doc);
    }
  }

  //===============================================================================================
  // TEST different ways api can respond
  //===============================================================================================

  [Fact]
  public async Task TestApiPing()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Get("/api/ping");
      var result = Assert.Json.Object(response);
      Assert.Json.Equal("pong", result["ping"]);
      Assert.Http.HasHeader("*", Http.Header.AccessControlAllowOrigin, response);
      Assert.Http.HasHeader("GET,HEAD,PUT,PATCH,POST,DELETE", Http.Header.AccessControlAllowMethods, response);
    }
  }

  [Fact]
  public async Task TestApiNotFound()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Get("/api/no/such/endpoint");
      Assert.Http.NotFound(response);
    }
  }

  [Fact]
  public async Task TestApiBadRequest()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("sysadmin");
      var response = await test.Get("/api/test/bad-request");
      var content = Assert.Http.BadRequest(response);
      Assert.NotNull(content);
      var problem = Json.Deserialize<ValidationProblemDetails>(content);
      Assert.Equal(Http.StatusCode.BadRequest, problem.Status);
      Assert.Equal("One or more validation errors occurred.", problem.Title);
      Assert.Equal(["is invalid"], problem.Errors["something"]);
      Assert.Null(problem.Detail);
    }
  }

  [Fact]
  public async Task TestApiError()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("sysadmin");
      var response = await test.Get("/api/test/error");
      var content = Assert.Http.InternalServerError(response);
      Assert.NotNull(content);
      var problem = Json.Deserialize<ProblemDetails>(content);
      Assert.Equal(Http.StatusCode.InternalServerError, problem.Status);
      Assert.Equal("An error occurred while processing your request.", problem.Title);
      Assert.Equal("uh oh", problem.Detail);
    }
  }

  [Fact]
  public async Task TestApiCrash()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("sysadmin");
      var response = await test.Get("/api/test/crash");
      var content = Assert.Http.InternalServerError(response);
      Assert.NotNull(content);
      var problem = Json.Deserialize<ValidationProblemDetails>(content);
      Assert.NotNull(problem);
      Assert.Equal(Http.StatusCode.InternalServerError, problem.Status);
      Assert.Equal("An error occurred while processing your request.", problem.Title);
      Assert.Equal([], problem.Errors);
      Assert.Null(problem.Detail);
      Assert.Null(problem.Instance);
    }
  }

  //-----------------------------------------------------------------------------------------------
}