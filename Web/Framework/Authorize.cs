namespace Void.Platform.Web;

public static class Policy
{
  public const string SysAdmin = "Policy.SysAdmin";
  public const string OrgMember = "Policy.OrgMember";
  public const string GameMember = "Policy.GameMember";
  public const string TokenAuthenticated = "Policy.TokenAuthenticated";
  public const string CookieAuthenticated = "Policy.CookieAuthenticated";
}

public static class Authorization
{
  //-----------------------------------------------------------------------------------------------

  public static IServiceCollection AddVoidAuthorization(this IServiceCollection services, Config config)
  {
    services.AddAuthorization(options =>
    {
      options.AddPolicy(Policy.SysAdmin, policy => policy.RequireRole(Account.UserRole.SysAdmin));
      options.AddPolicy(Policy.OrgMember, policy => policy.AddRequirements(new OrgMemberRequirement()));
      options.AddPolicy(Policy.GameMember, policy => policy.AddRequirements(new GameMemberRequirement()));
      options.AddPolicy(Policy.TokenAuthenticated, policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(Config.TokenAuthenticationScheme));
      options.AddPolicy(Policy.CookieAuthenticated, policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(Config.CookieAuthenticationScheme));
    });
    services.AddSingleton<IAuthorizationHandler, OrgMemberRequirementHandler>();
    services.AddSingleton<IAuthorizationHandler, GameMemberRequirementHandler>();
    return services;
  }

  //-----------------------------------------------------------------------------------------------

  public static AuthorizationPolicyBuilder RequireRole(this AuthorizationPolicyBuilder policy, Account.UserRole role)
  {
    return policy.RequireClaim(UserClaim.Role, UserClaim.Value(role));
  }

  //-----------------------------------------------------------------------------------------------

  public class OrgMemberRequirement : IAuthorizationRequirement { }

  public class OrgMemberRequirementHandler : AuthorizationHandler<OrgMemberRequirement, Account.Organization>
  {
    public OrgMemberRequirementHandler() { }

    protected override Task HandleRequirementAsync(
      AuthorizationHandlerContext context,
      OrgMemberRequirement requirement,
      Account.Organization org)
    {
      var principal = context.User.Wrap();
      if (principal.IsSysAdmin || principal.IsMemberOf(org))
        context.Succeed(requirement);
      return Task.CompletedTask;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public class GameMemberRequirement : IAuthorizationRequirement { }

  public class GameMemberRequirementHandler : AuthorizationHandler<GameMemberRequirement, Account.Game>
  {
    public GameMemberRequirementHandler() { }

    protected override Task HandleRequirementAsync(
      AuthorizationHandlerContext context,
      GameMemberRequirement requirement,
      Account.Game game)
    {
      var principal = context.User.Wrap();
      if (principal.IsSysAdmin || principal.IsMemberOf(game))
        context.Succeed(requirement);
      return Task.CompletedTask;
    }
  }

  //-----------------------------------------------------------------------------------------------
}