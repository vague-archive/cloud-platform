namespace Void.Platform.Web.Api;

public class MonitorControllerTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestPing()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Get($"/api/ping");
      var json = Assert.Json.Object(response);
      Assert.Json.Equal("pong", json["ping"]);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestAuthValidate()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");
      var response = await test.Get("/api/auth/validate");
      var content = Assert.Http.Ok(response);
      Assert.Equal("Valid Authorization Token", content);
    }
  }

  [Fact]
  public async Task TestAuthValidatePreflight()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Options("/api/auth/validate");
      Assert.Http.NoContent(response);
    }
  }

  [Fact]
  public async Task TestAuthValidateAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Get("/api/auth/validate");
      Assert.Http.Unauthorized(response);
    }
  }

  //-----------------------------------------------------------------------------------------------
}