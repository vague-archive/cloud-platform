namespace Void.Platform.Domain;

public partial class Account
{
  //===============================================================================================
  // USER TYPES
  //===============================================================================================

  public enum UserRole
  {
    SysAdmin,
  }

  //-----------------------------------------------------------------------------------------------

  public record User
  {
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string TimeZone { get; set; }
    public required string Locale { get; set; }
    public string? Password { get; set; }
    public bool Disabled { get; set; }
    public Instant CreatedOn { get; set; }
    public Instant UpdatedOn { get; set; }

    [DatabaseIgnore] public List<UserRole>? Roles { get; set; }
    [DatabaseIgnore] public List<Identity>? Identities { get; set; }
    [DatabaseIgnore] public List<Organization>? Organizations { get; set; }

    public bool BelongsTo(Organization org)
    {
      return Organizations!.Any(o => o.Id == org.Id);
    }
  }

  public static string UserFields = @"
    users.id         as Id,
    users.name       as Name,
    users.email      as Email,
    users.timezone   as TimeZone,
    users.locale     as Locale,
    users.disabled   as Disabled,
    users.created_on as CreatedOn,
    users.updated_on as UpdatedOn
  ";

  //-----------------------------------------------------------------------------------------------

  public enum IdentityProvider
  {
    Google,
    GitHub,
    Microsoft,
    Discord,
  }

  public record Identity
  {
    public long Id { get; set; }
    public long UserId { get; set; }
    public IdentityProvider Provider { get; set; }
    public required string Identifier { get; set; }
    public required string UserName { get; set; }
    public Instant CreatedOn { get; set; }
    public Instant UpdatedOn { get; set; }

    public override string ToString()
    {
      return $"{Provider.ToString().ToLower()}:{UserName}";
    }
  }

  //-----------------------------------------------------------------------------------------------

  public record Member
  {
    public long OrganizationId { get; set; }
    public long UserId { get; set; }
    public Instant CreatedOn { get; set; }
    public Instant UpdatedOn { get; set; }

    [DatabaseIgnore] public User? User { get; set; }
  }

  //===============================================================================================
  // GET USER METHODS
  //===============================================================================================

