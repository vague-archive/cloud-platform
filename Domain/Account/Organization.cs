namespace Void.Platform.Domain;

public partial class Account
{
  //===============================================================================================
  // ORGANIZATION TYPES
  //===============================================================================================

  public record Organization
  {
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public Instant CreatedOn { get; set; }
    public Instant UpdatedOn { get; set; }
  }

  public const string OrganizationFields = @"
    organizations.id         as Id,
    organizations.name       as Name,
    organizations.slug       as Slug,
    organizations.created_on as CreatedOn,
    organizations.updated_on as UpdatedOn
  ";

  //===============================================================================================
  // GET ORGANIZATION METHODS
  //===============================================================================================

  public Organization? GetOrganization(long id)
  {
    return Db.QuerySingleOrDefault<Organization>(@$"
      SELECT {OrganizationFields}
      FROM organizations
      WHERE id = @Id
    ", new { Id = id });
  }

  public Organization? GetOrganization(string slug)
  {
    return Db.QuerySingleOrDefault<Organization>(@$"
      SELECT {OrganizationFields}
      FROM organizations
      WHERE slug = @Slug
    ", new { Slug = slug });
  }

  public Dictionary<long, Organization> GetOrganizations(IEnumerable<long> orgIds)
  {
    var orgs = Db.Query<Organization>(@$"
      SELECT {OrganizationFields}
        FROM organizations
       WHERE id IN @OrgIds
       ", new { OrgIds = orgIds });
    return orgs
      .GroupBy(org => org.Id)
      .ToDictionary(g => g.Key, g => g.First());
  }

  //===============================================================================================
  // ORGANIZATION ASSOCIATIONS
  //===============================================================================================

  public List<Member> GetOrganizationMembers(Organization org)
  {
    return GetOrganizationMembers(org.Id);
  }

  public List<Member> GetOrganizationMembers(long organizationId)
  {
    var members = Db.SplitQuery<User, Member, Member>(@"
      SELECT
        u.id,
        u.name,
        u.email,
        u.timezone,
        u.locale,
        u.disabled,
        u.created_on as CreatedOn,
        u.updated_on as UpdatedOn,
        m.organization_id as OrganizationId,
        m.user_id as UserId,
        m.created_on as CreatedOn,
        m.updated_on as UpdatedOn
      FROM members m
      INNER JOIN users u on u.id = m.user_id
      WHERE m.organization_id = @organizationId
   ORDER BY u.name, u.id
    ", new { organizationId }, (user, member) =>
    {
      member.User = user;
      return member;
    }, splitOn: "OrganizationId");
    var users = members.Select(m => m.User!);
    WithRoles(users);
    WithIdentities(users);
    return members;
  }

  //===============================================================================================
  // UPDATE ORGANIZATION COMMAND
  //===============================================================================================

  public class UpdateOrganizationCommand
  {
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";

    public class Validator : AbstractValidator<UpdateOrganizationCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Name)
          .NotEmpty().WithMessage(Validation.IsMissing)
          .MaximumLength(255).WithMessage(Validation.TooLong(255));

        RuleFor(cmd => cmd.Slug)
          .NotEmpty().WithMessage(Validation.IsMissing)
          .MaximumLength(255).WithMessage(Validation.TooLong(255));
      }
    }
  }

  public Result<Organization> UpdateOrganization(Organization org, string name, string slug)
  {
    return UpdateOrganization(org, new UpdateOrganizationCommand
    {
      Name = name,
      Slug = slug,
    });
  }

  public Result<Organization> UpdateOrganization(Organization org, UpdateOrganizationCommand cmd)
  {
    var result = new UpdateOrganizationCommand.Validator().Validate(cmd);
    if (!result.IsValid)
      return Validation.Fail(result);

    try
    {
      org.Name = cmd.Name;
      org.Slug = cmd.Slug;
      org.UpdatedOn = Now;

      var numRows = Db.Execute(@"
        UPDATE organizations
           SET name       = @Name,
               slug       = @Slug,
               updated_on = @UpdatedOn
         WHERE id = @Id
      ", org);
      RuntimeAssert.True(numRows == 1);
      return Result.Ok(org);
    }
    catch (MySqlConnector.MySqlException ex) when (ex.ErrorCode == MySqlConnector.MySqlErrorCode.DuplicateKeyEntry)
    {
      return Validation.Fail(nameof(cmd.Slug), "is already in use");
    }
  }

  //-----------------------------------------------------------------------------------------------
}