namespace Void.Platform.Fixture;

public class FixtureFactory
{
  public const string EncryptKey = "x+eLvtCTh0dXznoyLt3gOGtGZWrvBmlz0u1Qqd1qmMU="; // == Web.Config.DefaultEncryptKey
  public const string SigningKey = "Zqlkii6IEpmFILaNd0ZZGFfbLqGZgFrJopnnjPttGyw="; // == Web.Config.DefaultSigningKey

  public readonly Database Db;
  public readonly IClock Clock;
  public readonly Fake Fake;
  public readonly Crypto.Encryptor Encryptor;
  public readonly Crypto.PasswordHasher PasswordHasher;

  public FixtureFactory(Database db, IClock? clock = null)
  {
    Db = db;
    Clock = clock ?? Lib.Clock.System;
    Fake = new Fake();
    Encryptor = new Crypto.Encryptor(EncryptKey);
    PasswordHasher = new Crypto.PasswordHasher();
  }

  public Instant Now
  {
    get
    {
      return Clock.Now;
    }
  }

  static int nid = 2000;
  static int NextId()
  {
    return Interlocked.Increment(ref nid);
  }

  //===============================================================================================
  // ORGANIZATIONS
  //===============================================================================================

  public Account.Organization BuildOrganization(
    string id,
    string? name = null,
    string? slug = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return BuildOrganization(
      id: Identify(id),
      name: name,
      slug: slug,
      createdOn: createdOn,
      updatedOn: updatedOn
    );
  }

  public Account.Organization BuildOrganization(
    long? id = null,
    string? name = null,
    string? slug = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    name = name ?? Fake.CompanyName();
    slug = slug ?? Format.Slugify(name);
    return new Account.Organization()
    {
      Id = id ?? NextId(),
      Name = name,
      Slug = slug,
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
    };
  }

  public Account.Organization CreateOrganization(
    string id,
    string? name = null,
    string? slug = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return CreateOrganization(
      id: Identify(id),
      name: name,
      slug: slug,
      createdOn: createdOn,
      updatedOn: updatedOn
    );
  }

