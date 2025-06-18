namespace Void.Platform.Lib;

using System.Net.Http.Headers;

public class DiscordApi
{
  //-----------------------------------------------------------------------------------------------

  public record User
  {
    public required string Id { get; set; }
    public required string UserName { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
  }

  //-----------------------------------------------------------------------------------------------

  public static readonly Uri Endpoint = new Uri("https://discord.com/api/v10");

  //-----------------------------------------------------------------------------------------------

  HttpClient Client;
  string Token;

  public DiscordApi(HttpClient client, string token)
  {
    Client = client;
    Token = token;
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<User> GetCurrentUser()
  {
    using var json = Json.Parse(await Get("/users/@me"));
    var id = json.RequiredString("id");
    var username = json.RequiredString("username");
    var globalName = json.OptionalString("global_name");
    var email = json.OptionalString("email");
    return new User
    {
      Id = id,
      UserName = username,
      Name = globalName,
      Email = email,
    };
  }

  //-----------------------------------------------------------------------------------------------

  private async Task<string> Get(string path)
  {
    var url = new Uri($"{Endpoint}{path}");
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

    var response = await Client.SendAsync(request);
    response.EnsureSuccessStatusCode();
    RuntimeAssert.True(response.Content.Headers.ContentType?.MediaType == "application/json");
    var content = await response.Content.ReadAsStringAsync();
    return content;
  }

  //-----------------------------------------------------------------------------------------------
}