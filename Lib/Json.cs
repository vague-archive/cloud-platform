namespace Void.Platform.Lib;

using NodaTime.Serialization.SystemTextJson;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;

public static class Json
{
  //-----------------------------------------------------------------------------------------------

  public static readonly JsonSerializerOptions SerializerOptions = (new JsonSerializerOptions()).ConfigureForVoid();

  public static JsonSerializerOptions ConfigureForVoid(this JsonSerializerOptions options)
  {
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.PropertyNameCaseInsensitive = true;
    options.Converters.Add(new JsonStringEnumConverter());
    options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    return options;
  }

  //-----------------------------------------------------------------------------------------------

  public static string Serialize<T>(T input)
  {
    return Serialize(input, SerializerOptions);
  }

  public static string Serialize<T>(T input, JsonSerializerOptions options)
  {
    return JsonSerializer.Serialize(input, options);
  }

  //-----------------------------------------------------------------------------------------------

  public static T Deserialize<T>(string json)
  {
    return Deserialize<T>(json, SerializerOptions);
  }

  public static T Deserialize<T>(string json, JsonSerializerOptions options)
  {
    var value = JsonSerializer.Deserialize<T>(json, options);
    RuntimeAssert.Present(value);
    return value;
  }

  //-----------------------------------------------------------------------------------------------

  public static JsonDocument Parse(string json)
  {
    return JsonDocument.Parse(json); // IMPORTANT: JsonDocument is IDisposable so dont forget to .Dispose() of the document when done with it
  }

  public static string? OptionalString(this JsonDocument doc, string key)
  {
    return doc.RootElement.OptionalString(key);
  }

  public static bool OptionalBool(this JsonDocument doc, string key, bool defaultValue = false)
  {
    return doc.RootElement.OptionalBool(key);
  }

  public static string RequiredString(this JsonDocument doc, string key)
  {
    return doc.RootElement.RequiredString(key);
  }

  public static long RequiredLong(this JsonDocument doc, string key)
  {
    return doc.RootElement.RequiredLong(key);
  }

  public static Instant RequiredInstant(this JsonDocument doc, string key)
  {
    return doc.RootElement.RequiredInstant(key);
  }

  public static string? OptionalString(this JsonElement el, string key)
  {
    if (el.TryGetProperty(key, out var prop))
    {
      return prop.GetString();
    }
    return null;
  }

  public static bool OptionalBool(this JsonElement el, string key, bool defaultValue = false)
  {
    if (el.TryGetProperty(key, out var prop))
    {
      return prop.GetBoolean();
    }
    return defaultValue;
  }

  public static string RequiredString(this JsonElement el, string key)
  {
    var value = el.OptionalString(key);
    if (value is null)
      throw new Exception($"invalid key {key}");
    return value;
  }

  public static int RequiredInt(this JsonElement el, string key)
  {
    if (el.GetProperty(key).TryGetInt32(out var value))
    {
      return value;
    }
    else
    {
      throw new Exception($"invalid key {key}");
    }
  }

  public static long RequiredLong(this JsonElement el, string key)
  {
    if (el.GetProperty(key).TryGetInt64(out var value))
    {
      return value;
    }
    else
    {
      throw new Exception($"invalid key {key}");
    }
  }

  public static Instant RequiredInstant(this JsonElement el, string key)
  {
    return Moment.FromIso8601(el.RequiredString(key));
  }

  //-----------------------------------------------------------------------------------------------

  public static bool IsObject(this JsonElement el)
  {
    return el.ValueKind == JsonValueKind.Object;
  }

  public static bool IsArray(this JsonElement el)
  {
    return el.ValueKind == JsonValueKind.Array;
  }

  //-----------------------------------------------------------------------------------------------

  public static List<string> GetPropertyNames(this JsonElement element)
  {
    return element.EnumerateObject().Select(p => p.Name).ToList();
  }

  //-----------------------------------------------------------------------------------------------

  public static string[] DefaultSensitiveKeys = new string[]
  {
    "password",
    "digest",
    "csrf",
    "secret",
    "key",
    "token",
    "databaseurl",   // TODO: more surgical, should only redact the PASSWORD
    "database_url",  // TODO: ditto
    "redisCacheUrl", // TODO: ditto
  };

  public static string SerializeRedacted<T>(T input)
  {
    if (input is null)
      return "null";
    else
      return Redact(input).ToJsonString(SerializerOptions);
  }

  public static JsonNode Redact<T>(T input)
  {
    var text = Serialize(input, SerializerOptions);
    var node = JsonNode.Parse(text);
    RuntimeAssert.Present(node);
    var redactor = new Redactor(DefaultSensitiveKeys);
    return redactor.Redact(node);
  }

  private class Redactor
  {
    private HashSet<string> SensitiveKeys;
    private string RedactionValue;

    public Redactor(string[] sensitiveKeys, string redactionValue = "[REDACTED]")
    {
      SensitiveKeys = new HashSet<string>(sensitiveKeys.Select(k => k.ToLower()));
      RedactionValue = redactionValue;
    }

    private bool IsSensitive(string key)
    {
      var normalizedKey = key.ToLower();
      return SensitiveKeys.Any((k) => normalizedKey.Contains(k));
    }

    public JsonNode Redact(JsonNode node)
    {
      switch (node)
      {
        case JsonObject jsonObject:
          foreach (var key in Keys(jsonObject))
          {
            if (IsSensitive(key))
            {
              if (jsonObject[key] is not null)
              {
                jsonObject[key] = RedactionValue;
              }
            }
            else if (jsonObject[key] is JsonNode childNode)
            {
              Redact(childNode);
            }
          }
          break;

        case JsonArray jsonArray:
          for (int i = 0; i < jsonArray.Count; i++)
          {
            if (jsonArray[i] is JsonNode childNode)
            {
              Redact(childNode);
            }
          }
          break;
      }
      return node;
    }

    private List<string> Keys(JsonObject obj)
    {
      var keys = new List<string>();
      foreach (var property in obj)
        keys.Add(property.Key);
      return keys;
    }
  }

  //-----------------------------------------------------------------------------------------------
}