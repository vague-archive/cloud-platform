namespace Void.Platform.Test;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Void.Platform.Web.Htmx;

public class AssertHttp
{
  //---------------------------------------------------------------------------------------------

  public object? Ok(StatusCodeResult result) => Result(Http.StatusCode.Ok, result);
  public object? Ok(ObjectResult result) => Result(Http.StatusCode.Ok, result);
  public object? Ok(IActionResult result) => Result(Http.StatusCode.Ok, result);
  public R? Ok<R>(ObjectResult result) => Result<R>(Http.StatusCode.Ok, result);
  public R? Ok<R>(IActionResult result) => Result<R>(Http.StatusCode.Ok, result);
  public R? Ok<R>(ActionResult<R> result) => Result<R>(Http.StatusCode.Ok, result);
  public R? Ok<R, T>(ActionResult<T> result) => Result<R, T>(Http.StatusCode.Ok, result);
  public string Ok(HttpResponseMessage response) => Result(Http.StatusCode.Ok, response);
  public T Ok<T>(HttpResponseMessage response) => Result<T>(Http.StatusCode.Ok, response);

  //---------------------------------------------------------------------------------------------

  public object? BadRequest(StatusCodeResult result) => Result(Http.StatusCode.BadRequest, result);
  public object? BadRequest(ObjectResult result) => Result(Http.StatusCode.BadRequest, result);
  public object? BadRequest(IActionResult result) => Result(Http.StatusCode.BadRequest, result);
  public R? BadRequest<R>(ObjectResult result) => Result<R>(Http.StatusCode.BadRequest, result);
  public R? BadRequest<R>(IActionResult result) => Result<R>(Http.StatusCode.BadRequest, result);
  public R? BadRequest<R>(ActionResult<R> result) => Result<R>(Http.StatusCode.BadRequest, result);
  public R? BadRequest<R, T>(ActionResult<T> result) => Result<R, T>(Http.StatusCode.BadRequest, result);
  public string BadRequest(HttpResponseMessage response) => Result(Http.StatusCode.BadRequest, response);

  //---------------------------------------------------------------------------------------------

  public object? Unauthorized(StatusCodeResult result) => Result(Http.StatusCode.Unauthorized, result);
  public object? Unauthorized(ObjectResult result) => Result(Http.StatusCode.Unauthorized, result);
  public object? Unauthorized(IActionResult result) => Result(Http.StatusCode.Unauthorized, result);
  public R? Unauthorized<R>(ObjectResult result) => Result<R>(Http.StatusCode.Unauthorized, result);
  public R? Unauthorized<R>(IActionResult result) => Result<R>(Http.StatusCode.Unauthorized, result);
  public R? Unauthorized<R>(ActionResult<R> result) => Result<R>(Http.StatusCode.Unauthorized, result);
  public R? Unauthorized<R, T>(ActionResult<T> result) => Result<R, T>(Http.StatusCode.Unauthorized, result);
  public string Unauthorized(HttpResponseMessage response) => Result(Http.StatusCode.Unauthorized, response);

  //---------------------------------------------------------------------------------------------

  public object? Forbidden(StatusCodeResult result) => Result(Http.StatusCode.Forbidden, result);
  public object? Forbidden(ObjectResult result) => Result(Http.StatusCode.Forbidden, result);
  public object? Forbidden(IActionResult result) => Result(Http.StatusCode.Forbidden, result);
  public R? Forbidden<R>(ObjectResult result) => Result<R>(Http.StatusCode.Forbidden, result);
  public R? Forbidden<R>(IActionResult result) => Result<R>(Http.StatusCode.Forbidden, result);
  public R? Forbidden<R>(ActionResult<R> result) => Result<R>(Http.StatusCode.Forbidden, result);
  public R? Forbidden<R, T>(ActionResult<T> result) => Result<R, T>(Http.StatusCode.Forbidden, result);
  public string Forbidden(HttpResponseMessage response) => Result(Http.StatusCode.Forbidden, response);

  //---------------------------------------------------------------------------------------------