  public Account.Organization CreateOrganization(
    long? id = null,
    string? name = null,
    string? slug = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    var org = BuildOrganization(id, name, slug, createdOn, updatedOn);
    Db.Execute(@"
      INSERT INTO organizations (
        id,
        name,
        slug,
        created_on,
        updated_on
      )
      VALUES (
        @Id,
        @Name,
        @Slug,
        @CreatedOn,
        @UpdatedOn
      )
    ", org);
    return org;
  }

  public Account.Organization LoadOrganization(string id)
  {
    return LoadOrganization(Identify(id));
  }

  public Account.Organization LoadOrganization(long id)
  {
    return Db.QuerySingle<Account.Organization>(@"
      SELECT id,
             name,
             slug,
             created_on as CreatedOn,
             updated_on as UpdatedOn
      FROM organizations
      WHERE id = @Id
    ", new { Id = id });
  }

  //===============================================================================================
  // USERS
  //===============================================================================================

  public Account.User BuildUser(
    string id,
    string? name = null,
    string? email = null,
    string? timezone = null,
    string? locale = null,
    string? password = null,
    bool? disabled = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return BuildUser(
      id: Identify(id),
      name: name,
      email: email,
      timezone: timezone,
      locale: locale,
      password: password,
      disabled: disabled,
      createdOn: createdOn,
      updatedOn: updatedOn
    );
  }

  public Account.User BuildUser(
    long? id = null,
    string? name = null,
    string? email = null,
    string? timezone = null,
    string? locale = null,
    string? password = null,
    bool? disabled = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return new Account.User()
    {
      Id = id ?? NextId(),
      Name = name ?? Fake.FullName(),
      Email = email ?? Fake.Email(),
      TimeZone = timezone ?? Fake.TimeZone(),
      Locale = locale ?? Fake.Locale(),
      Password = PasswordHasher.Hash(password ?? "password"),
      Disabled = disabled ?? false,
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
    };
  }

  public Account.User CreateUser(
    string id,
    string? name = null,
    string? email = null,
    string? timezone = null,
    string? locale = null,
    string? password = null,
    bool? disabled = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return CreateUser(
      id: Identify(id),
      name: name,
      email: email,
      timezone: timezone,
      locale: locale,
      password: password,
      disabled: disabled,
      createdOn: createdOn,
      updatedOn: updatedOn
    );
  }

  public Account.User CreateUser(
    long? id = null,
    string? name = null,
    string? email = null,
    string? timezone = null,
    string? locale = null,
    string? password = null,
    bool? disabled = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    var user = BuildUser(id, name, email, timezone, locale, password, disabled, createdOn, updatedOn);
    Db.Execute(@"
      INSERT INTO users (
        id,
        name,
        email,
        timezone,
        locale,
        password,
        disabled,
        created_on,
        updated_on
      )
      VALUES (
        @Id,
        @Name,
        @Email,
        @Timezone,
        @Locale,
        @Password,
        @Disabled,
        @CreatedOn,
        @UpdatedOn
      )
    ", user);
    return user;
  }

  public Account.User LoadUser(string id)
  {
    return LoadUser(Identify(id));
  }

  public Account.User LoadUser(long id)
  {
    return Db.QuerySingle<Account.User>(@"
      SELECT id,
             name,
             email,
             timezone,
             locale,
             password,
             disabled,
             created_on as CreatedOn,
             updated_on as UpdatedOn
      FROM users
      WHERE id = @Id
    ", new { Id = id });
  }

  public void DisableUser(long userId, bool disabled = true)
  {
    Db.Execute(@"
      UPDATE users
         SET disabled = @Disabled
       WHERE id = @Id
    ", new
    {
      Id = userId,
      Disabled = disabled,
    });
  }

  //===============================================================================================
  // AUTHENTICATED USER DETAILS
  //===============================================================================================

  public Account.AuthenticatedUser BuildAuthenticatedUser(
    Account.User user,
    List<Account.UserRole>? roles = null,
    List<Account.Identity>? identities = null,
    List<Account.Organization>? organizations = null,
    Instant? authenticatedOn = null
  )
  {
    return new Account.AuthenticatedUser
    {
      Id = user.Id,
      Name = user.Name,
      Email = user.Email,
      TimeZone = user.TimeZone,
      Locale = user.Locale,
      Roles = roles,
      Identities = identities,
      Organizations = organizations,
      AuthenticatedOn = Timestamp(authenticatedOn),
    };
  }

  //===============================================================================================
  // USER ROLES
  //===============================================================================================

  public Account.UserRole CreateUserRole(
    Account.User user,
    Account.UserRole role,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    Db.Execute(@"
      INSERT INTO user_roles (
        user_id,
        role,
        created_on,
        updated_on
      )
      VALUES (
        @Id,
        @Role,
        @CreatedOn,
        @UpdatedOn
      )
    ", new
    {
      Id = user.Id,
      Role = role,
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
    });
    return role;
  }

  //===============================================================================================
  // IDENTITIES
  //===============================================================================================

  public Account.Identity BuildIdentity(
    Account.User user,
    string id,
    Account.IdentityProvider? provider = null,
    string? identifier = null,
    string? username = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return BuildIdentity(
      user: user,
      id: Identify(id),
      provider: provider,
      identifier: identifier,
      username: username
    );
  }

  public Account.Identity BuildIdentity(
    Account.User user,
    long? id = null,
    Account.IdentityProvider? provider = null,
    string? identifier = null,
    string? username = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return new Account.Identity()
    {
      Id = id ?? NextId(),
      UserId = user.Id,
      Provider = provider ?? Fake.IdentityProvider(),
      Identifier = identifier ?? Fake.Identifier(),
      UserName = username ?? Fake.UserName(),
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
    };
  }

  public Account.Identity CreateIdentity(
    Account.User user,
    string id,
    Account.IdentityProvider? provider = null,
    string? identifier = null,
    string? username = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return CreateIdentity(
      user: user,
      id: Identify(id),
      provider: provider,
      identifier: identifier,
      username: username,
      createdOn: createdOn,
      updatedOn: updatedOn
    );
  }

  public Account.Identity CreateIdentity(
    Account.User user,
    long? id = null,
    Account.IdentityProvider? provider = null,
    string? identifier = null,
    string? username = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    var identity = BuildIdentity(user, id, provider, identifier, username, createdOn, updatedOn);
    Db.Execute(@"
      INSERT INTO identities (
        id,
        user_id,
        provider,
        identifier,
        username,
        created_on,
        updated_on
      )
      VALUES (
        @Id,
        @UserId,
        @Provider,
        @Identifier,
        @UserName,
        @CreatedOn,
        @UpdatedOn
      )
    ", identity);
    return identity;
  }

  public Account.Identity LoadIdentity(string id)
  {
    return LoadIdentity(Identify(id));
  }

  public Account.Identity LoadIdentity(long id)
  {
    return Db.QuerySingle<Account.Identity>(@"
      SELECT id,
             user_id as UserId,
             provider,
             identifier,
             username,
             created_on as CreatedOn,
             updated_on as UpdatedOn
      FROM identities
      WHERE id = @Id
    ", new { Id = id });
  }

  //===============================================================================================
  // MEMBERS
  //===============================================================================================

  public Account.Member BuildMember(
    Account.Organization org,
    Account.User user,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return new Account.Member()
    {
      OrganizationId = org.Id,
      UserId = user.Id,
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
    };
  }

  public Account.Member CreateMember(
    Account.Organization org,
    Account.User user,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    var member = BuildMember(org, user, createdOn, updatedOn);
    Db.Execute(@"
      INSERT INTO members (
        organization_id,
        user_id,
        created_on,
        updated_on
      )
      VALUES (
        @OrganizationId,
        @UserId,
        @CreatedOn,
        @UpdatedOn
      )
    ", member);
    return member;
  }

  public Account.Member LoadMember(string organizationId, string userId)
  {
    return LoadMember(Identify(organizationId), Identify(userId));
  }

  public Account.Member LoadMember(long organizationId, long userId)
  {
    return Db.QuerySingle<Account.Member>(@"
      SELECT organization_id as OrganizationId,
             user_id as UserId,
             created_on as CreatedOn,
             updated_on as UpdatedOn
      FROM members
      WHERE organization_id = @OrganizationId
        AND user_id = @UserId
    ", new
    {
      OrganizationId = organizationId,
      UserId = userId
    });
  }

  //===============================================================================================
  // TOKENS
  //===============================================================================================

  public Account.Token BuildToken(
    string id,
    Account.TokenType? type = null,
    string? value = null,
    Account.User? user = null,
    Account.Organization? org = null,
    string? sentTo = null,
    bool isSpent = false,
    Instant? expiresOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return BuildToken(
      id: Identify(id),
      type: type,
      value: value,
      user: user,
      org: org,
      sentTo: sentTo,
      isSpent: isSpent,
      expiresOn: expiresOn,
      createdOn: createdOn,
      updatedOn: updatedOn
    );
  }

  public Account.Token BuildToken(
    long? id = null,
    Account.TokenType? type = null,
    string? value = null,
    Account.User? user = null,
    Account.Organization? org = null,
    string? sentTo = null,
    bool isSpent = false,
    Instant? expiresOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    value = Crypto.GenerateToken(value);
    var digest = Crypto.HashToken(value);
    var tail = Account.TokenTail(value);
    return new Account.Token()
    {
      Id = id ?? NextId(),
      Type = type ?? Account.TokenType.Access,
      Value = value,
      Digest = digest,
      Tail = tail,
      UserId = user?.Id,
      OrganizationId = org?.Id,
      SentTo = sentTo,
      IsSpent = isSpent,
      ExpiresOn = expiresOn,
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
    };
  }

  public Account.Token CreateToken(
    string id,
    Account.TokenType? type = null,
    string value = "token",
    Account.User? user = null,
    Account.Organization? org = null,
    string? sentTo = null,
    bool isSpent = false,
    Instant? expiresOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return CreateToken(
      id: Identify(id),
      type: type,
      value: value,
      user: user,
      org: org,
      sentTo: sentTo,
      isSpent: isSpent,
      expiresOn: expiresOn,
      createdOn: createdOn,
      updatedOn: updatedOn
    );
  }

  public Account.Token CreateToken(
    long? id = null,
    Account.TokenType? type = null,
    string? value = null,
    Account.User? user = null,
    Account.Organization? org = null,
    string? sentTo = null,
    bool isSpent = false,
    Instant? expiresOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    var token = BuildToken(id, type, value, user, org, sentTo, isSpent, expiresOn, createdOn, updatedOn);
    Db.Execute(@"
      INSERT INTO tokens (
        id,
        type,
        digest,
        tail,
        user_id,
        organization_id,
        sent_to,
        is_spent,
        expires_on,
        created_on,
        updated_on
      )
      VALUES (
        @Id,
        @Type,
        @Digest,
        @Tail,
        @UserId,
        @OrganizationId,
        @SentTo,
        @IsSpent,
        @ExpiresOn,
        @CreatedOn,
        @UpdatedOn
      )
    ", token);
    return token;
  }

  //===============================================================================================
  // GAMES
  //===============================================================================================

  public Account.Game LoadGame(string id)
  {
    return LoadGame(Identify(id));
  }

  public Account.Game LoadGame(long id)
  {
    return Db.QuerySingle<Account.Game>(@"
      SELECT
        games.id              as Id,
        games.organization_id as OrganizationId,
        games.purpose         as Purpose,
        games.name            as Name,
        games.slug            as Slug,
        games.description     as Description,
        games.archived        as IsArchived,
        games.archived_on     as ArchivedOn,
        games.created_on      as CreatedOn,
        games.updated_on      as UpdatedOn
      FROM games
      WHERE id = @Id
    ", new { Id = id });
  }

  public Account.Game LoadGame(Account.Organization org, string id)
  {
    return LoadGame(org, Identify(id));
  }

  public Account.Game LoadGame(Account.Organization org, long id)
  {
    var game = LoadGame(id);
    RuntimeAssert.Equal(org.Id, game.OrganizationId);
    game.Organization = org;
    return game;
  }

  public Account.Game BuildGame(
    Account.Organization org,
    string id,
    Account.GamePurpose? purpose = null,
    string? name = null,
    string? slug = null,
    string? description = null,
    bool isArchived = false,
    Instant? archivedOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return BuildGame(
      org,
      Identify(id),
      purpose,
      name,
      slug,
      description,
      isArchived,
      archivedOn,
      createdOn,
      updatedOn
    );
  }

  public Account.Game BuildGame(
    Account.Organization org,
    long? id = null,
    Account.GamePurpose? purpose = null,
    string? name = null,
    string? slug = null,
    string? description = null,
    bool isArchived = false,
    Instant? archivedOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    name = name ?? Fake.GameName();
    slug = slug ?? Format.Slugify(name);
    isArchived = isArchived || archivedOn is not null;
    return new Account.Game()
    {
      Id = id ?? NextId(),
      OrganizationId = org.Id,
      Organization = org,
      Purpose = purpose ?? Account.GamePurpose.Game,
      Name = name,
      Slug = slug,
      Description = description ?? Fake.Description(),
      IsArchived = isArchived,
      ArchivedOn = isArchived ? Timestamp(archivedOn ?? Fake.RecentDateUtc()) : null,
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
    };
  }

  public Account.Game CreateGame(
    Account.Organization org,
    string id,
    Account.GamePurpose? purpose = null,
    string? name = null,
    string? slug = null,
    string? description = null,
    bool isArchived = false,
    Instant? archivedOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return CreateGame(
      org,
      Identify(id),
      purpose,
      name,
      slug,
      description,
      isArchived,
      archivedOn,
      createdOn,
      updatedOn
    );
  }

  public Account.Game CreateGame(
    Account.Organization org,
    long? id = null,
    Account.GamePurpose? purpose = null,
    string? name = null,
    string? slug = null,
    string? description = null,
    bool isArchived = false,
    Instant? archivedOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    var game = BuildGame(org, id, purpose, name, slug, description, isArchived, archivedOn, createdOn, updatedOn);
    Db.Execute(@"
      INSERT INTO games (
        id,
        organization_id,
        purpose,
        name,
        slug,
        description,
        archived,
        archived_on,
        created_on,
        updated_on
      )
      VALUES (
        @Id,
        @OrganizationId,
        @Purpose,
        @Name,
        @Slug,
        @Description,
        @IsArchived,
        @ArchivedOn,
        @CreatedOn,
        @UpdatedOn
      )
    ", game);
    return game;
  }

  //===============================================================================================
  // BRANCHES
  //===============================================================================================

  public Share.Branch LoadBranch(string id)
  {
    return LoadBranch(Identify(id));
  }

  public Share.Branch LoadBranch(long id)
  {
    return Db.QuerySingle<Share.Branch>(@$"
      SELECT {Share.BranchFields}
      FROM branches
      WHERE id = @Id
    ", new { Id = id });
  }

  public Share.Branch BuildBranch(
    Account.Game game,
    string id,
    string? slug = null,
    string? password = null,
    bool isPinned = false,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return BuildBranch(
      game,
      id: Identify(id),
      slug,
      password,
      isPinned,
      createdOn,
      updatedOn
    );
  }

  public Share.Branch BuildBranch(
    Account.Game game,
    long? id = null,
    string? slug = null,
    string? password = null,
    bool isPinned = false,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    var org = RuntimeAssert.Present(game.Organization);
    slug = Format.Slugify(slug ?? Fake.Label());
    return new Share.Branch(password, Encryptor)
    {
      Id = id ?? NextId(),
      OrganizationId = org.Id,
      Organization = org,
      GameId = game.Id,
      Game = game,
      Slug = slug,
      IsPinned = isPinned,
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
    };
  }

  public Share.Branch CreateBranch(
    Account.Game game,
    string id,
    string? slug = null,
    string? password = null,
    bool isPinned = false,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    return CreateBranch(
      game,
      Identify(id),
      slug,
      password,
      isPinned,
      createdOn,
      updatedOn
    );
  }

  public Share.Branch CreateBranch(
    Account.Game game,
    long? id = null,
    string? slug = null,
    string? password = null,
    bool isPinned = false,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    var branch = BuildBranch(
      game,
      id,
      slug,
      password,
      isPinned,
      createdOn,
      updatedOn);

    Db.Execute(@"
      INSERT INTO branches (
        id,
        organization_id,
        game_id,
        slug,
        password,
        pinned,
        created_on,
        updated_on
      )
      VALUES (
        @Id,
        @OrganizationId,
        @GameId,
        @Slug,
        @EncryptedPassword,
        @IsPinned,
        @CreatedOn,
        @UpdatedOn
      )
    ", branch);
    return branch;
  }

  //===============================================================================================
  // DEPLOYS
  //===============================================================================================

  public Share.Deploy LoadDeploy(string id)
  {
    return LoadDeploy(Identify(id));
  }

  public Share.Deploy LoadDeploy(long id)
  {
    return Db.QuerySingle<Share.Deploy>(@$"
      SELECT {Share.DeployFields}
      FROM deploys
      WHERE id = @Id
    ", new { Id = id });
  }

  public Share.Deploy BuildDeploy(
    Share.Branch branch,
    Account.User user,
    string id,
    string? path = null,
    Share.DeployState? state = null,
    int? number = null,
    string? error = null,
    Instant? deployingOn = null,
    Account.User? deployedBy = null,
    Instant? deployedOn = null,
    Instant? failedOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null,
    Instant? deletedOn = null,
    string? deletedReason = null
  )
  {
    return BuildDeploy(
      branch,
      user,
      id: Identify(id),
      path,
      state,
      number,
      error,
      deployingOn,
      deployedBy,
      deployedOn,
      failedOn,
      createdOn,
      updatedOn,
      deletedOn,
      deletedReason
    );
  }

  public Share.Deploy BuildDeploy(
    Share.Branch branch,
    Account.User user,
    long? id = null,
    string? path = null,
    Share.DeployState? state = null,
    int? number = null,
    string? error = null,
    Instant? deployingOn = null,
    Account.User? deployedBy = null,
    Instant? deployedOn = null,
    Instant? failedOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null,
    Instant? deletedOn = null,
    string? deletedReason = null
  )
  {
    var org = RuntimeAssert.Present(branch.Organization);
    var game = RuntimeAssert.Present(branch.Game);
    number = number ?? 1;
    return new Share.Deploy
    {
      Id = id ?? NextId(),
      OrganizationId = org.Id,
      Organization = org,
      GameId = game.Id,
      Game = game,
      BranchId = branch.Id,
      Branch = branch,
      Path = path ?? Share.DeployPath(branch, number.Value),
      State = state ?? Share.DeployState.Ready,
      Number = number.Value,
      Error = error,
      DeployingOn = deployingOn is null ? null : Timestamp(deployingOn),
      DeployedBy = deployedBy?.Id ?? user.Id,
      DeployedOn = Timestamp(deployedOn),
      FailedOn = OptionalTimestamp(failedOn),
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
      DeletedOn = OptionalTimestamp(deletedOn),
      DeletedReason = deletedReason,
    };
  }

  public Share.Deploy CreateDeploy(
    Share.Branch branch,
    Account.User user,
    string id,
    string? path = null,
    Share.DeployState? state = null,
    int? number = null,
    string? error = null,
    Instant? deployingOn = null,
    Account.User? deployedBy = null,
    Instant? deployedOn = null,
    Instant? failedOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null,
    Instant? deletedOn = null,
    string? deletedReason = null,
    bool? asActive = true,
    bool? asLatest = true
  )
  {
    return CreateDeploy(
      branch,
      user,
      Identify(id),
      path,
      state,
      number,
      error,
      deployingOn,
      deployedBy,
      deployedOn,
      failedOn,
      createdOn,
      updatedOn,
      deletedOn,
      deletedReason,
      asActive,
      asLatest
    );
  }

  public Share.Deploy CreateDeploy(
    Share.Branch branch,
    Account.User user,
    long? id = null,
    string? path = null,
    Share.DeployState? state = null,
    int? number = null,
    string? error = null,
    Instant? deployingOn = null,
    Account.User? deployedBy = null,
    Instant? deployedOn = null,
    Instant? failedOn = null,
    Instant? createdOn = null,
    Instant? updatedOn = null,
    Instant? deletedOn = null,
    string? deletedReason = null,
    bool? asActive = true,
    bool? asLatest = true
  )
  {
    var deploy = BuildDeploy(
      branch,
      user,
      id,
      path,
      state,
      number,
      error,
      deployingOn,
      deployedBy,
      deployedOn,
      failedOn,
      createdOn,
      updatedOn,
      deletedOn,
      deletedReason);

    Db.Execute(@"
      INSERT INTO deploys (
        id,
        organization_id,
        game_id,
        branch_id,
        path,
        state,
        number,
        error,
        deploying_on,
        deployed_by,
        deployed_on,
        failed_on,
        created_on,
        updated_on,
        deleted_on,
        deleted_reason
      )
      VALUES (
        @Id,
        @OrganizationId,
        @GameId,
        @BranchId,
        @Path,
        @State,
        @Number,
        @Error,
        @DeployingOn,
        @DeployedBy,
        @DeployedOn,
        @FailedOn,
        @CreatedOn,
        @UpdatedOn,
        @DeletedOn,
        @DeletedReason
      )
    ", deploy);
    if (asActive is true || asLatest is true)
    {
      branch.ActiveDeployId = asActive is true ? deploy.Id : branch.ActiveDeployId;
      branch.LatestDeployId = asLatest is true ? deploy.Id : branch.LatestDeployId;
      Db.Execute("""
        UPDATE branches
           SET active_deploy_id = @ActiveDeployId,
               latest_deploy_id = @LatestDeployId
         WHERE id = @Id
      """, branch);

    }
    return deploy;
  }

  //===============================================================================================
  // CONTENT OBJECTS
  //===============================================================================================

  public Content.Object BuildContentObject(
    string content,
    long? id = null,
    string? blake3 = null,
    string? md5 = null,
    string? sha256 = null,
    string? contentType = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    blake3 = blake3 ?? Crypto.HexString(Crypto.Blake3(content));
    md5 = md5 ?? Crypto.HexString(Crypto.MD5(content));
    sha256 = sha256 ?? Crypto.HexString(Crypto.Sha256(content));
    var path = Content.ContentPath(blake3);
    return new Content.Object()
    {
      Id = id ?? NextId(),
      Path = path,
      Blake3 = blake3,
      MD5 = md5,
      Sha256 = sha256,
      ContentLength = content.Length,
      ContentType = contentType ?? Http.ContentType.Text,
      CreatedOn = Timestamp(createdOn),
      UpdatedOn = Timestamp(updatedOn),
    };
  }

  public Content.Object CreateContentObject(
    string content,
    long? id = null,
    string? blake3 = null,
    string? md5 = null,
    string? sha256 = null,
    string? contentType = null,
    Instant? createdOn = null,
    Instant? updatedOn = null
  )
  {
    var obj = BuildContentObject(content, id, blake3, md5, sha256, contentType, createdOn, updatedOn);
    Db.Execute(@"
      INSERT INTO content_objects (
        id,
        path,
        blake3,
        md5,
        sha256,
        content_length,
        content_type,
        created_on,
        updated_on
      )
      VALUES (
        @Id,
        @Path,
        @Blake3,
        @MD5,
        @Sha256,
        @ContentLength,
        @ContentType,
        @CreatedOn,
        @UpdatedOn
      )
    ", obj);
    return obj;
  }

  public Content.Object LoadContentObject(long id)
  {
    return Db.QuerySingle<Content.Object>(@$"
      SELECT {Content.ObjectFields}
      FROM content_objects
      WHERE id = @Id
    ", new { Id = id });
  }

  //===============================================================================================
  // GITHUB RELEASES
  //===============================================================================================

  public GitHub.Release BuildGitHubRelease(
    long? id = null,
    string? name = null,
    string? tagName = null,
    bool preRelease = false,
    bool draft = false,
    Instant? publishedOn = null,
    string? body = null,
    GitHub.ReleaseAsset[]? assets = null
  )
  {
    return new GitHub.Release()
    {
      Id = id ?? NextId(),
      Name = name ?? Fake.ProductName(),
      TagName = tagName ?? Fake.Version(),
      PreRelease = preRelease,
      Draft = draft,
      PublishedOn = Timestamp(publishedOn),
      Body = body ?? "",
      Assets = assets is not null ? assets.ToList() : new List<GitHub.ReleaseAsset>(),
    };
  }

  public GitHub.ReleaseAsset BuildGitHubReleaseAsset(
    long? id = null,
    string? name = null,
    string? contentType = null,
    int? contentLength = null,
    string? url = null,
    GitHub.ReleasePlatform? platform = null
  )
  {
    name = name ?? Fake.FileName();
    platform = platform ?? GitHub.IdentifyReleasePlatform(name);
    return new GitHub.ReleaseAsset()
    {
      Id = id ?? NextId(),
      Name = name,
      ContentType = contentType ?? Fake.ContentType(),
      ContentLength = contentLength ?? Fake.Random.Number(1000),
      Url = url ?? Fake.Url(),
      Platform = platform.Value,
    };
  }

  //===============================================================================================
  // HELPER METHODS
  //===============================================================================================

  public static long Identify(string id)
  {
    return Crypto.Crc32(id);
  }

  private Instant Timestamp(Instant? on)
  {
    return (on ?? Now).TruncateToMilliseconds();
  }

  private Instant? OptionalTimestamp(Instant? on)
  {
    return on is null ? null : on.Value.TruncateToMilliseconds();
  }

  //===============================================================================================
}