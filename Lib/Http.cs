namespace Void.Platform.Lib;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System.Collections.Specialized;
using System.Web;

public static class Http
{
  //-----------------------------------------------------------------------------------------------

  public static class StatusCode
  {
    public const int Ok = StatusCodes.Status200OK;
    public const int Accepted = StatusCodes.Status202Accepted;
    public const int NoContent = StatusCodes.Status204NoContent;
    public const int NotModified = StatusCodes.Status304NotModified;
    public const int BadRequest = StatusCodes.Status400BadRequest;
    public const int Forbidden = StatusCodes.Status403Forbidden;
    public const int MethodNotAllowed = StatusCodes.Status405MethodNotAllowed;
    public const int InternalServerError = StatusCodes.Status500InternalServerError;
    public const int NotFound = StatusCodes.Status404NotFound;
    public const int Unauthorized = StatusCodes.Status401Unauthorized;
  }


  public static class Header
  {
    public const string Accept = "Accept";
    public const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
    public const string AccessControlAllowMethods = "Access-Control-Allow-Methods";
    public const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
    public const string Authorization = "Authorization";
    public const string CSRFToken = "X-CSRF-Token";
    public const string CacheControl = "Cache-Control";
    public const string ContentDisposition = "Content-Disposition";
    public const string ContentEncoding = "Content-Encoding";
    public const string ContentLanguage = "Content-Language";
    public const string ContentLength = "Content-Length";
    public const string ContentLocation = "Content-Location";
    public const string ContentMD5 = "Content-MD5";
    public const string ContentRange = "Content-Range";
    public const string ContentType = "Content-Type";
    public const string Cookie = "Cookie";
    public const string CrossOriginEmbedderPolicy = "Cross-Origin-Embedder-Policy";
    public const string CrossOriginOpenerPolicy = "Cross-Origin-Opener-Policy";
    public const string CrossOriginResourcePolicy = "Cross-Origin-Resource-Policy";
    public const string CustomCommand = "X-Command";
    public const string ETag = "ETag";
    public const string Expires = "Expires";
    public const string HxRedirect = "HX-Redirect";
    public const string HxRefresh = "HX-Refresh";
    public const string HxRequest = "HX-Request";
    public const string HxRetarget = "HX-Retarget";
    public const string IfModifiedSince = "If-Modified-Since";
    public const string LastModified = "Last-Modified";
    public const string Location = "Location";
    public const string UserAgent = "User-Agent";
    public const string XDeployId = "X-Deploy-Id";
    public const string XDeployLabel = "X-Deploy-Label";
    public const string XDeployPassword = "X-Deploy-Password";
    public const string XDeployPinned = "X-Deploy-Pinned";
    public const string XForwardedFor = "X-Forwarded-For";
    public const string XForwardedHost = "X-Forwarded-Host";
    public const string XForwardedPrefix = "X-Forwarded-Prefix";
    public const string XForwardedProto = "X-Forwarded-Proto";
    public const string XFrameOptions = "X-Frame-Options";
  }

  public static class ContentType
  {
    public const string Bytes = "application/octet-stream";
    public const string Css = "text/css";
    public const string Form = "application/x-www-form-urlencoded";
    public const string Gzip = "application/gzip";
    public const string Html = "text/html";
    public const string Javascript = "text/javascript";
    public const string Json = "application/json";
    public const string Markdown = "text/markdown";
    public const string Pdf = "application/pdf";
    public const string Png = "image/png";
    public const string Text = "text/plain";
    public const string TextUtf8 = "text/plain; charset=utf-8";
    public const string Wasm = "application/wasm";
    public const string Xml = "text/xml";
  }

  public static class CORS
  {
    public const string CrossOrigin = "cross-origin";
    public const string RequireCorp = "require-corp";
    public const string SameOrigin = "same-origin";
  }

  public static class CacheControl
  {
    public const string MaxAgeZero = "max-age=0";
    public const string MustRevalidate = "must-revalidate";
    public const string MustRevalidateStrict = $"{MustRevalidate}, {NoCache}, {MaxAgeZero}";
    public const string NoCache = "no-cache";
  }

  //-----------------------------------------------------------------------------------------------

  private static readonly HashSet<string> contentHeaders = new()
  {
    Http.Header.ContentDisposition,
    Http.Header.ContentEncoding,
    Http.Header.ContentLanguage,
    Http.Header.ContentLength,
    Http.Header.ContentLocation,
    Http.Header.ContentMD5,
    Http.Header.ContentRange,
    Http.Header.ContentType,
    Http.Header.LastModified,
  };

  public static bool IsContentHeader(string key)
  {
    return contentHeaders.Contains(key);
  }

  //-----------------------------------------------------------------------------------------------

  private static FileExtensionContentTypeProvider ContentTypeProvider = new FileExtensionContentTypeProvider();

  public static string DeriveContentType(string fileName, string defaultType = ContentType.Bytes)
  {
    if (ContentTypeProvider.TryGetContentType(fileName, out var contentType))
    {
      return contentType;
    }
    else
    {
      return defaultType;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static string ETag(string content)
  {
    return @$"""{Crypto.HexString(Crypto.MD5(content))}""";
  }

  //-----------------------------------------------------------------------------------------------

  public static NameValueCollection Params(Uri uri)
  {
    return Params(uri.Query);
  }

  public static NameValueCollection Params(string values = "")
  {
    return HttpUtility.ParseQueryString(values);
  }

  public static NameValueCollection Params(Param[] entries)
  {
    var result = Params();
    foreach (var entry in entries)
      result.Add(entry.Name, entry.Value);
    return result;
  }

  public struct Param
  {
    public string Name { get; }
    public string Value { get; }

    public Param(string name, string value)
    {
      Name = name;
      Value = value;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static Uri WithParam(string uri, string name, string value)
  {
    return WithParams(new Uri(uri), new[] { new Param(name, value) });
  }

  public static Uri WithParam(this Uri uri, string name, string value)
  {
    return WithParams(uri, new[] { new Param(name, value) });
  }

  public static Uri WithParams(this Uri uri, Param[] entries)
  {
    return new UriBuilder(uri).WithParams(entries).Uri;
  }

  public static UriBuilder WithParams(this UriBuilder builder, Param[] entries)
  {
    var qp = Params(builder.Query);
    qp.Add(Params(entries));
    builder.Query = qp.ToString();
    return builder;
  }

  //-----------------------------------------------------------------------------------------------

  public static string? GetHeader(this HttpContext context, string key)
  {
    return GetHeader(context.Request, key);
  }

  public static string? GetHeader(this HttpRequest request, string key)
  {
    return request.Headers[key].FirstOrDefault();
  }

  public static string? GetQueryParam(this HttpRequest request, string key)
  {
    return request.Query[key].FirstOrDefault();
  }

  public static string? GetRouteValue(this HttpRequest request, string key)
  {
    return request.RouteValues[key] as string;
  }

  //-----------------------------------------------------------------------------------------------

  public static bool IsAbsoluteUrl(string url)
  {
    return url.StartsWith("http", StringComparison.OrdinalIgnoreCase);
  }

  //-----------------------------------------------------------------------------------------------

}