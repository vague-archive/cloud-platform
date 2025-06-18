namespace Void.Platform.Web;

public class JoinPageTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestJoinPageAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var email = Fake.Email();
      var org = test.Factory.LoadOrganization("atari");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: email);

      var response = await test.Get($"/join/{token.Value}");
      var doc = Assert.Html.Document(response, expectedTitle: "Join Organization");
      var card = Assert.Html.Select("card[qa=anonymous]", doc);
      var github = Assert.Html.Select("form[qa=github]", card);
      var discord = Assert.Html.Select("form[qa=discord]", card);

      Assert.Equal("/join/github/signup", github.GetAttribute("hx-post"));
      Assert.Equal(token.Value, github.QuerySelector("input[name=token]")?.GetAttribute("value"));

      Assert.Equal("/join/discord/signup", discord.GetAttribute("hx-post"));
      Assert.Equal(token.Value, discord.QuerySelector("input[name=token]")?.GetAttribute("value"));

      Assert.Equal(org.Name, card.QuerySelector("[qa=org-name]")?.TextContent);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestJoinPageLoggedIn()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("outsider");
      var org = test.Factory.LoadOrganization("atari");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: user.Email);
      var response = await test.Get($"/join/{token.Value}");
      var doc = Assert.Html.Document(response, expectedTitle: "Join Organization");
      var form = Assert.Html.Select("form[qa=loggedin]", doc);
      Assert.Equal($"/join/{token.Value}?handler=accept", form.GetAttribute("hx-post"));
      Assert.Equal(user.Name, form.QuerySelector("[qa=user-name]")?.TextContent);
      Assert.Equal(org.Name, form.QuerySelector("[qa=org-name]")?.TextContent);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestJoinPageInviteUnknown()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Get($"/join/unknown-token-value");
      var doc = Assert.Html.Document(response, expectedTitle: "Invite Unavailable");
      var card = Assert.Html.Select("card[qa=unavailable]", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestJoinPageInviteAlreadySpent()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var email = Fake.Email();
      var org = test.Factory.LoadOrganization("atari");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, isSpent: true, org: org, sentTo: email);
      var response = await test.Get($"/join/{token.Value}");
      var doc = Assert.Html.Document(response, expectedTitle: "Invite Unavailable");
      var card = Assert.Html.Select("card[qa=unavailable]", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestJoinPageAccept()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("outsider");
      var org = test.Factory.LoadOrganization("atari");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: user.Email);

      var orgs = test.App.Account.GetUserOrganizations(user.Id);
      Assert.Preconditions.Absent(orgs.Find(o => o.Id == org.Id));

      var response = await test.HxPost($"/join/{token.Value}?handler=accept", redirect: true);
      var doc = Assert.Html.Document(response,
        expectedUrl: "https://localhost/atari/games",
        expectedTitle: "Games - Atari");

      orgs = test.App.Account.GetUserOrganizations(user.Id);
      Assert.NotNull(orgs.Find(o => o.Id == org.Id));
    }
  }

  [Fact]
  public async Task TestJoinPageAcceptNotLoggedIn()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var email = Fake.Email();
      var org = test.Factory.LoadOrganization("atari");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, org: org, sentTo: email);
      var response = await test.HxPost($"/join/{token.Value}?handler=accept", redirect: true);
      var doc = Assert.Html.Document(response,
        expectedUrl: $"https://localhost/join/{token.Value}",
        expectedTitle: "Join Organization");
      Assert.Html.Flash("User must be logged in", doc);
    }
  }

  [Fact]
  public async Task TestJoinPageAcceptInviteSpent()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("outsider");
      var org = test.Factory.LoadOrganization("atari");
      var token = test.Factory.CreateToken(type: Account.TokenType.Invite, isSpent: true, org: org, sentTo: user.Email);
      var response = await test.HxPost($"/join/{token.Value}?handler=accept", redirect: true);
      var doc = Assert.Html.Document(response,
        expectedUrl: $"https://localhost/join/{token.Value}",
        expectedTitle: "Invite Unavailable");
      Assert.Html.NoFlash(doc);
    }
  }

  //-----------------------------------------------------------------------------------------------
}