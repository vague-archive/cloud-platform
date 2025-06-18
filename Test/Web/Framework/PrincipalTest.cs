namespace Void.Platform.Web;

using System.Security.Claims;

public class UserPrincipalTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUserClaims()
  {
    Assert.Equal(ClaimTypes.NameIdentifier, UserClaim.SID);
    Assert.Equal("id", UserClaim.Id);
    Assert.Equal("name", UserClaim.Name);
    Assert.Equal("email", UserClaim.Email);
    Assert.Equal("timezone", UserClaim.TimeZone);
    Assert.Equal("locale", UserClaim.Locale);
    Assert.Equal("role", UserClaim.Role);
    Assert.Equal("identity", UserClaim.Identity);
    Assert.Equal("organization", UserClaim.Organization);
    Assert.Equal("authenticatedOn", UserClaim.AuthenticatedOn);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUserClaimValues()
  {
    using (var test = new DomainTest(this))
    {
      var role = Account.UserRole.SysAdmin;
      var org = test.Factory.BuildOrganization(id: 123);
      var game = test.Factory.BuildGame(org);
      var user = test.Factory.BuildUser();
      var identity = test.Factory.BuildIdentity(user, provider: Account.IdentityProvider.GitHub, username: "johndoe");

      Assert.Equal("sysadmin", UserClaim.Value(role));
      Assert.Equal("123", UserClaim.Value(org));
      Assert.Equal("123", UserClaim.Value(game));
      Assert.Equal("github:johndoe", UserClaim.Value(identity));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUserPrincipal()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.BuildUser(id: 42, name: "John Doe", email: "john@example.com", timezone: "Europe/Paris", locale: "en-US");
      var github = test.Factory.BuildIdentity(user, provider: Account.IdentityProvider.GitHub, username: "githubber");
      var discord = test.Factory.BuildIdentity(user, provider: Account.IdentityProvider.Discord, username: "discorder");
      var atari = test.Factory.BuildOrganization(id: 100, slug: "atari");
      var nintendo = test.Factory.BuildOrganization(id: 200, slug: "nintendo");
      var other = test.Factory.BuildOrganization(id: 300, slug: "other");
      var sysadmin = Account.UserRole.SysAdmin;
      var pong = test.Factory.BuildGame(atari);
      var mario = test.Factory.BuildGame(nintendo);
      var snakes = test.Factory.BuildGame(other);

      var authenticatedUser = test.Factory.BuildAuthenticatedUser(user,
        roles: [sysadmin],
        identities: [github, discord],
        organizations: [atari, nintendo]
      );

      var principal = UserPrincipal.From(authenticatedUser, TestConfig.AuthenticationScheme);

      Assert.True(principal.IsLoggedIn);
      Assert.Equal(42, principal.Id);
      Assert.Equal("John Doe", principal.Name);
      Assert.Equal("john@example.com", principal.Email);
      Assert.Equal("Europe/Paris", principal.TimeZone);
      Assert.Equal("en-US", principal.Locale);
      Assert.Equal([Account.UserRole.SysAdmin], principal.Roles);
      Assert.Equal(["github:githubber", "discord:discorder"], principal.Identities);
      Assert.Equal([atari.Id, nintendo.Id], principal.Organizations);

      Assert.True(principal.IsInRole("SYSADMIN"));
      Assert.True(principal.IsInRole("sysadmin"));
      Assert.True(principal.IsInRole(Account.UserRole.SysAdmin));
      Assert.True(principal.IsSysAdmin);

      Assert.False(principal.BelongsToNoOrganizations);
      Assert.False(principal.BelongsToSingleOrganization);
      Assert.True(principal.BelongsToMultipleOrganizations);

      Assert.True(principal.IsMemberOf(atari));
      Assert.True(principal.IsMemberOf(nintendo));
      Assert.False(principal.IsMemberOf(other));

      Assert.True(principal.IsMemberOf(pong));
      Assert.True(principal.IsMemberOf(mario));
      Assert.False(principal.IsMemberOf(snakes));

      Assert.Equal([
        $"{UserClaim.SID}: 42",
        $"{UserClaim.Id}: 42",
        $"{UserClaim.Name}: John Doe",
        $"{UserClaim.Email}: john@example.com",
        $"{UserClaim.TimeZone}: Europe/Paris",
        $"{UserClaim.Locale}: en-US",
        $"{UserClaim.Role}: sysadmin",
        $"{UserClaim.Identity}: github:githubber",
        $"{UserClaim.Identity}: discord:discorder",
        $"{UserClaim.Organization}: 100",
        $"{UserClaim.Organization}: 200",
        $"{UserClaim.AuthenticatedOn}: {Clock.Now.ToIso8601()}",
      ], principal.Claims.Select(c => c.ToString()));

      // also verify what our claims principal looks like as a JWT...

      var jwtGenerator = new Crypto.JwtGenerator(Fake.SigningKey(), Clock);
      var jwt = jwtGenerator.Create(principal, out var token);
      Assert.LooksLikeJwt(jwt);

      var expectedNotBefore = Clock.Now.ToUnixTimeSeconds();
      var expectedExpires = expectedNotBefore + (24 * 60 * 60);

      Assert.Equal([
        "alg:HS256",
        "typ:JWT",
      ], token.Header.Select(h => $"{h.Key}:{h.Value}"));

      Assert.Equal([
        $"{UserClaim.SID}: 42",
        $"{UserClaim.Id}: 42",
        $"{UserClaim.Name}: John Doe",
        $"{UserClaim.Email}: john@example.com",
        $"{UserClaim.TimeZone}: Europe/Paris",
        $"{UserClaim.Locale}: en-US",
        $"{UserClaim.Role}: sysadmin",
        $"{UserClaim.Identity}: github:githubber",
        $"{UserClaim.Identity}: discord:discorder",
        $"{UserClaim.Organization}: 100",
        $"{UserClaim.Organization}: 200",
        $"{UserClaim.AuthenticatedOn}: {Clock.Now.ToIso8601()}",
        $"{UserClaim.NotBefore}: {expectedNotBefore}",
        $"{UserClaim.Expires}: {expectedExpires}",
        $"{UserClaim.Issuer}: https://void.dev",
        $"{UserClaim.Audience}: platform",
      ], token.Claims.Select(c => c.ToString()));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestEmptyUserPrincipal()
  {
    var identity = new ClaimsIdentity();
    var principal = UserPrincipal.From(identity, TestConfig.AuthenticationScheme);

    Assert.False(principal.IsLoggedIn);
    Assert.Equal([], principal.Claims.Select(c => c.ToString()));

    Assert.False(principal.IsInRole(Account.UserRole.SysAdmin));
    Assert.False(principal.IsSysAdmin);

    Assert.True(principal.BelongsToNoOrganizations);
    Assert.False(principal.BelongsToSingleOrganization);
    Assert.False(principal.BelongsToMultipleOrganizations);
  }

  //-----------------------------------------------------------------------------------------------
}