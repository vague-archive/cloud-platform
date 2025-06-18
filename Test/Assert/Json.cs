namespace Void.Platform.Test;

using System.Text.Json;
using System.Text.Json.Nodes;

public class AssertJson
{
  //-----------------------------------------------------------------------------------------------

  public JsonNode Object(HttpResponseMessage response, int expectedStatusCode = 200)
  {
    var content = Assert.Http.Result(expectedStatusCode, response);
    Assert.Http.ContentTypeJson(response);
    return Object(content);
  }

  public JsonNode Object(string content)
  {
    var doc = JsonNode.Parse(content);
    Assert.Present(doc);
    return Object(doc);
  }

  //-----------------------------------------------------------------------------------------------

  public JsonObject Object(JsonNode? node)
  {
    Assert.Present(node);
    Assert.True(node is JsonObject);
    var obj = node as JsonObject;
    Assert.Present(obj);
    return obj;
  }

  public JsonArray Array(int expectedLength, JsonNode? node)
  {
    Assert.Present(node);
    Assert.True(node is JsonArray);
    var arr = node as JsonArray;
    Assert.Present(arr);
    Assert.Equal(expectedLength, arr.Count);
    return arr;
  }

  public string String(JsonNode? node)
  {
    Assert.Present(node);
    Assert.True(node is JsonValue);
    var value = node as JsonValue;
    Assert.Present(value);
    Assert.Equal(JsonValueKind.String, value.GetValueKind());
    Assert.True(value.TryGetValue<string>(out string? actual));
    Assert.Present(actual);
    return actual;
  }

  public long Number(JsonNode? node)
  {
    Assert.Present(node);
    Assert.True(node is JsonValue);
    var value = node as JsonValue;
    Assert.Present(value);
    Assert.Equal(JsonValueKind.Number, value.GetValueKind());
    Assert.True(value.TryGetValue<long>(out long actual));
    return actual;
  }

  public bool Boolean(JsonNode? node)
  {
    Assert.Present(node);
    Assert.True(node is JsonValue);
    var value = node as JsonValue;
    Assert.Present(value);
    var kind = value.GetValueKind();
    Assert.True(kind == JsonValueKind.True || kind == JsonValueKind.False);
    return value.GetValue<bool>();
  }

  //-----------------------------------------------------------------------------------------------

  public void Properties(string[] expected, JsonNode? node)
  {
    Assert.Present(node);
    Assert.True(node is JsonObject);
    var obj = node as JsonObject;
    Assert.Present(obj);
    var actual = obj.Select(prop => prop.Key);
    Assert.Equal(expected.Order(), actual.Order());
  }

  //-----------------------------------------------------------------------------------------------

  public void Equal(string expected, JsonNode? node) =>
    Assert.Equal(expected, String(node));

  public void Equal(long expected, JsonNode? node) =>
    Assert.Equal(expected, Number(node));

  public void Equal(bool expected, JsonNode? node) =>
    Assert.Equal(expected, Boolean(node));

  //-----------------------------------------------------------------------------------------------
}