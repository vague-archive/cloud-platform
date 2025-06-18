namespace Void.Platform.Domain;

public partial class Account
{
  //-----------------------------------------------------------------------------------------------

  public enum TokenType
  {
    Register,
    Access,
    Invite,
    ResetPassword,
    ChangeEmail,
  }

  //-----------------------------------------------------------------------------------------------

  private static readonly Dictionary<TokenType, Duration> tokenTTL = new Dictionary<TokenType, Duration>
  {
    { TokenType.Register, Duration.FromHours(1) },
    { TokenType.Access, Duration.FromDays(365) },
    { TokenType.Invite, Duration.FromDays(7) },
    { TokenType.ResetPassword, Duration.FromHours(1) },
    { TokenType.ChangeEmail, Duration.FromHours(1) },
  };

  public static Duration TokenTTL(TokenType type)
  {
    return tokenTTL[type];
  }

  //-----------------------------------------------------------------------------------------------

  public record Token
  {
    public required long Id { get; set; }
    public required TokenType Type { get; set; }
    public string? Value { get; set; }          // the (base64 encoded) token, but is never actually stored in DB...
    public required string Digest { get; set; } // ... instead we store the hash digest in the DB
    public required string Tail { get; set; }   // ... we do also store the tail digits of the token to help the user identify which is which in the UX
    public long? UserId { get; set; }
    public long? OrganizationId { get; set; }
    public string? SentTo { get; set; }
    public required bool IsSpent { get; set; }
    public Instant? ExpiresOn { get; set; }
    public required Instant CreatedOn { get; set; }
    public required Instant UpdatedOn { get; set; }

    public bool HasExpired(Instant instant)
    {
      return ExpiresOn is not null && (ExpiresOn < instant);
    }
  }

  public static string TokenTail(string value)
  {
    return value.Substring(Math.Max(0, value.Length - 6));
  }

  //===============================================================================================
  // ACCESS TOKEN METHODS
  //===============================================================================================

  public Token? GetAccessToken(string tokenValue)
  {
    return GetToken(TokenType.Access, tokenValue);
  }

  public List<Token> GetAccessTokens(User user)
  {
    return GetAccessTokens(user.Id);
  }

  public List<Token> GetAccessTokens(long userId)
  {
    return Db.Query<Token>(@"
      SELECT id,
             type,
             digest,
             tail,
             user_id as UserId,
             organization_id as OrganizationId,
             sent_to as SentTo,
             is_spent as IsSpent,
             expires_on as ExpiresOn,
             created_on as CreatedOn,
             updated_on as UpdatedOn
      FROM tokens
      WHERE type = @Type
        AND user_id = @UserId
    ", new
    {
      Type = TokenType.Access,
      UserId = userId
    });
  }

  public Token GenerateAccessToken(User user)
  {
    return GenerateAccessToken(user.Id);
  }

  public Token GenerateAccessToken(long userId)
  {
    return CreateToken(TokenType.Access, new CreateTokenOptions
    {
      UserId = userId
    });
  }

  public bool RevokeAccessToken(Token token)
  {
    RuntimeAssert.True(token.Type == TokenType.Access);
    return DeleteToken(token);
  }

  //===============================================================================================
  // INVITATE TOKEN METHODS
  //===============================================================================================

  public Token? GetInvite(string tokenValue)
  {
    return GetToken(TokenType.Invite, tokenValue);
  }

  public Token? GetInviteById(long id)
  {
    return GetTokenById(TokenType.Invite, id);
  }

  public List<Token> GetInvitesFor(Organization org)
  {
    return Db.Query<Token>(@"
      SELECT id,
             type,
             digest,
             tail,
             user_id as UserId,
             organization_id as OrganizationId,
             sent_to as SentTo,
             is_spent as IsSpent,
             expires_on as ExpiresOn,
             created_on as CreatedOn,
             updated_on as UpdatedOn
      FROM tokens
      WHERE type = @Type
        AND organization_id = @OrganizationId
        AND is_spent IS FALSE
        AND ((expires_on IS NULL) OR
             (expires_on > @Now))
   ORDER BY sent_to
    ", new
    {
      Type = TokenType.Invite,
      OrganizationId = org.Id,
      Now = Now,
    });
    throw new NotImplementedException();
  }

  //-----------------------------------------------------------------------------------------------

  public class SendInviteCommand
  {
    public const string TokenPlaceholder = "insert-token-here";

    public string Email { get; set; } = "";
    public string ActionUrl { get; set; } = "";

    public class Validator : AbstractValidator<SendInviteCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Email)
          .NotEmpty().WithMessage(Validation.IsMissing);

        RuleFor(cmd => cmd.Email)
          .EmailAddress()
          .When(cmd => !String.IsNullOrWhiteSpace(cmd.Email))
          .WithMessage(Validation.IsInvalid);

        RuleFor(cmd => cmd.ActionUrl)
          .NotEmpty().WithMessage(Validation.IsMissing);

