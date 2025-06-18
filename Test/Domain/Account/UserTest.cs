namespace Void.Platform.Domain;

public class UserTest : TestCase
{
  //===============================================================================================
  // TEST Get Account.User By ...
  //===============================================================================================

  [Fact]
  public void TestGetUserById()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var reloaded = test.App.Account.GetUserById(user.Id);
      Assert.NotNull(reloaded);
      Assert.Domain.Equal(user, reloaded);
      Assert.Null(reloaded.Password); // password digest is NOT loaded
    }
  }

  [Fact]
  public void TestGetUserByIdNotFound()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.App.Account.GetUserById(42);
      Assert.Null(user);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetUserByEmail()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.CreateUser();
      var reloaded = test.App.Account.GetUserByEmail(user.Email);
      Assert.NotNull(reloaded);
      Assert.Domain.Equal(user, reloaded);
      Assert.Null(reloaded.Password); // password digest is NOT loaded
    }
  }

  [Fact]
  public void TestGetUserByEmailNotFound()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.App.Account.GetUserByEmail("unknown@example.com");
      Assert.Null(user);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetUserByIdentity()
  {
    using (var test = new DomainTest(this))
    {
      var expected = test.Factory.LoadUser("active");
      var user = test.App.Account.GetUserByIdentity(Account.IdentityProvider.GitHub, "active");
      Assert.NotNull(user);
      Assert.Domain.Equal(expected, user);
      Assert.Null(user.Password);

      user = test.App.Account.GetUserByIdentity(Account.IdentityProvider.Discord, "active");
      Assert.Null(user); // active user doesn't have a discord identity
    }
  }

  [Fact]
  public void TestGetUserByIdentityNotFound()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.App.Account.GetUserByIdentity(Account.IdentityProvider.Discord, "unknown");
      Assert.Null(user);
      user = test.App.Account.GetUserByIdentity(Account.IdentityProvider.GitHub, "unknown");
      Assert.Null(user);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetUserByAccessToken()
  {
    using (var test = new DomainTest(this))
    {
      var user1 = test.Factory.LoadUser("active");
      var user2 = test.Factory.LoadUser("other");

      var token1 = Crypto.GenerateToken("active");
      var token2 = Crypto.GenerateToken("other");
      var token3 = Crypto.GenerateToken("unknown");

      var user = test.App.Account.GetUserByAccessToken(token1);

      Assert.NotNull(user);
      Assert.Domain.Equal(user1, user);

      user = test.App.Account.GetUserByAccessToken(token2);
      Assert.NotNull(user);
      Assert.Domain.Equal(user2, user);

      user = test.App.Account.GetUserByAccessToken(token3);
      Assert.Null(user);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetUsers()
  {
    using (var test = new DomainTest(this))
    {
      var id1 = Identify("active");
      var id2 = Identify("other");
      var id3 = Identify("unknown");

      var userIds = new long[] { id1, id2, id3 };
      var users = test.App.Account.GetUsers(userIds);
      var user1 = users.GetValueOrDefault(id1);
      var user2 = users.GetValueOrDefault(id2);
      var user3 = users.GetValueOrDefault(id3);
      Assert.Present(user1);
      Assert.Present(user2);
      Assert.Absent(user3);
      Assert.Equal("Active User", user1.Name);
      Assert.Equal("Other User", user2.Name);
    }
  }

  //===============================================================================================
  // TEST Account.User Associations
  //===============================================================================================

  [Fact]
  public void TestUserWithRoles()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("sysadmin");
      Assert.Absent(user.Roles);

      test.App.Account.WithRoles(user);

      Assert.Present(user.Roles);
      Assert.Equal([Account.UserRole.SysAdmin], user.Roles);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUserWithIdentities()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("jake");
      Assert.Absent(user.Identities);

      test.App.Account.WithIdentities(user);

      Assert.Present(user.Identities);
      Assert.Equal([
        "github:jakesgordon",
        "discord:jakesgordon",
      ], user.Identities.Select(i => i.ToString()));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUserWithOrganizations()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("floater");
      Assert.Absent(user.Organizations);
      user = test.App.Account.WithOrganizations(user);
      Assert.Present(user.Organizations);
      Assert.Equal([
        "Atari",
        "Nintendo",
        "Void",
      ], user.Organizations.Select(u => u.Name));
      var atari = test.Factory.LoadOrganization("atari");
      Assert.Domain.Equal(atari, user.Organizations[0]);
    }
  }

  //===============================================================================================
  // TEST Account.DisconnectMember
  //===============================================================================================

  [Fact]
  public void TestDisconnectMember()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("floater");
      var org = test.Factory.LoadOrganization("atari");

      var userOrgs = test.App.Account.GetUserOrganizations(user);
      Assert.Equal([
        "Atari",
        "Nintendo",
        "Void",
      ], userOrgs.Select(o => o.Name));

      test.App.Account.DisconnectMember(org, user);

      userOrgs = test.App.Account.GetUserOrganizations(user);
      Assert.Equal([
        "Nintendo",
        "Void",
      ], userOrgs.Select(o => o.Name));
    }
  }

  //===============================================================================================
  // TEST Account.UpdateProfileCommand
  //===============================================================================================

  [Fact]
  public void TestUpdateProfile()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var newName = "New Name";
      var newTimeZone = "Europe/Paris";
      var newLocale = "en-GB";

      var result = test.App.Account.UpdateProfile(user, newName, newTimeZone, newLocale);
      Assert.True(result.Succeeded);

      Assert.Equal(newName, user.Name);
      Assert.Equal(newTimeZone, user.TimeZone);
      Assert.Equal(newLocale, user.Locale);
      Assert.Equal(Clock.Now, user.UpdatedOn);

      var reloaded = test.App.Account.GetUserById(user.Id);
      Assert.NotNull(reloaded);
      Assert.Equal(newName, reloaded.Name);
      Assert.Equal(newTimeZone, reloaded.TimeZone);
      Assert.Equal(newLocale, reloaded.Locale);
      Assert.Equal(Clock.Now, reloaded.UpdatedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUpdateProfileMissingFields()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var result = test.App.Account.UpdateProfile(user, "", "", "");
      var errors = Assert.FailedValidation(result);
      Assert.Equal([
        "name is missing",
        "timezone is missing",
        "locale is missing",
      ], errors.Select(e => e.Format()));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUpdateProfileNameTooLong()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var newName = "this name is far too long to fit in our basic varchar(255) field and so a validation error should occur in order to avoid triggering a database mysql error which would otherwise throw an exception and cause a 500 internal server error to be shown to the user";
      var newTimeZone = "Europe/Rome";
      var newLocale = "en-GB";
      var result = test.App.Account.UpdateProfile(user, newName, newTimeZone, newLocale);
      var errors = Assert.FailedValidation(result);
      Assert.Equal([
        "name must be less than 255 characters",
      ], errors.Select(e => e.Format()));
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUpdateProfileInvalidTimeZoneAndLocale()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var newName = "Bob";
      var newTimeZone = "not a valid timezone";
      var newLocale = "not a valid locale";
      var result = test.App.Account.UpdateProfile(user, newName, newTimeZone, newLocale);
      var errors = Assert.FailedValidation(result);
      Assert.Equal([
        "timezone is not a valid timezone",
        "locale is not a valid locale",
      ], errors.Select(e => e.Format()));
    }
  }

  //===============================================================================================
  // TEST Account.LoginCommand
  //===============================================================================================

  [Fact]
  public void TestLogin()
  {
    using (var test = new DomainTest(this))
    {
      var result = test.App.Account.Login("active@example.com", "password");

      Assert.True(result.Succeeded);

      var user = result.Value;

      Assert.NotNull(user);
      Assert.Equal(Identify("active"), user.Id);
      Assert.Equal("Active User", user.Name);
      Assert.Equal("active@example.com", user.Email);
      Assert.Equal("America/Los_Angeles", user.TimeZone);
      Assert.Equal("en-US", user.Locale);
      Assert.Present(user.Roles);
      Assert.Present(user.Identities);
      Assert.Present(user.Organizations);
      Assert.Empty(user.Roles);
      Assert.Equal(["github:active"], user.Identities.Select(i => i.ToString()));
      Assert.Equal(["atari"], user.Organizations.Select(o => o.Slug));
      Assert.Equal(Clock.Now, user.AuthenticatedOn);
    }
  }

  [Fact]
  public void TestLoginSysAdminInMultipleOrganizations()
  {
    using (var test = new DomainTest(this))
    {
      var result = test.App.Account.Login("jake@void.dev", "password");

      Assert.True(result.Succeeded);

      var user = result.Value;

      Assert.NotNull(user);
      Assert.Equal(Identify("jake"), user.Id);
      Assert.Equal("Jake Gordon", user.Name);
      Assert.Equal("jake@void.dev", user.Email);
      Assert.Equal("America/Los_Angeles", user.TimeZone);
      Assert.Equal("en-US", user.Locale);
      Assert.Present(user.Roles);
      Assert.Present(user.Identities);
      Assert.Present(user.Organizations);
      Assert.Equal([Account.UserRole.SysAdmin], user.Roles);
      Assert.Equal(["github:jakesgordon", "discord:jakesgordon"], user.Identities.Select(i => i.ToString()));
      Assert.Equal(["atari", "nintendo", "void"], user.Organizations.Select(o => o.Slug));
    }
  }

  [Fact]
  public void TestDisabledUserCannotLogin()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.Factory.LoadUser("disabled");
      var result = test.App.Account.Login(user.Email, "password");
      var errors = Assert.FailedValidation(result);
      Assert.Equal([
        "email is disabled"
      ], errors.Select(e => e.Format()));
    }
  }

  [Fact]
  public void TestLoginValidationErrors()
  {
    using (var test = new DomainTest(this))
    {
      var result = test.App.Account.Login("", "");
      var errors = Assert.FailedValidation(result);
      Assert.Equal([
        "email is missing",
        "email is invalid",
        "password is missing",
      ], errors.Select(e => e.Format()));

      result = test.App.Account.Login("active@example.com", "invalid");
      errors = Assert.FailedValidation(result);
      Assert.Equal("password is invalid", errors.Format());

      result = test.App.Account.Login("unknown@example.com", "password");
      errors = Assert.FailedValidation(result);
      Assert.Equal("email not found", errors.Format());
    }
  }

  //===============================================================================================
  // TEST Account.GetAuthenticatedUser()
  //===============================================================================================

  [Fact]
  public void TestGetAuthenticatedUser()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.App.Account.GetAuthenticatedUser(Identify("active"));
      Assert.NotNull(user);
      Assert.Equal(Identify("active"), user.Id);
      Assert.Equal("Active User", user.Name);
      Assert.Equal("active@example.com", user.Email);
      Assert.Equal("America/Los_Angeles", user.TimeZone);
      Assert.Equal("en-US", user.Locale);
      Assert.Present(user.Roles);
      Assert.Present(user.Identities);
      Assert.Present(user.Organizations);
      Assert.Empty(user.Roles);
      Assert.Equal(["github:active"], user.Identities.Select(i => i.ToString()));
      Assert.Equal(["atari"], user.Organizations.Select(o => o.Slug));
      Assert.Equal(Clock.Now, user.AuthenticatedOn);
    }
  }

  [Fact]
  public void TestGetAuthenticatedUserUnknown()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.App.Account.GetAuthenticatedUser(Identify("unknown"));
      Assert.Null(user);
    }
  }

  [Fact]
  public void TestGetAuthenticatedUserDisabled()
  {
    using (var test = new DomainTest(this))
    {
      var user = test.App.Account.GetAuthenticatedUser(Identify("disabled"));
      Assert.Null(user);
    }
  }

  [Fact]
  public void TestGetAuthenticatedUserByProvider()
  {
    using (var test = new DomainTest(this))
    {
      var expected = test.App.Account.GetAuthenticatedUser(Identify("active"));
      var actual = test.App.Account.GetAuthenticatedUser(Account.IdentityProvider.GitHub, "active");
      Assert.NotNull(expected);
      Assert.NotNull(actual);
      Assert.Domain.Equal(expected, actual);
    }
  }

  [Fact]
  public void TestAuthenticatedUserByAccessToken()
  {
    using (var test = new DomainTest(this))
    {
      var expected = test.App.Account.GetAuthenticatedUser(Identify("active"));
      var actual = test.App.Account.GetAuthenticatedUser(Crypto.GenerateToken("active"));
      Assert.NotNull(expected);
      Assert.NotNull(actual);
      Assert.Domain.Equal(expected, actual);
    }
  }

  //===============================================================================================
  // TEST DATABASE CONSTRAINTS
  //===============================================================================================

  [Fact]
  public void TestUserEmailMustBeUnique()
  {
    using (var test = new DomainTest(this))
    {
      var first = test.Factory.CreateUser(email: "first@example.com");
      var second = test.Factory.CreateUser(email: "second@example.com");

      var ex = Assert.Throws<MySqlConnector.MySqlException>(() => test.Factory.CreateUser(email: "first@example.com"));
      Assert.Equal("Duplicate entry 'first@example.com' for key 'users.email'", ex.Message);

      ex = Assert.Throws<MySqlConnector.MySqlException>(() => test.Factory.CreateUser(email: "FIRST@EXAMPLE.COM"));
      Assert.Equal("Duplicate entry 'FIRST@EXAMPLE.COM' for key 'users.email'", ex.Message);
    }
  }

  //-----------------------------------------------------------------------------------------------
}