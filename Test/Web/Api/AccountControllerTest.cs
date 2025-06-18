namespace Void.Platform.Web.Api;

public class AccountControllerTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestMe()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.AuthenticateViaToken("active");

      var response = await test.Get($"/api/account/me");
      var me = Assert.Json.Object(response);
      Assert.Json.Properties([
        "id",
        "name",
        "email",
        "timeZone",
        "locale",
        "roles",
        "identities",
        "organizations",
        "authenticatedOn",
      ], me);

      Assert.Json.Equal(Identify("active"), me["id"]);
      Assert.Json.Equal("Active User", me["name"]);
      Assert.Json.Equal("active@example.com", me["email"]);
      Assert.Json.Equal("America/Los_Angeles", me["timeZone"]);
      Assert.Json.Equal("en-US", me["locale"]);

      var roles = Assert.Json.Array(0, me["roles"]);

      var identities = Assert.Json.Array(1, me["identities"]);
      var identity = Assert.Json.Object(identities[0]);
      Assert.Json.Equal("github", identity["provider"]);
      Assert.Json.Equal(Identify("active:github"), identity["id"]);
      Assert.Json.Equal(Identify("active"), identity["userId"]);
      Assert.Json.Equal("active", identity["identifier"]);
      Assert.Json.Equal("active", identity["userName"]);

      var orgs = Assert.Json.Array(1, me["organizations"]);
      var org = Assert.Json.Object(orgs[0]);
      Assert.Json.Equal(Identify("atari"), org["id"]);
      Assert.Json.Equal("Atari", org["name"]);
      Assert.Json.Equal("atari", org["slug"]);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestMeAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Get($"/api/account/me");
      Assert.Http.Unauthorized(response);
    }
  }

  [Fact]
  public async Task TestMePreflightIsAllowedAnonymously()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Options($"/api/account/me");
      Assert.Http.NoContent(response);
    }
  }

  //-----------------------------------------------------------------------------------------------
}