  public object? NotFound(StatusCodeResult result) => Result(Http.StatusCode.NotFound, result);
  public object? NotFound(ObjectResult result) => Result(Http.StatusCode.NotFound, result);
  public object? NotFound(IActionResult result) => Result(Http.StatusCode.NotFound, result);
  public R? NotFound<R>(ObjectResult result) => Result<R>(Http.StatusCode.NotFound, result);
  public R? NotFound<R>(IActionResult result) => Result<R>(Http.StatusCode.NotFound, result);
  public R? NotFound<R>(ActionResult<R> result) => Result<R>(Http.StatusCode.NotFound, result);
  public R? NotFound<R, T>(ActionResult<T> result) => Result<R, T>(Http.StatusCode.NotFound, result);
  public string NotFound(HttpResponseMessage response) => Result(Http.StatusCode.NotFound, response);

  //---------------------------------------------------------------------------------------------

  public object? InternalServerError(StatusCodeResult result) => Result(Http.StatusCode.InternalServerError, result);
  public object? InternalServerError(ObjectResult result) => Result(Http.StatusCode.InternalServerError, result);
  public object? InternalServerError(IActionResult result) => Result(Http.StatusCode.InternalServerError, result);
  public R? InternalServerError<R>(ObjectResult result) => Result<R>(Http.StatusCode.InternalServerError, result);
  public R? InternalServerError<R>(IActionResult result) => Result<R>(Http.StatusCode.InternalServerError, result);
  public R? InternalServerError<R>(ActionResult<R> result) => Result<R>(Http.StatusCode.InternalServerError, result);
  public R? InternalServerError<R, T>(ActionResult<T> result) => Result<R, T>(Http.StatusCode.InternalServerError, result);
  public string InternalServerError(HttpResponseMessage response) => Result(Http.StatusCode.InternalServerError, response);

  //---------------------------------------------------------------------------------------------

  public object? NoContent(StatusCodeResult result) => Result(Http.StatusCode.NoContent, result);
  public object? NoContent(ObjectResult result) => Result(Http.StatusCode.NoContent, result);
  public object? NoContent(IActionResult result) => Result(Http.StatusCode.NoContent, result);
  public R? NoContent<R>(ObjectResult result) => Result<R>(Http.StatusCode.NoContent, result);
  public R? NoContent<R>(IActionResult result) => Result<R>(Http.StatusCode.NoContent, result);
  public R? NoContent<R>(ActionResult<R> result) => Result<R>(Http.StatusCode.NoContent, result);
  public R? NoContent<R, T>(ActionResult<T> result) => Result<R, T>(Http.StatusCode.NoContent, result);
  public string NoContent(HttpResponseMessage response) => Result(Http.StatusCode.NoContent, response);

  //---------------------------------------------------------------------------------------------

  public object? Result(int expected, StatusCodeResult result)
  {
    Assert.Equal(expected, result.StatusCode);
    return null;
  }

  //---------------------------------------------------------------------------------------------

  public object? Result(int expected, ObjectResult result)
  {
    Assert.NotNull(result.StatusCode);
    Assert.Equal(expected, (int) result.StatusCode);
    return result.Value;
  }

  public R? Result<R>(int expected, ObjectResult result)
  {
    var content = Result(expected, result);
    Assert.IsType<R>(content);
    return (R) content;
  }

  //---------------------------------------------------------------------------------------------

  public object? Result(int expected, IActionResult result)
  {
    if (result is StatusCodeResult)
    {
      return Result(expected, (StatusCodeResult) result);
    }
    else if (result is ObjectResult)
    {
      return Result(expected, (ObjectResult) result);
    }
    else if (result is PageResult)
    {
      throw new Exception("Not supported. Use Assert.Http.PageResult instead");
    }
    else
    {
      throw new Exception(String.Format("TODO: handle other types {0}", result.GetType()));
    }
  }

  public R? Result<R>(int expected, IActionResult result)
  {
    var content = Result(expected, result);
    Assert.IsType<R>(content);
    return (R) content;
  }

  //---------------------------------------------------------------------------------------------

  public R? Result<R>(int expected, ActionResult<R> result)
  {
    return Result<R, R>(expected, result);
  }

