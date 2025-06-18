namespace Void.Platform.Web;

public class OrganizationTeamPageTest : TestCase
{
  //===============================================================================================
  // TEST OnGet
  //===============================================================================================

  [Fact]
  public async Task TestGetPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");

      var response = await test.Get("/atari/team");
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/atari/team",
          expectedTitle: "Team - Atari");

      var members = Assert.Html.SelectAll("[qa=team] li [qa=name]", doc);
      var invites = Assert.Html.SelectAll("[qa=invites] li [qa=sent-to]", doc);

      Assert.Equal(7, members.Count);
      Assert.Equal("Active User", members[0].TextContent);
      Assert.Equal("Disabled User", members[1].TextContent);
      Assert.Equal("Jake Gordon", members[2].TextContent);
      Assert.Equal("Nolan Bushnell", members[3].TextContent);
      Assert.Equal("Scarlett Blaiddyd", members[4].TextContent);
      Assert.Equal("Sysadmin User", members[5].TextContent);
      Assert.Equal("The Floater", members[6].TextContent);

      Assert.Equal(2, invites.Count);
      Assert.Equal("contractor@agency.com", invites[0].TextContent);
      Assert.Equal("pending@member.com", invites[1].TextContent);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageUnknownOrg()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/unknown/team");
      var doc = Assert.Html.Document(response, expectedStatus: Http.StatusCode.NotFound);
      Assert.Html.Title("Page Not Found", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact()]
  public async Task TestGetPageUnauthorized()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("outsider");
      var response = await test.Get("/atari/team");
      var doc = Assert.Html.Document(response, expectedStatus: Http.StatusCode.NotFound);
      Assert.Html.Title("Page Not Found", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Get("/atari/team");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fatari%2Fteam", location);
    }
  }

  //===============================================================================================
  // TEST OnPostDisconnectMember
  //===============================================================================================

  [Fact]
  public async Task TestDisconnectMember()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");

      var form = test.BuildForm(new Dictionary<string, string>
      {
        {"userId", Identify("jake").ToString()},
      });

      var response = await test.HxPost("/atari/team?handler=DisconnectMember", form);
      var doc = Assert.Html.Partial(response);

      var names = Assert.Html.SelectAll("[qa=team] li [qa=name]", doc);

      Assert.Equal(6, names.Count);
      Assert.Equal("Active User", names[0].TextContent);
      Assert.Equal("Disabled User", names[1].TextContent);
      Assert.Equal("Nolan Bushnell", names[2].TextContent);
      Assert.Equal("Scarlett Blaiddyd", names[3].TextContent);
      Assert.Equal("Sysadmin User", names[4].TextContent);
      Assert.Equal("The Floater", names[5].TextContent);
    }
  }

  //===============================================================================================
  // TEST OnPostSendInvite
  //===============================================================================================

  [Fact]
  public async Task TestSendInvite()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");

      var email = "another@member.com";
      var form = test.BuildForm(new Dictionary<string, string>
      {
        {"email", email},
      });

      var response = await test.HxPost("/atari/team?handler=SendInvite", form);
      var doc = Assert.Html.Partial(response);

      var invites = Assert.Html.SelectAll("[qa=invites] li [qa=sent-to]", doc);

      Assert.Equal(3, invites.Count);
      Assert.Equal("another@member.com", invites[0].TextContent);
      Assert.Equal("contractor@agency.com", invites[1].TextContent);
      Assert.Equal("pending@member.com", invites[2].TextContent);

      var mail = Assert.Mailed(test.Mailer, "invite", email);
      Assert.Equal($"Atari", mail.Data["organization"]);
      Assert.StartsWith($"https://void.test/join/", mail.Data["action_url"].ToString());
    }
  }

  //===============================================================================================
  // TEST OnPostRetractInvite
  //===============================================================================================

  [Fact]
  public async Task TestRetractInvite()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");

      var form = test.BuildForm(new Dictionary<string, string>
      {
        {"inviteId", Identify("invite:contractor").ToString()},
      });

      var response = await test.HxPost("/atari/team?handler=RetractInvite", form);
      var doc = Assert.Html.Partial(response);
      var invites = Assert.Html.SelectAll("[qa=invites] li [qa=sent-to]", doc);

      Assert.Single(invites);
      Assert.Equal("pending@member.com", invites[0].TextContent);
    }
  }

  //-----------------------------------------------------------------------------------------------
}