namespace Void.Platform.Web;

public class ApiSerializerTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestSerializeBasics()
  {
    var serializer = new ApiSerializer();

    Assert.Equal("\"hello world\"", serializer.Serialize("hello world"));
    Assert.Equal("42", serializer.Serialize(42));
    Assert.Equal("3.14", serializer.Serialize(3.14));
    Assert.Equal("true", serializer.Serialize(true));
    Assert.Equal("false", serializer.Serialize(false));
    Assert.Equal("[1,2,3]", serializer.Serialize(new int[] { 1, 2, 3 }));
    Assert.Equal("[\"foo\",\"bar\"]", serializer.Serialize(new string[] { "foo", "bar" }));
    Assert.Equal("{\"foo\":\"bar\"}", serializer.Serialize(new { foo = "bar" }));
    Assert.Equal("{\"foo\":\"bar\"}", serializer.Serialize(new { Foo = "bar" }));
    Assert.Equal("{\"firstName\":\"bob\"}", serializer.Serialize(new { FirstName = "bob" }));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestSerializeUser()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.BuildUser();

      var serializer = new ApiSerializer();
      var serialized = serializer.Serialize(user);

      var result = Assert.Json.Object(serialized);

      Assert.Json.Properties([
        "id",
        "name",
        "email",
        "timeZone",
        "locale",
      ], result);

      Assert.Json.Equal(user.Id, result["id"]);
      Assert.Json.Equal(user.Name, result["name"]);
      Assert.Json.Equal(user.Email, result["email"]);
      Assert.Json.Equal(user.TimeZone, result["timeZone"]);
      Assert.Json.Equal(user.Locale, result["locale"]);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestSerializeUserWithAssociations()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.BuildUser();
      var github = test.Factory.BuildIdentity(user, provider: Account.IdentityProvider.GitHub);
      var discord = test.Factory.BuildIdentity(user, provider: Account.IdentityProvider.Discord);
      var nintendo = test.Factory.BuildOrganization(name: "Nintendo");
      var atari = test.Factory.BuildOrganization(name: "Atari");

      user.Identities = new List<Account.Identity> { github, discord };
      user.Roles = new List<Account.UserRole> { Account.UserRole.SysAdmin };
      user.Organizations = new List<Account.Organization> { atari, nintendo };

      var serializer = new ApiSerializer();
      var serialized = serializer.Serialize(user);

      var result = Assert.Json.Object(serialized);

      Assert.Json.Properties([
        "id",
        "name",
        "email",
        "timeZone",
        "locale",
        "roles",
        "identities",
        "organizations",
      ], result);

      Assert.Json.Equal(user.Id, result["id"]);
      Assert.Json.Equal(user.Name, result["name"]);
      Assert.Json.Equal(user.Email, result["email"]);
      Assert.Json.Equal(user.TimeZone, result["timeZone"]);
      Assert.Json.Equal(user.Locale, result["locale"]);

      var roles = Assert.Json.Array(1, result["roles"]);
      Assert.Json.Equal("sysadmin", roles[0]);

      var identities = Assert.Json.Array(2, result["identities"]);
      var identity1 = Assert.Json.Object(identities[0]);
      var identity2 = Assert.Json.Object(identities[1]);
      Assert.Json.Equal("github", identity1["provider"]);
      Assert.Json.Equal("discord", identity2["provider"]);

      Assert.Json.Equal(github.Id, identity1["id"]);
      Assert.Json.Equal(github.UserId, identity1["userId"]);
      Assert.Json.Equal(github.Identifier, identity1["identifier"]);
      Assert.Json.Equal(github.UserName, identity1["userName"]);
      Assert.Json.Equal(discord.Id, identity2["id"]);
      Assert.Json.Equal(discord.UserId, identity2["userId"]);
      Assert.Json.Equal(discord.Identifier, identity2["identifier"]);
      Assert.Json.Equal(discord.UserName, identity2["userName"]);

      var orgs = Assert.Json.Array(2, result["organizations"]);
      var org1 = Assert.Json.Object(orgs[0]);
      var org2 = Assert.Json.Object(orgs[1]);
      Assert.Json.Equal(atari.Id, org1["id"]);
      Assert.Json.Equal(atari.Name, org1["name"]);
      Assert.Json.Equal(atari.Slug, org1["slug"]);
      Assert.Json.Equal(nintendo.Id, org2["id"]);
      Assert.Json.Equal(nintendo.Name, org2["name"]);
      Assert.Json.Equal(nintendo.Slug, org2["slug"]);
    }
  }

  //-----------------------------------------------------------------------------------------------
}