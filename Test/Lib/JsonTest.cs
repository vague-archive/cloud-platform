namespace Void.Platform.Lib;

public class JsonTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  public record Example
  {
    public required string Name { get; set; }
    public required long Size { get; set; }
    public required Instant Date { get; set; }
    public string? Description { get; set; }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestSerialize()
  {
    var example = new Example
    {
      Name = "Thing",
      Size = 42,
      Date = Instant.FromUtc(2025, 1, 14, 9, 56, 30),
    };

    var json = Json.Serialize(example);
    Assert.Equal(@"{""name"":""Thing"",""size"":42,""date"":""2025-01-14T09:56:30Z""}", Json.Serialize(example));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestDeserialize()
  {
    var json = @"
    {
      ""name"": ""Other Thing"",
      ""SIZE"": 100,
      ""DaTe"": ""2025-07-09T10:20:30Z"",
      ""description"": ""yolo""
    }
    ";

    var example = Json.Deserialize<Example>(json);
    Assert.Equal("Other Thing", example.Name);
    Assert.Equal(100, example.Size);
    Assert.Equal("2025-07-09T10:20:30Z", example.Date.ToIso8601());
    Assert.Equal("yolo", example.Description);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestParse()
  {
    var content = @"
    {
      ""name"": ""John Doe"",
      ""age"": 42,
      ""description"": null,
      ""true"": true,
      ""false"": false,
      ""birthday"": ""2024-07-09T12:30:45.123Z""
    }";

    using var json = Json.Parse(content);
    Assert.Equal("John Doe", json.RequiredString("name"));
    Assert.Equal(42, json.RequiredLong("age"));
    Assert.Null(json.OptionalString("description"));
    Assert.Null(json.OptionalString("missing"));
    Assert.True(json.OptionalBool("true"));
    Assert.False(json.OptionalBool("false"));
    Assert.Equal(Moment.FromIso8601("2024-07-09T12:30:45.123Z"), json.RequiredInstant("birthday"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestIsKind()
  {
    using var obj = Json.Parse("{}");
    using var arr = Json.Parse("[]");

    Assert.True(obj.RootElement.IsObject());
    Assert.False(arr.RootElement.IsObject());

    Assert.False(obj.RootElement.IsArray());
    Assert.True(arr.RootElement.IsArray());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestPropertyNames()
  {
    using var empty = Json.Parse("{}");
    Assert.Empty(empty.RootElement.GetPropertyNames());

    using var obj = Json.Parse("{\"foo\": 1, \"bar\": 2}");
    Assert.Equal([
      "foo",
      "bar",
    ], obj.RootElement.GetPropertyNames());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestRedactSensitiveValues()
  {
    var values = new
    {
      foo = "foo",
      bar = "bar",
      password = "password",
      digest = "digest",
      csrf = "csrf",
      _csrf_token = "csrf",
      secret = "secret",
      secret_thing = "secret thing",
      token = "token",
      magic_token = "token",
      name = "name",
      safe = "safe thing",
      key = "secret key",
      myKey = "secret key",
      my_key = "secret key",
      databaseUrl = "might have a password",
      database_url = "might have a password",
    };

    var redacted = Json.Redact(values);

    Assert.Equal("foo", redacted["foo"]?.ToString());
    Assert.Equal("bar", redacted["bar"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["password"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["digest"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["csrf"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["_csrf_token"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["secret"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["secret_thing"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["token"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["magic_token"]?.ToString());
    Assert.Equal("name", redacted["name"]?.ToString());
    Assert.Equal("safe thing", redacted["safe"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["key"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["myKey"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["my_key"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["databaseUrl"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["database_url"]?.ToString());
  }

  [Fact]
  public void TestRedactInNestedObject()
  {
    var values = new
    {
      nested = new
      {
        safe = "safe",
        secret = "secret"
      }
    };

    var redacted = Json.Redact(values);

    Assert.Equal("safe", redacted["nested"]?["safe"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["nested"]?["secret"]?.ToString());
  }

  [Fact]
  public void TestRedactInNestedArray()
  {
    var values = new
    {
      nested = new[]
      {
        new { safe = "safe", secret = "secret" },
        new { safe = "safe", secret = "secret" },
        new { safe = "safe", secret = "secret" }
      }
    };

    var redacted = Json.Redact(values);

    Assert.Equal("safe", redacted["nested"]?[0]?["safe"]?.ToString());
    Assert.Equal("safe", redacted["nested"]?[1]?["safe"]?.ToString());
    Assert.Equal("safe", redacted["nested"]?[2]?["safe"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["nested"]?[0]?["secret"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["nested"]?[1]?["secret"]?.ToString());
    Assert.Equal("[REDACTED]", redacted["nested"]?[2]?["secret"]?.ToString());
  }

  //-----------------------------------------------------------------------------------------------
}