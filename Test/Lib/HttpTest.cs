namespace Void.Platform.Lib;

using Microsoft.AspNetCore.Http;

public class HttpTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestStatusCodes()
  {
    Assert.Equal(200, Http.StatusCode.Ok);
    Assert.Equal(202, Http.StatusCode.Accepted);
    Assert.Equal(204, Http.StatusCode.NoContent);
    Assert.Equal(304, Http.StatusCode.NotModified);
    Assert.Equal(400, Http.StatusCode.BadRequest);
    Assert.Equal(401, Http.StatusCode.Unauthorized);
    Assert.Equal(403, Http.StatusCode.Forbidden);
    Assert.Equal(404, Http.StatusCode.NotFound);
    Assert.Equal(405, Http.StatusCode.MethodNotAllowed);
    Assert.Equal(500, Http.StatusCode.InternalServerError);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestHeaderNames()
  {
    Assert.Equal("Accept", Http.Header.Accept);
    Assert.Equal("Access-Control-Allow-Headers", Http.Header.AccessControlAllowHeaders);
    Assert.Equal("Access-Control-Allow-Methods", Http.Header.AccessControlAllowMethods);
    Assert.Equal("Access-Control-Allow-Origin", Http.Header.AccessControlAllowOrigin);
    Assert.Equal("Authorization", Http.Header.Authorization);
    Assert.Equal("Cache-Control", Http.Header.CacheControl);
    Assert.Equal("Content-Disposition", Http.Header.ContentDisposition);
    Assert.Equal("Content-Encoding", Http.Header.ContentEncoding);
    Assert.Equal("Content-Language", Http.Header.ContentLanguage);
    Assert.Equal("Content-Length", Http.Header.ContentLength);
    Assert.Equal("Content-Location", Http.Header.ContentLocation);
    Assert.Equal("Content-MD5", Http.Header.ContentMD5);
    Assert.Equal("Content-Range", Http.Header.ContentRange);
    Assert.Equal("Content-Type", Http.Header.ContentType);
    Assert.Equal("Cookie", Http.Header.Cookie);
    Assert.Equal("Cross-Origin-Embedder-Policy", Http.Header.CrossOriginEmbedderPolicy);
    Assert.Equal("Cross-Origin-Opener-Policy", Http.Header.CrossOriginOpenerPolicy);
    Assert.Equal("Cross-Origin-Resource-Policy", Http.Header.CrossOriginResourcePolicy);
    Assert.Equal("ETag", Http.Header.ETag);
    Assert.Equal("Expires", Http.Header.Expires);
    Assert.Equal("HX-Redirect", Http.Header.HxRedirect);
    Assert.Equal("HX-Refresh", Http.Header.HxRefresh);
    Assert.Equal("HX-Request", Http.Header.HxRequest);
    Assert.Equal("HX-Retarget", Http.Header.HxRetarget);
    Assert.Equal("If-Modified-Since", Http.Header.IfModifiedSince);
    Assert.Equal("If-Modified-Since", Http.Header.IfModifiedSince);
    Assert.Equal("Last-Modified", Http.Header.LastModified);
    Assert.Equal("Last-Modified", Http.Header.LastModified);
    Assert.Equal("Location", Http.Header.Location);
    Assert.Equal("Location", Http.Header.Location);
    Assert.Equal("User-Agent", Http.Header.UserAgent);
    Assert.Equal("X-CSRF-Token", Http.Header.CSRFToken);
    Assert.Equal("X-Command", Http.Header.CustomCommand);
    Assert.Equal("X-Deploy-Id", Http.Header.XDeployId);
    Assert.Equal("X-Deploy-Label", Http.Header.XDeployLabel);
    Assert.Equal("X-Deploy-Password", Http.Header.XDeployPassword);
    Assert.Equal("X-Deploy-Pinned", Http.Header.XDeployPinned);
    Assert.Equal("X-Forwarded-For", Http.Header.XForwardedFor);
    Assert.Equal("X-Forwarded-Host", Http.Header.XForwardedHost);
    Assert.Equal("X-Forwarded-Prefix", Http.Header.XForwardedPrefix);
    Assert.Equal("X-Forwarded-Proto", Http.Header.XForwardedProto);
    Assert.Equal("X-Frame-Options", Http.Header.XFrameOptions);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestContentTypes()
  {
    Assert.Equal("application/gzip", Http.ContentType.Gzip);
    Assert.Equal("application/json", Http.ContentType.Json);
    Assert.Equal("application/octet-stream", Http.ContentType.Bytes);
    Assert.Equal("application/pdf", Http.ContentType.Pdf);
    Assert.Equal("application/wasm", Http.ContentType.Wasm);
    Assert.Equal("application/x-www-form-urlencoded", Http.ContentType.Form);
    Assert.Equal("image/png", Http.ContentType.Png);
    Assert.Equal("text/css", Http.ContentType.Css);
    Assert.Equal("text/html", Http.ContentType.Html);
    Assert.Equal("text/javascript", Http.ContentType.Javascript);
    Assert.Equal("text/markdown", Http.ContentType.Markdown);
    Assert.Equal("text/plain", Http.ContentType.Text);
    Assert.Equal("text/plain; charset=utf-8", Http.ContentType.TextUtf8);
    Assert.Equal("text/xml", Http.ContentType.Xml);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void DeriveContentTypes()
  {
    Assert.Equal(Http.ContentType.Css, Http.DeriveContentType("path/to/file.css"));
    Assert.Equal(Http.ContentType.Html, Http.DeriveContentType("path/to/file.html"));
    Assert.Equal(Http.ContentType.Javascript, Http.DeriveContentType("path/to/file.js"));
    Assert.Equal(Http.ContentType.Json, Http.DeriveContentType("path/to/file.json"));
    Assert.Equal(Http.ContentType.Pdf, Http.DeriveContentType("path/to/file.pdf"));
    Assert.Equal(Http.ContentType.Text, Http.DeriveContentType("path/to/file.txt"));
    Assert.Equal(Http.ContentType.Wasm, Http.DeriveContentType("path/to/file.wasm"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestETagGenerator()
  {
    Assert.Equal(@"""8b04d5e3775d298e78455efc5ca404d5""", Http.ETag("first"));
    Assert.Equal(@"""a9f0e61a137d86aa9db53465e0801612""", Http.ETag("second"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestCORSValues()
  {
    Assert.Equal("cross-origin", Http.CORS.CrossOrigin);
    Assert.Equal("require-corp", Http.CORS.RequireCorp);
    Assert.Equal("same-origin", Http.CORS.SameOrigin);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestCacheControlValues()
  {
    Assert.Equal("max-age=0", Http.CacheControl.MaxAgeZero);
    Assert.Equal("must-revalidate", Http.CacheControl.MustRevalidate);
    Assert.Equal("must-revalidate, no-cache, max-age=0", Http.CacheControl.MustRevalidateStrict);
    Assert.Equal("no-cache", Http.CacheControl.NoCache);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestIsContentHeader()
  {
    Assert.True(Http.IsContentHeader(Http.Header.ContentDisposition));
    Assert.True(Http.IsContentHeader(Http.Header.ContentEncoding));
    Assert.True(Http.IsContentHeader(Http.Header.ContentLanguage));
    Assert.True(Http.IsContentHeader(Http.Header.ContentLength));
    Assert.True(Http.IsContentHeader(Http.Header.ContentLocation));
    Assert.True(Http.IsContentHeader(Http.Header.ContentMD5));
    Assert.True(Http.IsContentHeader(Http.Header.ContentRange));
    Assert.True(Http.IsContentHeader(Http.Header.ContentType));
    Assert.True(Http.IsContentHeader(Http.Header.LastModified));

    Assert.False(Http.IsContentHeader(Http.Header.Accept));
    Assert.False(Http.IsContentHeader(Http.Header.AccessControlAllowOrigin));
    Assert.False(Http.IsContentHeader(Http.Header.Authorization));
    Assert.False(Http.IsContentHeader(Http.Header.CSRFToken));
    Assert.False(Http.IsContentHeader(Http.Header.CacheControl));
    Assert.False(Http.IsContentHeader(Http.Header.Cookie));
    Assert.False(Http.IsContentHeader(Http.Header.CrossOriginEmbedderPolicy));
    Assert.False(Http.IsContentHeader(Http.Header.CrossOriginOpenerPolicy));
    Assert.False(Http.IsContentHeader(Http.Header.CrossOriginResourcePolicy));
    Assert.False(Http.IsContentHeader(Http.Header.CustomCommand));
    Assert.False(Http.IsContentHeader(Http.Header.ETag));
    Assert.False(Http.IsContentHeader(Http.Header.Expires));
    Assert.False(Http.IsContentHeader(Http.Header.HxRedirect));
    Assert.False(Http.IsContentHeader(Http.Header.HxRefresh));
    Assert.False(Http.IsContentHeader(Http.Header.HxRequest));
    Assert.False(Http.IsContentHeader(Http.Header.HxRetarget));
    Assert.False(Http.IsContentHeader(Http.Header.IfModifiedSince));
    Assert.False(Http.IsContentHeader(Http.Header.Location));
    Assert.False(Http.IsContentHeader(Http.Header.XDeployLabel));
    Assert.False(Http.IsContentHeader(Http.Header.XDeployPassword));
    Assert.False(Http.IsContentHeader(Http.Header.XDeployPinned));

  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestParams()
  {
    var qp = Http.Params();
    qp.Add("hello", "world");
    qp.Add("foo", "a+b/c");
    Assert.Equal("hello=world&foo=a%2bb%2fc", qp.ToString());

    qp = Http.Params("foo=bar");
    qp.Add("hello", "world");
    Assert.Equal("foo=bar&hello=world", qp.ToString());

    qp = Http.Params(new[] {
      new Http.Param("hello", "world"),
      new Http.Param("foo", "a+b/c"),
    });
    Assert.Equal("hello=world&foo=a%2bb%2fc", qp.ToString());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestWithParam()
  {
    Assert.Equal("https://play.void.dev/path?foo=bar", Http.WithParam("https://play.void.dev/path", "foo", "bar"));
    Assert.Equal("https://play.void.dev/path?foo=bar&name=bob", Http.WithParam("https://play.void.dev/path?foo=bar", "name", "bob"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestWithParams()
  {
    var uri = new Uri("https://play.void.dev/profile").WithParams(new[]
    {
      new Http.Param("hello", "world"),
      new Http.Param("foo", "a+b/c"),
    });
    Assert.IsType<Uri>(uri);
    Assert.Equal("https://play.void.dev/profile?hello=world&foo=a%2bb%2fc", uri.ToString());

    var qp = Http.Params(uri);
    Assert.Equal(["hello", "foo"], qp.AllKeys.ToList());
    Assert.Equal("world", qp.Get("hello"));
    Assert.Equal("a+b/c", qp.Get("foo"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetHeader()
  {
    var ctx = new DefaultHttpContext();

    Assert.Null(ctx.GetHeader(Http.Header.ContentType));
    Assert.Null(ctx.GetHeader("X-Custom-Header"));

    ctx.Request.Headers.Append(Http.Header.ContentType, Http.ContentType.Json);
    ctx.Request.Headers.Append("X-Custom-Header", "first");
    ctx.Request.Headers.Append("X-Custom-Header", "second");

    Assert.Equal(Http.ContentType.Json, ctx.GetHeader(Http.Header.ContentType));
    Assert.Equal("first", ctx.GetHeader("X-Custom-Header"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestIsAbsoluteUrl()
  {
    Assert.True(Http.IsAbsoluteUrl("https://play.void.dev"));
    Assert.True(Http.IsAbsoluteUrl("https://play.void.dev/"));
    Assert.True(Http.IsAbsoluteUrl("https://play.void.dev/foo"));
    Assert.True(Http.IsAbsoluteUrl("https://play.void.dev/foo/bar"));
    Assert.True(Http.IsAbsoluteUrl("http://localhost"));
    Assert.True(Http.IsAbsoluteUrl("http://localhost/"));
    Assert.True(Http.IsAbsoluteUrl("http://localhost:3000"));
    Assert.True(Http.IsAbsoluteUrl("http://localhost:3000/"));
    Assert.True(Http.IsAbsoluteUrl("http://localhost:3000/foo"));
    Assert.True(Http.IsAbsoluteUrl("http://localhost:3000/foo/bar"));

    Assert.False(Http.IsAbsoluteUrl("/"));
    Assert.False(Http.IsAbsoluteUrl("/ping"));
    Assert.False(Http.IsAbsoluteUrl("/foo/bar"));
    Assert.False(Http.IsAbsoluteUrl("eggs"));
  }

  //-----------------------------------------------------------------------------------------------
}