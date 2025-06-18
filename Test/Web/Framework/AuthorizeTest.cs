namespace Void.Platform.Web;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public class PolicyTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestConstants()
  {
    Assert.Equal("Policy.SysAdmin", Policy.SysAdmin);
    Assert.Equal("Policy.OrgMember", Policy.OrgMember);
    Assert.Equal("Policy.GameMember", Policy.GameMember);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestOrgMemberRequirementMet()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var (context, handler) = BuildOrgMemberRequirementContext(org, new Claim[]
      {
        new Claim(UserClaim.Organization, UserClaim.Value(org))
      });
      await handler.HandleAsync(context);
      Assert.True(context.HasSucceeded);
      Assert.False(context.HasFailed);
    }
  }

  [Fact]
  public async Task TestOrgMemberRequirementMetForSysAdmin()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var (context, handler) = BuildOrgMemberRequirementContext(org, new Claim[]
      {
        new Claim(UserClaim.Role, UserClaim.Value(Account.UserRole.SysAdmin))
      });
      await handler.HandleAsync(context);
      Assert.True(context.HasSucceeded);
      Assert.False(context.HasFailed);
    }
  }

  [Fact]
  public async Task TestOrgMemberRequirementNotMet()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var (context, handler) = BuildOrgMemberRequirementContext(org, new Claim[] { });
      await handler.HandleAsync(context);
      Assert.False(context.HasSucceeded);
      Assert.False(context.HasFailed);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGameMemberRequirementMet()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var game = test.Factory.BuildGame(org);
      var (context, handler) = BuildGameMemberRequirementContext(game, new Claim[]
      {
        new Claim(UserClaim.Organization, UserClaim.Value(game))
      });
      await handler.HandleAsync(context);
      Assert.True(context.HasSucceeded);
      Assert.False(context.HasFailed);
    }
  }

  [Fact]
  public async Task TestGameMemberRequirementMetForSysAdmin()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var game = test.Factory.BuildGame(org);
      var (context, handler) = BuildGameMemberRequirementContext(game, new Claim[]
      {
        new Claim(UserClaim.Role, UserClaim.Value(Account.UserRole.SysAdmin))
      });
      await handler.HandleAsync(context);
      Assert.True(context.HasSucceeded);
      Assert.False(context.HasFailed);
    }
  }

  [Fact]
  public async Task TestGameMemberRequirementNotMet()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.BuildOrganization();
      var game = test.Factory.BuildGame(org);
      var (context, handler) = BuildGameMemberRequirementContext(game, new Claim[] { });
      await handler.HandleAsync(context);
      Assert.False(context.HasSucceeded);
      Assert.False(context.HasFailed);
    }
  }

  //-----------------------------------------------------------------------------------------------

  private (AuthorizationHandlerContext, Authorization.OrgMemberRequirementHandler) BuildOrgMemberRequirementContext(Account.Organization org, Claim[] claims)
  {
    var requirement = new Authorization.OrgMemberRequirement();
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
    var context = new AuthorizationHandlerContext(new[] { requirement }, user, org);
    var handler = new Authorization.OrgMemberRequirementHandler();
    return (context, handler);
  }

  private (AuthorizationHandlerContext, Authorization.GameMemberRequirementHandler) BuildGameMemberRequirementContext(Account.Game game, Claim[] claims)
  {
    var requirement = new Authorization.GameMemberRequirement();
    var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
    var context = new AuthorizationHandlerContext(new[] { requirement }, user, game);
    var handler = new Authorization.GameMemberRequirementHandler();
    return (context, handler);
  }

  //-----------------------------------------------------------------------------------------------
}