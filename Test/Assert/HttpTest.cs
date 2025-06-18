namespace Void.Platform.Test;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Xunit.Sdk;

public class AssertHttpTest
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestHttpOk()
  {
    var result1 = new StatusCodeResult(Http.StatusCode.Ok);
    var result2 = new OkObjectResult("value2");
    var result3 = new ActionResult<string>("value3");
    var result4 = new ActionResult<string>(new StatusCodeResult(Http.StatusCode.Ok));
    var result5 = new ActionResult<string>(new OkObjectResult("value5"));
    var result6 = new ActionResult<string>(new OkObjectResult(42));
    var result7 = (IActionResult) new StatusCodeResult(Http.StatusCode.Ok);
    var result8 = (IActionResult) new OkObjectResult("value8");

    var value1 = Assert.Http.Ok(result1);
    var value2 = Assert.Http.Ok(result2);
    var value3 = Assert.Http.Ok(result3);
    var value4 = Assert.Http.Ok(result4);
    var value5 = Assert.Http.Ok(result5);
    var value6 = Assert.Http.Ok<int, string>(result6); // requires explicit types when ActionResult<T> contains an ObjectResult with something other than T
    var value7 = Assert.Http.Ok(result7);
    var value8 = Assert.Http.Ok(result8);

    Assert.Null(value1);
    Assert.Equal("value2", value2);
    Assert.Equal("value3", value3);
    Assert.Null(value4);
    Assert.Equal("value5", value5);
    Assert.Equal(42, value6);
    Assert.Null(value7);
    Assert.Equal("value8", value8);

    Assert.Throws<EqualException>(() => Assert.Http.BadRequest(result1));
    Assert.Throws<EqualException>(() => Assert.Http.BadRequest(result2));
    Assert.Throws<EqualException>(() => Assert.Http.BadRequest(result3));
    Assert.Throws<EqualException>(() => Assert.Http.BadRequest(result4));
    Assert.Throws<EqualException>(() => Assert.Http.BadRequest(result5));
    Assert.Throws<EqualException>(() => Assert.Http.BadRequest(result6));
    Assert.Throws<EqualException>(() => Assert.Http.BadRequest(result7));
    Assert.Throws<EqualException>(() => Assert.Http.BadRequest(result8));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestHttpBadRequest()
  {
    var result1 = new StatusCodeResult(Http.StatusCode.BadRequest);
    var result2 = new BadRequestObjectResult("error2");
    var result3 = new ActionResult<string>("value3"); // impossible to make this non-200, so ignore this case
    var result4 = new ActionResult<string>(new StatusCodeResult(Http.StatusCode.BadRequest));
    var result5 = new ActionResult<string>(new BadRequestObjectResult("error5"));
    var result6 = new ActionResult<string>(new BadRequestObjectResult(42));
    var result7 = (IActionResult) new StatusCodeResult(Http.StatusCode.BadRequest);
    var result8 = (IActionResult) new BadRequestObjectResult("error8");

    var value1 = Assert.Http.BadRequest(result1);
    var value2 = Assert.Http.BadRequest(result2);
    var value4 = Assert.Http.BadRequest(result4);
    var value5 = Assert.Http.BadRequest(result5);
    var value6 = Assert.Http.BadRequest<int, string>(result6); // requires explicit types when ActionResult<T> contains an ObjectResult with something other than T
    var value7 = Assert.Http.BadRequest(result7);
    var value8 = Assert.Http.BadRequest(result8);

    Assert.Null(value1);
    Assert.Equal("error2", value2);
    Assert.Null(value4);
    Assert.Equal("error5", value5);
    Assert.Equal(42, value6);
    Assert.Null(value7);
    Assert.Equal("error8", value8);

    Assert.Throws<EqualException>(() => Assert.Http.Ok(result1));
    Assert.Throws<EqualException>(() => Assert.Http.Ok(result2));
    Assert.Throws<EqualException>(() => Assert.Http.Ok(result4));
    Assert.Throws<EqualException>(() => Assert.Http.Ok(result5));
    Assert.Throws<EqualException>(() => Assert.Http.Ok(result6));
    Assert.Throws<EqualException>(() => Assert.Http.Ok(result7));
    Assert.Throws<EqualException>(() => Assert.Http.Ok(result8));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestHttpPageResult()
  {
    IActionResult result1 = new PageResult();
    IActionResult result2 = new PageResult() { StatusCode = Http.StatusCode.Ok };
    IActionResult result3 = new PageResult() { StatusCode = Http.StatusCode.BadRequest };
    IActionResult result4 = new PageResult() { StatusCode = Http.StatusCode.InternalServerError };

    Assert.Equal(result1, Assert.Http.PageOk(result1));
    Assert.Equal(result2, Assert.Http.PageOk(result2));

    Assert.Equal(result1, Assert.Http.PageResult(Http.StatusCode.Ok, result1));
    Assert.Equal(result2, Assert.Http.PageResult(Http.StatusCode.Ok, result2));
    Assert.Equal(result3, Assert.Http.PageResult(Http.StatusCode.BadRequest, result3));
    Assert.Equal(result4, Assert.Http.PageResult(Http.StatusCode.InternalServerError, result4));
  }

  //-----------------------------------------------------------------------------------------------
}