  public User? GetUserById(long id)
  {
    return Db.QuerySingleOrDefault<User>(@$"
      SELECT {UserFields}
      FROM users
      WHERE id = @Id
    ", new { Id = id });
  }

  public User? GetUserByEmail(string email)
  {
    return Db.QuerySingleOrDefault<User>(@$"
      SELECT {UserFields}
      FROM users
      WHERE email = @Email
    ", new { Email = email });
  }

  public User? GetUserByIdentity(IdentityProvider provider, string identifier)
  {
    var userId = Db.QuerySingleOrDefault<long?>(@"
      SELECT user_id
        FROM identities
       WHERE provider = @Provider
         AND identifier = @Identifier
    ", new
    {
      Provider = provider,
      Identifier = identifier
    });
    if (userId is null)
      return null;
    else
      return GetUserById(userId.Value);
  }

  public User? GetUserByAccessToken(string tokenValue)
  {
    var token = GetAccessToken(tokenValue);
    if (token is not null && token.UserId is long userId)
      return GetUserById(userId);
    else
      return null;
  }

  public User WithPassword(User user)
  {
    user.Password = Db.QuerySingleOrDefault<string>(@"
      SELECT password
        FROM users
       WHERE id = @Id
    ", new { Id = user.Id });
    return user;
  }

  public Dictionary<long, User> GetUsers(IEnumerable<long> userIds)
  {
    var users = Db.Query<User>(@$"
      SELECT {UserFields}
        FROM users
       WHERE id IN @UserIds
       ", new { UserIds = userIds });
    return users
      .GroupBy(user => user.Id)
      .ToDictionary(g => g.Key, g => g.First());
  }

  //===============================================================================================
  // USER ROLES ASSOCIATION
  //===============================================================================================

  public void WithRoles(User user)
  {
    user.Roles = GetUserRoles(user.Id);
  }

  public void WithRoles(IEnumerable<User> users)
  {
    var roles = GetUserRoles(users.Select(u => u.Id));
    foreach (var user in users)
    {
      if (roles.TryGetValue(user.Id, out var userRoles))
        user.Roles = userRoles;
      else
        user.Roles = [];
    }
  }

  public List<UserRole> GetUserRoles(long userId)
  {
    return Db.Query<string>(@"
      SELECT role
        FROM user_roles
       WHERE user_id = @UserId
    ORDER BY role
       ", new { UserId = userId }).ToEnumList<UserRole>();
  }

  public Dictionary<long, List<UserRole>> GetUserRoles(IEnumerable<long> userIds)
  {
    var records = Db.Query<(long, string)>(@"
      SELECT user_id, role
        FROM user_roles
       WHERE user_id IN @UserIds
    ORDER BY role
       ", new { UserIds = userIds });
    return records
      .GroupBy(record => record.Item1)
      .ToDictionary(
        group => group.Key,
        group => group.Select(user => user.Item2.ToEnum<UserRole>()).AsList()
      );
  }

  //===============================================================================================
  // USER IDENTITIES ASSOCIATION
  //===============================================================================================

  public void WithIdentities(User user)
  {
    user.Identities = GetUserIdentities(user.Id);
  }

  public void WithIdentities(IEnumerable<User> users)
  {
    var identities = GetUserIdentities(users.Select(u => u.Id));
    foreach (var user in users)
    {
      if (identities.TryGetValue(user.Id, out var userIdentities))
        user.Identities = userIdentities;
      else
        user.Identities = [];
    }
  }

  public List<Identity> GetUserIdentities(long userId)
  {
    return Db.Query<Identity>(@"
      SELECT id,
             user_id as UserId,
             provider,
             identifier,
             username,
             created_on as CreatedOn,
             updated_on as UpdatedOn
        FROM identities
       WHERE user_id = @UserId
    ORDER BY username, provider
    ", new { UserId = userId });
  }

  public Dictionary<long, List<Identity>> GetUserIdentities(IEnumerable<long> userIds)
  {
    var identities = Db.Query<Identity>(@"
      SELECT id,
             user_id as UserId,
             provider,
             identifier,
             username,
             created_on as CreatedOn,
             updated_on as UpdatedOn
        FROM identities
       WHERE user_id IN @UserIds
    ORDER BY username, provider
    ", new { UserIds = userIds });
    return identities
      .GroupBy(identity => identity.UserId)
      .ToDictionary(
        group => group.Key,
        group => group.ToList()
      );
  }

  //===============================================================================================
  // USER ORGANIZATIONS ASSOCIATION
  //===============================================================================================

  public User WithOrganizations(User user)
  {
    user.Organizations ??= GetUserOrganizations(user.Id);
    return user;
  }

  public List<Organization> GetUserOrganizations(User user)
  {
    return GetUserOrganizations(user.Id);
  }

  public List<Organization> GetUserOrganizations(long userId)
  {
    return Db.Query<Organization>(@"
      SELECT o.id,
             o.name,
             o.slug,
             o.created_on as CreatedOn,
             o.updated_on as UpdatedOn
        FROM members m JOIN organizations o ON o.id = m.organization_id
       WHERE m.user_id = @UserId
    ORDER BY o.name
    ", new { UserId = userId });
  }

  //===============================================================================================
  // UPDATE PROFILE COMMAND
  //===============================================================================================

  public class UpdateProfileCommand
  {
    public string Name { get; init; } = "";
    public string TimeZone { get; init; } = "";
    public string Locale { get; init; } = "";

    public class Validator : AbstractValidator<UpdateProfileCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Name)
          .NotEmpty().WithMessage(Validation.IsMissing)
          .MaximumLength(255).WithMessage(Validation.TooLong(255));

        RuleFor(cmd => cmd.TimeZone)
          .NotEmpty().WithMessage(Validation.IsMissing);

        RuleFor(cmd => cmd.TimeZone)
          .Must(value => International.TimeZoneIds.Contains(value))
          .When(cmd => !string.IsNullOrWhiteSpace(cmd.TimeZone))
          .WithMessage("is not a valid timezone");

        RuleFor(cmd => cmd.Locale)
          .NotEmpty().WithMessage(Validation.IsMissing);

        RuleFor(cmd => cmd.Locale)
          .Must(value => International.Locales.Contains(value))
          .When(cmd => !string.IsNullOrWhiteSpace(cmd.Locale))
          .WithMessage("is not a valid locale");
      }
    }
  }

  public Result<User> UpdateProfile(User user, string name, string timezone, string locale)
  {
    return UpdateProfile(user, new UpdateProfileCommand
    {
      Name = name,
      TimeZone = timezone,
      Locale = locale
    });
  }

  public Result<User> UpdateProfile(User user, UpdateProfileCommand cmd)
  {
    var result = new UpdateProfileCommand.Validator().Validate(cmd);
    if (!result.IsValid)
      return Validation.Fail(result);

    user.Name = cmd.Name;
    user.TimeZone = cmd.TimeZone;
    user.Locale = cmd.Locale;
    user.UpdatedOn = Now;

    var numRows = Db.Execute(@"
      UPDATE users
         SET name = @Name,
             timezone = @TimeZone,
             locale = @Locale,
             updated_on = @UpdatedOn
       WHERE id = @Id
    ", user);
    RuntimeAssert.True(numRows == 1);

    return Result.Ok(user);
  }

  //===============================================================================================
  // LOGIN COMMAND
  //===============================================================================================

  public class LoginCommand
  {
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";

    public class Validator : AbstractValidator<LoginCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Email)
          .NotEmpty().WithMessage(Validation.IsMissing)
          .EmailAddress().WithMessage(Validation.IsInvalid);

        RuleFor(cmd => cmd.Password)
          .NotEmpty().WithMessage(Validation.IsMissing);
      }
    }
  }

  public Result<AuthenticatedUser> Login(string email, string password)
  {
    return Login(new LoginCommand
    {
      Email = email,
      Password = password,
    });
  }

  public Result<AuthenticatedUser> Login(LoginCommand cmd)
  {
    var result = new LoginCommand.Validator().Validate(cmd);
    if (!result.IsValid)
      return Validation.Fail(result);

    var user = GetUserByEmail(cmd.Email);
    if (user is null)
      return Validation.Fail(nameof(cmd.Email), Validation.NotFound);

    if (user.Disabled)
      return Validation.Fail(nameof(cmd.Email), Validation.IsDisabled);

    user = WithPassword(user);

    if (user.Password == null || !PasswordHasher.Verify(cmd.Password, user.Password))
      return Validation.Fail(nameof(cmd.Password), Validation.IsInvalid);

    var authenticatedUser = AsAuthenticatedUser(user);
    if (authenticatedUser is null)
      return Validation.Fail(nameof(cmd.Email), Validation.NotFound);

    return Result.Ok(authenticatedUser);
  }

  //===============================================================================================
  // AUTHENTICATED USER
  //===============================================================================================

  public record AuthenticatedUser : User
  {
    public required Instant AuthenticatedOn { get; set; }
  }

  public AuthenticatedUser? GetAuthenticatedUser(IdentityProvider provider, string identifier)
  {
    return AsAuthenticatedUser(GetUserByIdentity(provider, identifier));
  }

  public AuthenticatedUser? GetAuthenticatedUser(long id)
  {
    return AsAuthenticatedUser(GetUserById(id));
  }

  public AuthenticatedUser? GetAuthenticatedUser(string tokenValue)
  {
    return AsAuthenticatedUser(GetUserByAccessToken(tokenValue));
  }

  private AuthenticatedUser? AsAuthenticatedUser(User? user)
  {
    if (user is null || user.Disabled)
      return null;

    WithRoles(user);
    WithIdentities(user);
    WithOrganizations(user);

    return new AuthenticatedUser
    {
      Id = user.Id,
      Name = user.Name,
      Email = user.Email,
      TimeZone = user.TimeZone,
      Locale = user.Locale,
      Roles = user.Roles,
      Disabled = user.Disabled,
      CreatedOn = user.CreatedOn,
      UpdatedOn = user.UpdatedOn,
      Identities = user.Identities,
      Organizations = user.Organizations,
      AuthenticatedOn = Now,
    };
  }

  //===============================================================================================
  // DISCONNECT MEMBER
  //===============================================================================================

  public void DisconnectMember(Organization org, User user)
  {
    Db.Execute(@"
      DELETE
        FROM members
       WHERE organization_id = @OrganizationId
         AND user_id = @UserId
    ", new
    {
      OrganizationId = org.Id,
      UserId = user.Id,
    });
  }

  //-----------------------------------------------------------------------------------------------
}