        RuleFor(cmd => cmd.ActionUrl)
          .Must(value => value.Contains(TokenPlaceholder))
          .When(cmd => !String.IsNullOrWhiteSpace(cmd.ActionUrl))
          .WithMessage("is missing token placeholder");
      }
    }
  }

  public async Task<Result<Token>> SendInvite(Organization org, SendInviteCommand cmd)
  {
    var result = new SendInviteCommand.Validator().Validate(cmd);
    if (!result.IsValid)
      return Validation.Fail(result);

    var members = GetOrganizationMembers(org);
    if (members.Any(m => m.User!.Email.ToLower() == cmd.Email.ToLower()))
    {
      return Validation.Fail(nameof(cmd.Email), "is already a member of this organization");
    }

    var invites = GetInvitesFor(org);
    if (invites.Any(i => i.SentTo?.ToLower() == cmd.Email.ToLower()))
    {
      return Validation.Fail(nameof(cmd.Email), "has already been invited to join this organization");
    }

    var token = CreateToken(TokenType.Invite, new CreateTokenOptions
    {
      OrganizationId = org.Id,
      SentTo = cmd.Email,
    });

    await this.Mailer.Deliver("invite", to: cmd.Email, data: new
    {
      organization = org.Name,
      action_url = cmd.ActionUrl.Replace(SendInviteCommand.TokenPlaceholder, token.Value),
    });

    return Result.Ok(token);
  }

  //-----------------------------------------------------------------------------------------------

  public Result<Token> RetractInvite(Token token)
  {
    RuntimeAssert.True(token.Type == TokenType.Invite);
    DeleteToken(token);
    return Result.Ok(token);
  }

  //-----------------------------------------------------------------------------------------------

  public class AcceptInviteCommand
  {
    public required IdentityProvider Provider { get; init; }
    public required string Identifier { get; init; }
    public required string UserName { get; init; }
    public required string FullName { get; init; }
    public required string TimeZone { get; init; }
    public required string Locale { get; init; }

    public class Validator : AbstractValidator<AcceptInviteCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Identifier)
          .NotEmpty().WithMessage(Validation.IsMissing);

        RuleFor(cmd => cmd.UserName)
          .NotEmpty().WithMessage(Validation.IsMissing);

        RuleFor(cmd => cmd.FullName)
          .NotEmpty().WithMessage(Validation.IsMissing);

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

  public Result<AuthenticatedUser> AcceptInviteForNewUser(Token token, AcceptInviteCommand cmd)
  {
    RuntimeAssert.True(token.Type == TokenType.Invite);
    RuntimeAssert.True(token.IsSpent == false);
    RuntimeAssert.True(token.HasExpired(Now) == false);
    RuntimeAssert.Present(token.OrganizationId);

    var result = new AcceptInviteCommand.Validator().Validate(cmd);
    if (!result.IsValid)
      return Validation.Fail(result);

    var org = GetOrganization(token.OrganizationId.Value);
    if (org is null)
      return Validation.Fail(nameof(token.OrganizationId), Validation.NotFound);

    try
    {
      var user = Db.Transaction(org, () =>
      {
        var userId = Db.Insert(@"
          INSERT INTO users (
            name,
            email,
            timezone,
            locale,
            disabled,
            created_on,
            updated_on
          ) VALUES (
            @Name,
            @Email,
            @TimeZone,
            @Locale,
            @Disabled,
            @CreatedOn,
            @UpdatedOn
          )
        ", new
        {
          Name = cmd.FullName,
          Email = token.SentTo,
          TimeZone = cmd.TimeZone,
          Locale = cmd.Locale,
          Disabled = false,
          CreatedOn = Now,
          UpdatedOn = Now,
        });

        Db.Insert(@"
          INSERT INTO identities (
            user_id,
            provider,
            identifier,
            username,
            created_on,
            updated_on
          ) VALUES (
            @UserId,
            @Provider,
            @Identifier,
            @UserName,
            @CreatedOn,
            @UpdatedOn
          )
        ", new
        {
          UserId = userId,
          Provider = cmd.Provider,
          Identifier = cmd.Identifier,
          UserName = cmd.UserName,
          CreatedOn = Now,
          UpdatedOn = Now,
        });

        Db.Insert(@"
          INSERT INTO members (
            organization_id,
            user_id,
            created_on,
            updated_on
          ) VALUES (
            @OrganizationId,
            @UserId,
            @CreatedOn,
            @UpdatedOn
          )
        ", new
        {
          OrganizationId = org.Id,
          UserId = userId,
          CreatedOn = Now,
          UpdatedOn = Now,
        });

        Db.Execute(@"
          UPDATE tokens
             SET is_spent = true
           WHERE id = @Id
        ", new { Id = token.Id });

        var user = GetAuthenticatedUser(userId);
        RuntimeAssert.Present(user);

        return user;
      });

      return Result.Ok(user);
    }
    catch (MySqlConnector.MySqlException ex) when (ex.ErrorCode == MySqlConnector.MySqlErrorCode.DuplicateKeyEntry)
    {
      if (ex.Message.Contains("identities.identities_provider_username_index"))
        return Validation.Fail($"{cmd.Provider.ToString().ToLower()} username", "is already in use");
      else if (ex.Message.Contains("identities.identities_provider_identifier_index"))
        return Validation.Fail($"{cmd.Provider.ToString().ToLower()} identity", "is already in use");
      else if (ex.Message.Contains("users.email"))
        return Validation.Fail("email", "is already in use");
      else
        throw;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public Result<(AuthenticatedUser, Organization)> AcceptInviteForExistingUser(Token token, User user)
  {
    return AcceptInviteForExistingUser(token, user.Id);
  }

  public Result<(AuthenticatedUser, Organization)> AcceptInviteForExistingUser(Token token, long userId)
  {
    RuntimeAssert.True(token.Type == TokenType.Invite);
    RuntimeAssert.True(token.IsSpent == false);
    RuntimeAssert.True(token.HasExpired(Now) == false);
    RuntimeAssert.Present(token.OrganizationId);

    var org = GetOrganization(token.OrganizationId.Value);
    if (org is null)
      return Validation.Fail(nameof(token.OrganizationId), Validation.NotFound);

    Db.Transaction(org, () =>
    {
      Db.Insert(@"
        INSERT INTO members (
          organization_id,
          user_id,
          created_on,
          updated_on
        ) VALUES (
          @OrganizationId,
          @UserId,
          @CreatedOn,
          @UpdatedOn
        )
        ON DUPLICATE KEY UPDATE updated_on = VALUES(updated_on)
      ", new
      {
        OrganizationId = org.Id,
        UserId = userId,
        CreatedOn = Now,
        UpdatedOn = Now,
      });

      Db.Execute(@"
        UPDATE tokens
           SET is_spent = true
         WHERE id = @Id
      ", new { Id = token.Id });
      return true;
    });

    var authenticatedUser = GetAuthenticatedUser(userId);
    RuntimeAssert.Present(authenticatedUser);

    return Result.Ok((authenticatedUser, org));
  }

  //===============================================================================================
  // SHARED TOKEN IMPLEMENTATION
  //===============================================================================================

  private Token? GetToken(TokenType type, string tokenValue)
  {
    var digest = Crypto.HashToken(tokenValue);
    return Db.QuerySingleOrDefault<Token>(@"
      SELECT id,
             type,
             digest,
             tail,
             user_id as UserId,
             organization_id as OrganizationId,
             sent_to as SentTo,
             is_spent as IsSpent,
             expires_on as ExpiresOn,
             created_on as CreatedOn,
             updated_on as UpdatedOn
      FROM tokens
      WHERE type = @Type
        AND digest = @Digest
        AND is_spent IS FALSE
        AND ((expires_on IS NULL) OR
             (expires_on > @Now))
    ", new
    {
      Type = type,
      Digest = digest,
      Now = Now,
    });
  }

  private Token? GetTokenById(TokenType type, long id)
  {
    return Db.QuerySingleOrDefault<Token>(@"
      SELECT id,
             type,
             digest,
             tail,
             user_id as UserId,
             organization_id as OrganizationId,
             sent_to as SentTo,
             is_spent as IsSpent,
             expires_on as ExpiresOn,
             created_on as CreatedOn,
             updated_on as UpdatedOn
      FROM tokens
      WHERE id = @Id
        AND type = @Type
        AND is_spent IS FALSE
        AND ((expires_on IS NULL) OR
             (expires_on > @Now))
    ", new
    {
      Id = id,
      Type = type,
      Now = Now,
    });
  }

  //-----------------------------------------------------------------------------------------------

  private record CreateTokenOptions
  {
    public long? UserId { get; init; }
    public long? OrganizationId { get; init; }
    public string? SentTo { get; init; }
  }

  private Token CreateToken(TokenType type, CreateTokenOptions options)
  {
    var value = Crypto.GenerateToken();
    var digest = Crypto.HashToken(value);
    var expires = Now.Plus(TokenTTL(type));
    var tail = Account.TokenTail(value);

    var token = new Token
    {
      Id = 0,
      Type = type,
      Value = value,
      Digest = digest,
      Tail = tail,
      UserId = options.UserId,
      OrganizationId = options.OrganizationId,
      SentTo = options.SentTo,
      IsSpent = false,
      ExpiresOn = expires,
      CreatedOn = Now,
      UpdatedOn = Now,
    };

    token.Id = Db.Insert(@"
      INSERT INTO tokens (
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
      ) VALUES (
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

  //-----------------------------------------------------------------------------------------------

  private bool DeleteToken(Token token)
  {
    var rowsAffected = Db.Execute(@"
      DELETE FROM tokens
            WHERE id = @Id
    ", new { Id = token.Id });
    return rowsAffected == 1;
  }

  //-----------------------------------------------------------------------------------------------
}