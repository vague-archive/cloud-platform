namespace Void.Platform.Domain;

public class TokenTest : TestCase
{
  //===============================================================================================
  // TEST GENERIC TOKEN STUFF
  //===============================================================================================

  [Fact]
  public void TestTokenTTL()
  {
    Assert.Equal(Duration.FromHours(1), Account.TokenTTL(Account.TokenType.Register));
    Assert.Equal(Duration.FromDays(365), Account.TokenTTL(Account.TokenType.Access));
    Assert.Equal(Duration.FromDays(7), Account.TokenTTL(Account.TokenType.Invite));
    Assert.Equal(Duration.FromHours(1), Account.TokenTTL(Account.TokenType.ResetPassword));
    Assert.Equal(Duration.FromHours(1), Account.TokenTTL(Account.TokenType.ChangeEmail));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestTokenHasExpired()
  {
    using (var test = new DomainTest(this))
    {
      var earlier = Clock.Now.Minus(Duration.FromSeconds(1));
      var later = Clock.Now.Plus(Duration.FromSeconds(1));

      var infiniteAccessToken = test.Factory.CreateToken();
      var temporaryAccessToken = test.Factory.CreateToken(expiresOn: later);
      var expiredAccessToken = test.Factory.CreateToken(expiresOn: earlier);

      Assert.False(infiniteAccessToken.HasExpired(Clock.Now));
      Assert.False(temporaryAccessToken.HasExpired(Clock.Now));
      Assert.True(expiredAccessToken.HasExpired(Clock.Now));
    }
  }

  //===============================================================================================
  // TEST ACCESS TOKENS
  //===============================================================================================

  [Fact]
  public void TestGetAccessToken()
  {
    using (var test = new DomainTest(this))
    {
      var earlier = Clock.Now.Minus(Duration.FromSeconds(1));
      var later = Clock.Now.Plus(Duration.FromSeconds(1));

      var user = test.Factory.LoadUser("active");

      var tokenValue1 = "token1";
      var tokenValue2 = "token2";
      var tokenValue3 = "token3";
      var tokenValue4 = "token4";

      var token1 = test.Factory.CreateToken(type: Account.TokenType.Access, value: tokenValue1, user: user);
      var token2 = test.Factory.CreateToken(type: Account.TokenType.Access, value: tokenValue2, user: user, expiresOn: later);
      var token3 = test.Factory.CreateToken(type: Account.TokenType.Access, value: tokenValue3, user: user, expiresOn: earlier);
      var token4 = test.Factory.CreateToken(type: Account.TokenType.Access, value: tokenValue4, user: user, expiresOn: later, isSpent: true);

      Assert.Present(token1.Value);
      Assert.Present(token2.Value);
      Assert.Present(token3.Value);
      Assert.Present(token4.Value);

      Assert.Equal(Crypto.GenerateToken(tokenValue1), token1.Value);
      Assert.Equal(Crypto.GenerateToken(tokenValue2), token2.Value);
      Assert.Equal(Crypto.GenerateToken(tokenValue3), token3.Value);
      Assert.Equal(Crypto.GenerateToken(tokenValue4), token4.Value);

      Assert.Preconditions.Absent(token1.ExpiresOn);
      Assert.Preconditions.Present(token2.ExpiresOn);
      Assert.Preconditions.Present(token3.ExpiresOn);
      Assert.Preconditions.Present(token4.ExpiresOn);

      Assert.Preconditions.False(token1.HasExpired(Clock.Now));
      Assert.Preconditions.False(token2.HasExpired(Clock.Now));
      Assert.Preconditions.True(token3.HasExpired(Clock.Now));
      Assert.Preconditions.False(token4.HasExpired(Clock.Now));

      Assert.Preconditions.False(token1.IsSpent);
      Assert.Preconditions.False(token2.IsSpent);
      Assert.Preconditions.False(token3.IsSpent);
      Assert.Preconditions.True(token4.IsSpent);

      var token = test.App.Account.GetAccessToken(token1.Value);

      Assert.NotNull(token);
      Assert.Equal(token1.Id, token.Id);
      Assert.Equal(user.Id, token.UserId);
      Assert.Null(token.Value);
      Assert.Equal(token1.Digest, token.Digest);
      Assert.Equal(token1.Tail, token.Tail);
      Assert.Null(token.ExpiresOn);
      Assert.False(token.IsSpent);

      token = test.App.Account.GetAccessToken(token2.Value);

      Assert.NotNull(token);
      Assert.Equal(token2.Id, token.Id);
      Assert.Equal(user.Id, token.UserId);
      Assert.Null(token.Value);
      Assert.Equal(token2.Digest, token.Digest);
      Assert.Equal(token2.Tail, token.Tail);
      Assert.Equal(later, token.ExpiresOn);
      Assert.False(token.IsSpent);

      token = test.App.Account.GetAccessToken(token3.Value);
      Assert.Null(token);

      token = test.App.Account.GetAccessToken(Crypto.GenerateToken("unknown"));
      Assert.Null(token);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetAccessTokensForUser()
  {
    using (var test = new DomainTest(this))
    {
      var user1 = test.Factory.CreateUser();
      var user2 = test.Factory.CreateUser();

      var token1a = test.Factory.CreateToken(id: 101, user: user1);
      var token1b = test.Factory.CreateToken(id: 102, user: user1);
      var token2a = test.Factory.CreateToken(id: 201, user: user2);

      var tokens = test.App.Account.GetAccessTokens(user1);
      Assert.Equal(2, tokens.Count);
      Assert.Equal(token1a.Id, tokens[0].Id);
      Assert.Equal(token1b.Id, tokens[1].Id);

      tokens = test.App.Account.GetAccessTokens(user2);
      Assert.Single(tokens);
      Assert.Equal(token2a.Id, tokens[0].Id);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGenerateAccessTokenForUser()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();

      var token = test.App.Account.GenerateAccessToken(user);

      Assert.Equal(Account.TokenType.Access, token.Type);
      Assert.NotNull(token.Value);
      Assert.Equal(Crypto.HashToken(token.Value), token.Digest);
      Assert.Equal(Account.TokenTail(token.Value), token.Tail);
      Assert.Equal(user.Id, token.UserId);
      Assert.Null(token.OrganizationId);
      Assert.False(token.IsSpent);
      Assert.Equal(Clock.Now.Plus(Account.TokenTTL(token.Type)), token.ExpiresOn);
      Assert.Equal(Clock.Now, token.CreatedOn);
      Assert.Equal(Clock.Now, token.UpdatedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestRevokeAccessToken()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var token = test.Factory.CreateToken(user: user);

      var reloadedUser = test.App.Account.GetUserByAccessToken(token.Value!);
      Assert.NotNull(reloadedUser);
      Assert.Equal(user.Id, reloadedUser.Id);

      var result = test.App.Account.RevokeAccessToken(token);
      Assert.True(result);

      reloadedUser = test.App.Account.GetUserByAccessToken(token.Value!);
      Assert.Null(reloadedUser);
    }
  }

  //===============================================================================================
  // TEST INVITATION TOKENS
  //===============================================================================================

  [Fact]
  public void TestGetInviteByTokenValueAndId()
  {
    using (var test = new DomainTest(this))
    {
      var earlier = Clock.Now.Minus(Duration.FromSeconds(1));
      var later = Clock.Now.Plus(Duration.FromSeconds(1));

      var org = test.Factory.LoadOrganization("nintendo");
      var email = Fake.Email();

      var validInviteToken = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: email, expiresOn: later);
      var expiredInviteToken = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: email, expiresOn: earlier);
      var spentInviteToken = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: email, isSpent: true);
      var accessToken = test.Factory.CreateToken(type: Account.TokenType.Access);

      Assert.False(validInviteToken.HasExpired(Clock.Now));
      Assert.True(expiredInviteToken.HasExpired(Clock.Now));
      Assert.False(spentInviteToken.HasExpired(Clock.Now));
      Assert.False(accessToken.HasExpired(Clock.Now));

      Assert.False(validInviteToken.IsSpent);
      Assert.False(expiredInviteToken.IsSpent);
      Assert.True(spentInviteToken.IsSpent);
      Assert.False(accessToken.IsSpent);

      var token = test.App.Account.GetInvite(validInviteToken.Value!);

      Assert.NotNull(token);
      Assert.Equal(validInviteToken.Id, token.Id);
      Assert.Equal(org.Id, token.OrganizationId);
      Assert.Equal(email, token.SentTo);
      Assert.Null(token.Value);
      Assert.Equal(validInviteToken.Digest, token.Digest);
      Assert.Equal(validInviteToken.Tail, token.Tail);
      Assert.Equal(later, token.ExpiresOn);
      Assert.False(token.IsSpent);

      token = test.App.Account.GetInvite(expiredInviteToken.Value!);
      Assert.Null(token);

      token = test.App.Account.GetInvite(spentInviteToken.Value!);
      Assert.Null(token);

      token = test.App.Account.GetInvite(accessToken.Value!);
      Assert.Null(token);

      token = test.App.Account.GetInvite("unknown");
      Assert.Null(token);

      token = test.App.Account.GetInviteById(validInviteToken.Id);
      Assert.NotNull(token);
      Assert.Equal(validInviteToken.Id, token.Id);
      Assert.Equal(org.Id, token.OrganizationId);
      Assert.Equal(email, token.SentTo);
      Assert.Null(token.Value);
      Assert.Equal(validInviteToken.Digest, token.Digest);
      Assert.Equal(validInviteToken.Tail, token.Tail);
      Assert.Equal(later, token.ExpiresOn);
      Assert.False(token.IsSpent);

      token = test.App.Account.GetInviteById(expiredInviteToken.Id);
      Assert.Null(token);

      token = test.App.Account.GetInviteById(spentInviteToken.Id);
      Assert.Null(token);

      token = test.App.Account.GetInviteById(accessToken.Id);
      Assert.Null(token);

      token = test.App.Account.GetInviteById(42);
      Assert.Null(token);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetInvitationsForOrganization()
  {
    using (var test = new DomainTest(this))
    {
      var earlier = Clock.Now.Minus(Duration.FromSeconds(1));
      var later = Clock.Now.Plus(Duration.FromSeconds(1));

      var org1 = test.Factory.CreateOrganization();
      var org2 = test.Factory.CreateOrganization();

      test.Factory.CreateToken(type: Account.TokenType.Invite, org: org1, sentTo: "first");
      test.Factory.CreateToken(type: Account.TokenType.Invite, org: org1, sentTo: "second", expiresOn: later);
      test.Factory.CreateToken(type: Account.TokenType.Invite, org: org1, sentTo: "expired", expiresOn: earlier);
      test.Factory.CreateToken(type: Account.TokenType.Invite, org: org1, sentTo: "spent", isSpent: true);
      test.Factory.CreateToken(type: Account.TokenType.Invite, org: org2, sentTo: "other", expiresOn: later);

      var invites = test.App.Account.GetInvitesFor(org1);
      Assert.Equal(["first", "second"], invites.Select(i => i.SentTo!));

      invites = test.App.Account.GetInvitesFor(org2);
      Assert.Equal(["other"], invites.Select(i => i.SentTo!));
    }
  }

  //===============================================================================================
  // TEST SENDING INVITES
  //===============================================================================================

  private const string JoinActionUrl = "/join/insert-token-here";

  [Fact]
  public async Task TestSendInvite()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("nintendo");
      var email = "someone@example.com";

      var result = await test.App.Account.SendInvite(org, new Account.SendInviteCommand
      {
        Email = email,
        ActionUrl = JoinActionUrl
      });

      Assert.Succeeded(result);

      var token = result.Value;
      Assert.Equal(Account.TokenType.Invite, token.Type);

      Assert.NotNull(token.Value);
      Assert.Equal(Crypto.HashToken(token.Value), token.Digest);
      Assert.Equal(Account.TokenTail(token.Value), token.Tail);
      Assert.Equal(org.Id, token.OrganizationId);
      Assert.Null(token.UserId);
      Assert.Equal(email, token.SentTo);
      Assert.False(token.IsSpent);
      Assert.Equal(Clock.Now.Plus(Account.TokenTTL(Account.TokenType.Invite)), token.ExpiresOn);
      Assert.Equal(Clock.Now, token.CreatedOn);
      Assert.Equal(Clock.Now, token.UpdatedOn);

      var mail = Assert.Mailed(test.Mailer, template: "invite", to: email);
      Assert.Equal(org.Name, mail.Data["organization"]);
      Assert.Equal($"/join/{token.Value}", mail.Data["action_url"]);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestSendInviteMissingFields()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("nintendo");
      var result = await test.App.Account.SendInvite(org, new Account.SendInviteCommand());
      var errors = Assert.FailedValidation(result);
      Assert.Equal(2, errors.Count);
      Assert.Equal("email is missing", errors[0].Format());
      Assert.Equal("actionurl is missing", errors[1].Format());
      Assert.NothingMailed(test.Mailer);
    }
  }

  [Fact]
  public async Task TestSendInviteInvalidEmail()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("nintendo");
      var result = await test.App.Account.SendInvite(org, new Account.SendInviteCommand { Email = "not an email", ActionUrl = JoinActionUrl });
      var errors = Assert.FailedValidation(result);
      Assert.Single(errors);
      Assert.Equal("email is invalid", errors[0].Format());
      Assert.NothingMailed(test.Mailer);
    }
  }

  [Fact]
  public async Task TestSendInviteInvalidActionUrl()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("nintendo");
      var result = await test.App.Account.SendInvite(org, new Account.SendInviteCommand { Email = "foo@bar.com", ActionUrl = "/join/missing-token" });
      var errors = Assert.FailedValidation(result);
      Assert.Single(errors);
      Assert.Equal("actionurl is missing token placeholder", errors[0].Format());
      Assert.NothingMailed(test.Mailer);
    }
  }

  [Fact]
  public async Task TestSendInviteAlreadyAMember()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var org = test.Factory.LoadOrganization("atari");
      var result = await test.App.Account.SendInvite(org, new Account.SendInviteCommand { Email = user.Email, ActionUrl = JoinActionUrl });
      var errors = Assert.FailedValidation(result);
      Assert.Single(errors);
      Assert.Equal("email is already a member of this organization", errors[0].Format());
      Assert.NothingMailed(test.Mailer);
    }
  }

  [Fact]
  public async Task TestSendInviteEmailAlreadyInvites()
  {
    using (var test = new DomainTest(this))
    {
      var email = Fake.Email();
      var org = test.Factory.LoadOrganization("atari");
      test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: email);
      var result = await test.App.Account.SendInvite(org, new Account.SendInviteCommand { Email = email, ActionUrl = JoinActionUrl });
      var errors = Assert.FailedValidation(result);
      Assert.Single(errors);
      Assert.Equal("email has already been invited to join this organization", errors[0].Format());
      Assert.NothingMailed(test.Mailer);
    }
  }

  //===============================================================================================
  // TEST RETRACTING INVITES
  //===============================================================================================

  [Fact]
  public void TestRetractInvite()
  {
    using (var test = new DomainTest(this))
    {
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite);
      Assert.NotNull(token.Value);

      var reload = test.App.Account.GetInvite(token.Value);
      Assert.NotNull(reload);
      Assert.Equal(token.Id, reload.Id);

      var result = test.App.Account.RetractInvite(token);
      Assert.Succeeded(result);

      reload = test.App.Account.GetInvite(token.Value);
      Assert.Null(reload);
    }
  }

  //===============================================================================================
  // TEST ACCEPTING INVITES FOR NEW USER
  //===============================================================================================

  [Fact]
  public void TestAcceptInviteForNewUser()
  {
    using (var test = new DomainTest(this))
    {
      var email = Fake.Email();
      var provider = Fake.IdentityProvider();
      var identifier = Fake.Identifier();
      var userName = Fake.UserName();
      var fullName = Fake.FullName();
      var timeZone = Fake.TimeZone();
      var locale = Fake.Locale();

      var org = test.Factory.LoadOrganization("nintendo");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: email);

      var result = test.App.Account.AcceptInviteForNewUser(token, new Account.AcceptInviteCommand
      {
        Provider = provider,
        Identifier = identifier,
        UserName = userName,
        FullName = fullName,
        TimeZone = timeZone,
        Locale = locale,
      });

      Assert.Succeeded(result);
      var user = result.Value;

      Assert.Equal(fullName, user.Name);
      Assert.Equal(email, user.Email);
      Assert.Equal(timeZone, user.TimeZone);
      Assert.Equal(locale, user.Locale);
      Assert.Null(user.Password);
      Assert.False(user.Disabled);
      Assert.Equal(Clock.Now, user.CreatedOn);
      Assert.Equal(Clock.Now, user.UpdatedOn);

      Assert.Present(user.Roles);
      Assert.Present(user.Identities);
      Assert.Present(user.Organizations);

      Assert.Empty(user.Roles);
      Assert.Single(user.Identities);
      Assert.Single(user.Organizations);

      Assert.Equal(user.Id, user.Identities[0].UserId);
      Assert.Equal(provider, user.Identities[0].Provider);
      Assert.Equal(identifier, user.Identities[0].Identifier);
      Assert.Equal(userName, user.Identities[0].UserName);
      Assert.Equal(Clock.Now, user.Identities[0].CreatedOn);
      Assert.Equal(Clock.Now, user.Identities[0].UpdatedOn);

      Assert.Equal(org, user.Organizations[0]);

      var reloadToken = test.App.Account.GetInvite(token.Value!);
      Assert.Null(reloadToken); // token has been spent
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestAcceptInviteForNewUserValidationErrors()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var org = test.Factory.LoadOrganization("nintendo");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: user.Email);

      var result = test.App.Account.AcceptInviteForNewUser(token, new Account.AcceptInviteCommand
      {
        Provider = Fake.IdentityProvider(),
        Identifier = "",
        UserName = "",
        FullName = "",
        TimeZone = "",
        Locale = "",
      });
      var errors = Assert.FailedValidation(result);
      Assert.Equal([
        "identifier is missing",
        "username is missing",
        "fullname is missing",
        "timezone is missing",
        "locale is missing",
      ], errors.Select(e => e.Format()));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestAcceptInviteForNewUserEmailAlreadyTaken()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var org = test.Factory.LoadOrganization("nintendo");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: user.Email);
      var result = test.App.Account.AcceptInviteForNewUser(token, new Account.AcceptInviteCommand
      {
        Provider = Fake.IdentityProvider(),
        Identifier = Fake.Identifier(),
        UserName = Fake.UserName(),
        FullName = Fake.FullName(),
        TimeZone = Fake.TimeZone(),
        Locale = Fake.Locale(),
      });
      var errors = Assert.FailedValidation(result);
      Assert.Equal(["email is already in use"], errors.Select(e => e.Format()));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestAcceptInviteForNewUserIdentifierAlreadyTaken()
  {
    using (var test = new DomainTest(this))
    {
      var identity = test.Factory.LoadIdentity("active:github");
      var org = test.Factory.LoadOrganization("nintendo");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: Fake.Email());
      var result = test.App.Account.AcceptInviteForNewUser(token, new Account.AcceptInviteCommand
      {
        Provider = identity.Provider,
        Identifier = identity.Identifier,
        UserName = Fake.UserName(),
        FullName = Fake.FullName(),
        TimeZone = Fake.TimeZone(),
        Locale = Fake.Locale(),
      });
      var errors = Assert.FailedValidation(result);
      Assert.Equal(["github identity is already in use"], errors.Select(e => e.Format()));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestAcceptInviteForNewUserUserNameAlreadyTaken()
  {
    using (var test = new DomainTest(this))
    {
      var identity = test.Factory.LoadIdentity("active:github");
      var org = test.Factory.LoadOrganization("nintendo");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: Fake.Email());
      var result = test.App.Account.AcceptInviteForNewUser(token, new Account.AcceptInviteCommand
      {
        Provider = identity.Provider,
        Identifier = Fake.Identifier(),
        UserName = identity.UserName,
        FullName = Fake.FullName(),
        TimeZone = Fake.TimeZone(),
        Locale = Fake.Locale(),
      });
      var errors = Assert.FailedValidation(result);
      Assert.Equal(["github username is already in use"], errors.Select(e => e.Format()));
    }
  }

  //===============================================================================================
  // TEST ACCEPTING INVITES FOR EXISTING USER
  //===============================================================================================

  [Fact]
  public void TestAcceptInviteForExistingUser()
  {
    using (var test = new DomainTest(this))
    {
      var existingOrg = test.Factory.LoadOrganization("atari");
      var existingUser = test.Factory.LoadUser("active");
      var newOrg = test.Factory.CreateOrganization(name: "Acme");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: newOrg, sentTo: existingUser.Email);

      existingUser = test.App.Account.WithOrganizations(existingUser);
      Assert.True(existingUser.BelongsTo(existingOrg));
      Assert.False(existingUser.BelongsTo(newOrg));

      var result = test.App.Account.AcceptInviteForExistingUser(token, existingUser);
      Assert.Succeeded(result);

      var (user, org) = result.Value;
      Assert.Equal(existingUser.Id, user.Id);
      Assert.Equal(existingUser.Name, user.Name);
      Assert.Equal(existingUser.Email, user.Email);
      Assert.Equal(existingUser.TimeZone, user.TimeZone);
      Assert.Equal(existingUser.Locale, user.Locale);
      Assert.Equal(existingUser.CreatedOn, user.CreatedOn);
      Assert.Equal(existingUser.UpdatedOn, user.UpdatedOn);
      Assert.Equal(newOrg, org);

      Assert.Present(user.Organizations);
      Assert.True(user.BelongsTo(existingOrg));
      Assert.True(user.BelongsTo(newOrg));

      var reloadToken = test.App.Account.GetInvite(token.Value!);
      Assert.Null(reloadToken);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestAcceptInviteForExistingUserAlreadyAMember()
  {
    using (var test = new DomainTest(this))
    {
      var existingOrg = test.Factory.LoadOrganization("atari");
      var existingUser = test.Factory.LoadUser("active");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: existingOrg, sentTo: existingUser.Email);

      existingUser = test.App.Account.WithOrganizations(existingUser);
      Assert.True(existingUser.BelongsTo(existingOrg));

      var result = test.App.Account.AcceptInviteForExistingUser(token, existingUser);
      Assert.Succeeded(result);

      var (user, org) = result.Value;
      Assert.Equal(existingUser.Id, user.Id);
      Assert.Equal(existingOrg.Id, org.Id);
      Assert.True(user.BelongsTo(existingOrg));

      var reloadToken = test.App.Account.GetInvite(token.Value!);
      Assert.Null(reloadToken);
    }
  }

  //-----------------------------------------------------------------------------------------------
}