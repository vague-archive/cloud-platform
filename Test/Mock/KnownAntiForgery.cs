namespace Void.Platform.Test;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Void.Platform.Web;

//=================================================================================================
//
// Summary:
//   A custom IAntiforgery implementation using a KNOWN CSRF TOKEN
//
//=================================================================================================

internal class KnownAntiForgery : IAntiforgery
{
  public const string Token = "TEST-CSRF-TOKEN";

  public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
  {
    return new AntiforgeryTokenSet(Token, Token, Config.CsrfFieldName, Config.CsrfHeaderName);
  }

  public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
  {
    return new AntiforgeryTokenSet(Token, Token, Config.CsrfFieldName, Config.CsrfHeaderName);
  }

  public void SetCookieTokenAndHeader(HttpContext httpContext)
  {
    httpContext.Response.Cookies.Append(Config.CsrfCookieName, Token, new CookieOptions { HttpOnly = false });
    httpContext.Response.Headers[Config.CsrfHeaderName] = Token;
  }

  public async Task ValidateRequestAsync(HttpContext httpContext)
  {
    if (!await IsRequestValidAsync(httpContext))
    {
      throw new AntiforgeryValidationException("Invalid CSRF token");
    }
  }

  public Task<bool> IsRequestValidAsync(HttpContext httpContext)
  {
    var token = GetToken(httpContext);
    return Task.FromResult(token != null && token == Token);
  }

  private string? GetToken(HttpContext httpContext)
  {
    if (httpContext.Request.Headers.TryGetValue("X-CSRF-TOKEN", out var headerToken))
      return headerToken;

    if (httpContext.Request.HasFormContentType && httpContext.Request.Form.TryGetValue(Config.CsrfFieldName, out var formToken))
      return formToken;

    if (httpContext.Request.Query.TryGetValue(Config.CsrfFieldName, out var queryToken))
      return queryToken;

    return null;
  }
}