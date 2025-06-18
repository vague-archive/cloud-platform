namespace Void.Platform.Web;

using System.Text.Json;

public interface ISerializer
{
  string Serialize<T>(T? item);
  object? AsJson<T>(T item);
}

public class ApiSerializer : ISerializer
{
  //-----------------------------------------------------------------------------------------------

  private JsonSerializerOptions Options;

  public ApiSerializer(JsonSerializerOptions? opts = null)
  {
    Options = opts ?? Json.SerializerOptions;
  }

  //-----------------------------------------------------------------------------------------------

  public string Serialize<T>(T? item)
  {
    return Json.Serialize(AsJson(item), Options);
  }

  public object? AsJson<T>(T item)
  {
    return item switch
    {
      Account.Identity identity => Object(identity),
      Account.UserRole role => Enum(role),
      Account.Organization org => Object(org),
      Account.AuthenticatedUser user => Object(user),
      Account.User user => Object(user),
      object other => other,
      null => null,
    };
  }

  //-----------------------------------------------------------------------------------------------

  private object Object(Account.Identity identity)
  {
    return new
    {
      id = identity.Id,
      userId = identity.UserId,
      provider = Enum(identity.Provider),
      identifier = identity.Identifier,
      userName = identity.UserName
    };
  }

  private object Object(Account.Organization org)
  {
    return new
    {
      id = org.Id,
      name = org.Name,
      slug = org.Slug
    };
  }

  private object Object(Account.AuthenticatedUser user)
  {
    return new
    {
      id = user.Id,
      name = user.Name,
      email = user.Email,
      timeZone = user.TimeZone,
      locale = user.Locale,
      roles = Association(user.Roles),
      identities = Association(user.Identities),
      organizations = Association(user.Organizations),
      authenticatedOn = user.AuthenticatedOn,
    };
  }

  private object Object(Account.User user)
  {
    return new
    {
      id = user.Id,
      name = user.Name,
      email = user.Email,
      timeZone = user.TimeZone,
      locale = user.Locale,
      roles = Association(user.Roles),
      identities = Association(user.Identities),
      organizations = Association(user.Organizations),
    };
  }

  //-----------------------------------------------------------------------------------------------

  private object[]? Association<T>(IEnumerable<T>? items)
  {
    if (items is null)
      return null;
    else
      return items.Select(item => RuntimeAssert.Present(AsJson(item))).ToArray();
  }

  private object Enum<T>(T value) where T : Enum
  {
    return value.ToString().ToLower();
  }

  //-----------------------------------------------------------------------------------------------
}