  public R? Result<R, T>(int expected, ActionResult<T> result)
  {
    var iactual = ((IConvertToActionResult) result).Convert();
    var value = Result(expected, iactual);
    if (value is not null)
    {
      Assert.IsAssignableFrom<R>(value);
      return (R) value;
    }
    else
    {
      return default(R?); // should be equivalent to return null
    }
  }

  //---------------------------------------------------------------------------------------------

  public string Result(int expected, HttpResponseMessage response)
  {
    Assert.Equal(expected, (int) response.StatusCode);
    using (var reader = new StreamReader(response.Content.ReadAsStream()))
    {
      return reader.ReadToEnd();
    }
  }

  public T Result<T>(int expected, HttpResponseMessage response)
  {
    var content = Result(expected, response);
    return Json.Deserialize<T>(content);
  }

  //---------------------------------------------------------------------------------------------

  public PageResult PageOk(IActionResult actual)
  {
    return PageResult(Http.StatusCode.Ok, actual);
  }

  public PageResult PageResult(int expected, IActionResult actual)
  {
    Assert.IsType<PageResult>(actual);
    var result = (PageResult) actual;
    Assert.Equal(expected, result.StatusCode ?? Http.StatusCode.Ok);
    return result;
  }

  //---------------------------------------------------------------------------------------------

  public string Redirect(HttpResponseMessage response)
  {
    if (response.IsHtmx())
    {
      Assert.Equal(200, (int) response.StatusCode);
      Assert.True(response.Headers.Contains(Http.Header.HxRedirect));
      Assert.True(response.Headers.TryGetValues(Http.Header.HxRedirect, out var location));
      Assert.Single(location);
      return location.First();
    }
    else
    {
      Assert.Equal(302, (int) response.StatusCode);
      var location = response.Headers.Location;
      Assert.NotNull(location);
      return location.ToString();
    }
  }

  //---------------------------------------------------------------------------------------------

  public void Refresh(HttpResponseMessage response)
  {
    if (response.IsHtmx())
    {
      Assert.Equal(200, (int) response.StatusCode);
      Assert.True(response.Headers.Contains(Http.Header.HxRefresh));
      Assert.True(response.Headers.TryGetValues(Http.Header.HxRefresh, out var value));
      Assert.Single(value);
      Assert.Equal("true", value.First());
    }
    else
    {
      throw new Exception("No such thing as a non-htmx refresh");
    }
  }

  //---------------------------------------------------------------------------------------------

  public void HasHeader(string expected, string key, HttpResponse response)
  {
    Assert.True(response.Headers.ContainsKey(key));
    Assert.True(response.Headers.TryGetValue(key, out var value));
    Assert.Equal(expected, value);
  }

  public string HasHeader(string key, HttpResponseMessage response)
  {
    if (Http.IsContentHeader(key))
    {
      Assert.True(response.Content.Headers.Contains(key));
      Assert.True(response.Content.Headers.TryGetValues(key, out var values));
      Assert.Single(values);
      return values.First();
    }
    else
    {
      Assert.True(response.Headers.Contains(key));
      Assert.True(response.Headers.TryGetValues(key, out var values));
      Assert.Single(values);
      return values.First();
    }
  }

  public void HasHeader(string expected, string key, HttpResponseMessage response)
  {
    Assert.Equal(expected, HasHeader(key, response));
  }

  public void HasNoHeader(string key, HttpResponseMessage response)
  {
    if (Http.IsContentHeader(key))
    {
      Assert.False(response.Content.Headers.Contains(key));
    }
    else
    {
      Assert.False(response.Headers.Contains(key));
    }
  }

  //---------------------------------------------------------------------------------------------

  public void ContentType(string expected, HttpResponseMessage response)
  {
    var value = response.Content.Headers.ContentType;
    Assert.NotNull(value);
    Assert.StartsWith(expected, value.ToString());
  }

  public void ContentTypeJson(HttpResponseMessage response)
  {
    ContentType(Http.ContentType.Json, response);
  }

  //---------------------------------------------------------------------------------------------

  public void CurrentUrl(string expected, HttpResponseMessage response)
  {
    Assert.NotNull(response.RequestMessage);
    Assert.NotNull(response.RequestMessage.RequestUri);
    Assert.Equal(expected, response.RequestMessage.RequestUri.ToString());
  }

  //---------------------------------------------------------------------------------------------
}