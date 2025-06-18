namespace Void.Platform.Lib;

public class DiscordApiTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetCurrentUser()
  {
    var token = "SECRETZ";
    var handler = new MockHttpMessageHandler();
    handler.When("https://discord.com/api/v10/users/@me")
      .WithHeaders("Authorization", $"Bearer {token}")
      .Respond("application/json", Json.Serialize(new
      {
        id = "123-456",
        username = "jakesgordon",
      }));
    var client = handler.ToHttpClient();
    var api = new DiscordApi(client, token);

    var user = await api.GetCurrentUser();

    Assert.NotNull(user);
    Assert.Equal("123-456", user.Id);
    Assert.Equal("jakesgordon", user.UserName);
    Assert.Null(user.Name);
    Assert.Null(user.Email);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetCurrentUserWithFullNameAndEmail()
  {
    var token = "SECRETZ";
    var handler = new MockHttpMessageHandler();
    handler.When("https://discord.com/api/v10/users/@me")
      .WithHeaders("Authorization", $"Bearer {token}")
      .Respond("application/json", Json.Serialize(new
      {
        id = "123-456",
        username = "jakesgordon",
        global_name = "Jake Gordon",
        email = "jake@void.dev",
      }));
    var client = handler.ToHttpClient();
    var api = new DiscordApi(client, token);

    var user = await api.GetCurrentUser();

    Assert.NotNull(user);
    Assert.Equal("123-456", user.Id);
    Assert.Equal("jakesgordon", user.UserName);
    Assert.Equal("Jake Gordon", user.Name);
    Assert.Equal("jake@void.dev", user.Email);
  }

  //-----------------------------------------------------------------------------------------------
}