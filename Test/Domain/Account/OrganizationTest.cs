namespace Void.Platform.Domain;

public class OrganizationTest : TestCase
{
  //===============================================================================================
  // TEST Get Account.Organization By...
  //===============================================================================================

  [Fact]
  public void TestGetOrganization()
  {
    using var test = new DomainTest(this);
    {
      var org = test.Factory.CreateOrganization();
      var reloaded = test.App.Account.GetOrganization(org.Id);
      Assert.NotNull(reloaded);
      Assert.Domain.Equal(org, reloaded);
    }
  }

  [Fact]
  public void TestGetOrganizationNotFound()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.App.Account.GetOrganization(42);
      Assert.Null(org);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetOrganizationBySlug()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.CreateOrganization();
      var reloaded = test.App.Account.GetOrganization(org.Slug);
      Assert.NotNull(reloaded);
      Assert.Domain.Equal(org, reloaded);
    }
  }

  [Fact]
  public void TestGetOrganizationBySlugNotFound()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.App.Account.GetOrganization("no-such-organization");
      Assert.Null(org);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetOrganizations()
  {
    using (var test = new DomainTest(this))
    {
      var id1 = Identify("void");
      var id2 = Identify("atari");
      var id3 = Identify("unknown");

      var orgIds = new long[] { id1, id2, id3 };
      var orgs = test.App.Account.GetOrganizations(orgIds);
      var org1 = orgs.GetValueOrDefault(id1);
      var org2 = orgs.GetValueOrDefault(id2);
      var org3 = orgs.GetValueOrDefault(id3);
      Assert.Present(org1);
      Assert.Present(org2);
      Assert.Absent(org3);
      Assert.Equal("Void", org1.Name);
      Assert.Equal("Atari", org2.Name);
    }
  }

  //===============================================================================================
  // TEST Organization Associations
  //===============================================================================================

  [Fact]
  public void TestGetOrganizationMembers()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("atari");

      var members = test.App.Account.GetOrganizationMembers(org);
      Assert.Equal(7, members.Count);

      Assert.Equal([
        "Active User",
        "Disabled User",
        "Jake Gordon",
        "Nolan Bushnell",
        "Scarlett Blaiddyd",
        "Sysadmin User",
        "The Floater",
      ], members.Select(m => m.User!.Name));

      Assert.Equal([
        "active@example.com",
        "disabled@example.com",
        "jake@void.dev",
        "bushnell@atari.com",
        "scarlett@void.dev",
        "sysadmin@example.com",
        "floater@unknown.com",
      ], members.Select(m => m.User!.Email));

      Assert.Equal([
        [],
        [],
        [Account.UserRole.SysAdmin],
        [],
        [Account.UserRole.SysAdmin],
        [Account.UserRole.SysAdmin],
        [],
      ], members.Select(m => m.User!.Roles));

      Assert.Equal([
        ["github:active"],
        ["github:disabled"],
        ["github:jakesgordon", "discord:jakesgordon"],
        ["github:bushnell"],
        ["github:scarlettblaiddyd"],
        ["github:sysadmin"],
        ["github:floater"],
      ], members.Select(m => m.User!.Identities!.Select(i => i.ToString())));

      var member = members.First();

      var activeUser = test.Factory.LoadUser("active");
      var activeMember = test.Factory.LoadMember("atari", "active");

      Assert.Domain.Equal(activeMember, member);
      Assert.Domain.Equal(activeUser, member.User!);
    }
  }

  //===============================================================================================
  // TEST Account.UpdateOrganizationCommand
  //===============================================================================================

  [Fact]
  public void TestUpdateOrganization()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("atari");
      var newName = "Hasbro";
      var newSlug = "hasbro";

      var result = test.App.Account.UpdateOrganization(org, newName, newSlug);
      Assert.True(result.Succeeded);

      Assert.Equal(newName, org.Name);
      Assert.Equal(newSlug, org.Slug);
      Assert.Equal(Clock.Now, org.UpdatedOn);

      var reloaded = test.App.Account.GetOrganization(org.Id);
      Assert.NotNull(reloaded);
      Assert.Equal(newName, reloaded.Name);
      Assert.Equal(newSlug, reloaded.Slug);

      Assert.Equal(Clock.Now, reloaded.UpdatedOn);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUpdateOrganizationMissingFields()
  {
    using (var test = new DomainTest(this))
    {
      var org = test.Factory.LoadOrganization("atari");
      var result = test.App.Account.UpdateOrganization(org, "", "");
      var errors = Assert.FailedValidation(result);
      Assert.Equal([
        "name is missing",
        "slug is missing",
      ], errors.Select(e => e.Format()));
    }
  }

  [Fact]
  public void TestUpdateOrganizationFieldsTooLong()
  {
    using (var test = new DomainTest(this))
    {
      var name = new string('n', 256);
      var slug = new string('s', 256);
      var org = test.Factory.LoadOrganization("atari");
      var result = test.App.Account.UpdateOrganization(org, name, slug);
      var errors = Assert.FailedValidation(result);
      Assert.Equal([
        "name must be less than 255 characters",
        "slug must be less than 255 characters",
      ], errors.Select(e => e.Format()));
    }
  }

  [Fact]
  public void TestUpdateOrganizationSlugAlreadyExists()
  {
    using (var test = new DomainTest(this))
    {
      var org1 = test.Factory.LoadOrganization("void");
      var org2 = test.Factory.LoadOrganization("atari");
      var result = test.App.Account.UpdateOrganization(org1, org2.Name, org2.Slug);
      var errors = Assert.FailedValidation(result);
      Assert.Single(errors);
      var error = Assert.Present(errors[0]);
      Assert.Equal("Slug", error.Property);
      Assert.Equal("is already in use", error.Message);
      Assert.Equal("slug is already in use", error.Format());
    }
  }

  //===============================================================================================
  // TEST DATABASE CONSTRAINTS
  //===============================================================================================

  [Fact]
  public void TestOrganizationSlugMustBeUnique()
  {
    using (var test = new DomainTest(this))
    {
      var first = test.Factory.CreateOrganization(slug: "first");
      var second = test.Factory.CreateOrganization(slug: "second");

      var ex = Assert.Throws<MySqlConnector.MySqlException>(() => test.Factory.CreateOrganization(slug: "first"));
      Assert.Equal("Duplicate entry 'first' for key 'organizations.organizations_slug'", ex.Message);

      ex = Assert.Throws<MySqlConnector.MySqlException>(() => test.Factory.CreateOrganization(slug: "FIRST"));
      Assert.Equal("Duplicate entry 'FIRST' for key 'organizations.organizations_slug'", ex.Message);
    }
  }

  //-----------------------------------------------------------------------------------------------
}