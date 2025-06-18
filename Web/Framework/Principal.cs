namespace Void.Platform.Web;

using System.Security.Claims;
using System.Security.Principal;

//=================================================================================================
// USER CLAIM NAMES
//=================================================================================================

public static class UserClaim
{
  public static readonly string SID = ClaimTypes.NameIdentifier; // subject ID known value required by ASP framework
  public static readonly string Id = "id";
  public static readonly string Name = "name";
  public static readonly string Email = "email";
  public static readonly string TimeZone = "timezone";
  public static readonly string Locale = "locale";
  public static readonly string Role = "role";
  public static readonly string Identity = "identity";
  public static readonly string Organization = "organization";
  public static readonly string AuthenticatedOn = "authenticatedOn";
  public static readonly string NotBefore = "nbf";
  public static readonly string Expires = "exp";
  public static readonly string Issuer = "iss";
  public static readonly string Audience = "aud";

  public static string Value(Account.UserRole role) { return role.ToString().ToLower(); }
  public static string Value(Account.Identity identity) { return identity.ToString(); }
  public static string Value(Account.Organization org) { return org.Id.ToString(); }
  public static string Value(Account.Game game) { return game.OrganizationId.ToString(); }
  public static string Value(Instant instant) { return instant.ToIso8601(); }
}

//=================================================================================================
// CUSTOM USER CLAIMS PRINCIPAL
//=================================================================================================

public class UserPrincipal : ClaimsPrincipal
{
  //-----------------------------------------------------------------------------------------------

  public UserPrincipal(IPrincipal principal) : base(principal)
  {
  }

  //-----------------------------------------------------------------------------------------------

  public bool IsLoggedIn
  {
    get
    {
      return Identity?.IsAuthenticated ?? false;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public override bool IsInRole(string role)
  {
    return HasClaim(UserClaim.Role, role.ToLower());
  }

  public bool IsInRole(Account.UserRole role)
  {
    return HasClaim(UserClaim.Role, UserClaim.Value(role));
  }

  public bool IsMemberOf(Account.Organization org)
  {
    return HasClaim(UserClaim.Organization, UserClaim.Value(org));
  }

  public bool IsMemberOf(Account.Game game)
  {
    return HasClaim(UserClaim.Organization, UserClaim.Value(game));
  }

  public bool IsSysAdmin
  {
    get
    {
      return IsInRole(Account.UserRole.SysAdmin);
    }
  }

  //-----------------------------------------------------------------------------------------------

  private long? id;
  public long Id
  {
    get
    {
      return id ??= GetLong(UserClaim.Id);
    }
  }

  private string? name;
  public string Name
  {
    get
    {
      return name ??= GetString(UserClaim.Name);
    }
  }

  private string? email;
  public string Email
  {
    get
    {
      return email ??= GetString(UserClaim.Email);
    }
  }

  private string? timezone;
  public string TimeZone
  {
    get
    {
      return timezone ??= GetString(UserClaim.TimeZone);
    }
  }

  private string? locale;
  public string Locale
  {
    get
    {
      return locale ??= GetString(UserClaim.Locale);
    }
  }

  private Instant? authenticatedOn;
  public Instant AuthenticatedOn
  {
    get
    {
      return authenticatedOn ??= GetInstant(UserClaim.AuthenticatedOn);
    }
  }

  private List<Account.UserRole>? roles;
  public List<Account.UserRole> Roles
  {
    get
    {
      return roles ??= FindAll(UserClaim.Role).Select(c => c.Value.ToEnum<Account.UserRole>()).ToList();
    }
  }

  private List<string>? identities;
  public new List<string> Identities
  {
    get
    {
      return identities ??= FindAll(UserClaim.Identity).Select(c => c.Value).ToList();
    }
  }

  private List<long>? organizations;
  public List<long> Organizations
  {
    get
    {
      return organizations ??= FindAll(UserClaim.Organization).Select(c => Convert.ToInt64(c.Value)).ToList();
    }
  }

  public bool BelongsToNoOrganizations
  {
    get
    {
      return Organizations.Count == 0;
    }
  }

  public bool BelongsToSingleOrganization
  {
    get
    {
      return Organizations.Count == 1;
    }
  }

  public bool BelongsToMultipleOrganizations
  {
    get
    {
      return Organizations.Count > 1;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static UserPrincipal From(Account.AuthenticatedUser user, string scheme)
  {
    var userId = user.Id.ToString();
    var claims = new List<Claim>
    {
      new Claim(UserClaim.SID, userId), // always required for ASP.NET auth framework
      new Claim(UserClaim.Id, userId),
      new Claim(UserClaim.Name, user.Name),
      new Claim(UserClaim.Email, user.Email),
      new Claim(UserClaim.TimeZone, user.TimeZone),
      new Claim(UserClaim.Locale, user.Locale),
    };

    var roles = RuntimeAssert.Present(user.Roles);
    var identities = RuntimeAssert.Present(user.Identities);
    var organizations = RuntimeAssert.Present(user.Organizations);

    foreach (var role in roles)
      claims.Add(new Claim(UserClaim.Role, UserClaim.Value(role)));

    foreach (var identity in identities)
      claims.Add(new Claim(UserClaim.Identity, UserClaim.Value(identity)));

    foreach (var org in organizations)
      claims.Add(new Claim(UserClaim.Organization, UserClaim.Value(org)));

    claims.Add(new Claim(UserClaim.AuthenticatedOn, UserClaim.Value(user.AuthenticatedOn)));

    return From(claims, scheme);
  }

  public static UserPrincipal From(List<Claim> claims, string scheme)
  {
    return From(new ClaimsIdentity(claims, scheme), scheme);
  }

  public static UserPrincipal From(ClaimsIdentity identity, string scheme)
  {
    var principal = new ClaimsPrincipal(identity);
    return new UserPrincipal(principal);
  }

  //-----------------------------------------------------------------------------------------------

  private string GetString(string name)
  {
    var value = FindFirst(name)?.Value;
    RuntimeAssert.Present(value, $"missing {name}");
    return value;
  }

  private long GetLong(string name)
  {
    return long.Parse(GetString(name));
  }

  private Instant GetInstant(string name)
  {
    return GetString(name).FromIso8601();
  }

  //-----------------------------------------------------------------------------------------------
}

//=================================================================================================
// CUSTOM TRANSFORMER
//=================================================================================================

public class UserPrincipalTransformer : IClaimsTransformation
{
  public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    return Task.FromResult(principal.Wrap() as ClaimsPrincipal);
  }
}

//=================================================================================================
// CUSTOM EXTENSION METHODS
//=================================================================================================

public static class UserPrincipalExtensions
{
  public static UserPrincipal Wrap(this ClaimsPrincipal principal)
  {
    if (principal is UserPrincipal)
    {
      return (UserPrincipal) principal;
    }
    else
    {
      return new UserPrincipal(principal);
    }
  }
}