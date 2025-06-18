namespace Void.Platform.Test;

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp;

public class AssertHtml
{
  //---------------------------------------------------------------------------------------------

  public IHtmlElement Document(HttpResponseMessage response, int expectedStatus = Http.StatusCode.Ok, string? expectedTitle = null, string? expectedUrl = null)
  {
    var content = Assert.Http.Result(expectedStatus, response);
    Assert.NotNull(content);
    var doc = Document(content);
    if (expectedUrl != null)
    {
      Assert.Http.CurrentUrl(expectedUrl, response);
    }
    if (expectedTitle != null)
    {
      Title(expectedTitle, doc);
    }
    return doc;
  }

  public IHtmlElement Document(string html)
  {
    Assert.Contains("<!DOCTYPE html>", html);
    var context = BrowsingContext.New();
    var parser = context.GetService<IHtmlParser>()!;
    var doc = parser.ParseDocument(html);
    Assert.IsAssignableFrom<IHtmlElement>(doc.DocumentElement);
    return (IHtmlElement) doc.DocumentElement;
  }

  //-----------------------------------------------------------------------------------------------

  public IHtmlElement Partial(HttpResponseMessage response, int expectedStatus = Http.StatusCode.Ok)
  {
    var content = Assert.Http.Result(expectedStatus, response);
    Assert.NotNull(content);
    var context = BrowsingContext.New();
    var parser = context.GetService<IHtmlParser>()!;
    var doc = parser.ParseDocument(content);
    Assert.IsAssignableFrom<IHtmlElement>(doc.DocumentElement);
    return (IHtmlElement) doc.DocumentElement;
  }

  //-----------------------------------------------------------------------------------------------

  public IHtmlElement Select(string selector, IHtmlElement el)
  {
    return Select<IHtmlElement>(selector, el);
  }

  public T Select<T>(string selector, IHtmlElement el)
  {
    var elements = SelectAll<T>(selector, el);
    var count = elements.Count();
    if (count != 1)
    {
      Assert.Fail($"expected to find single element via {selector}, found {count}");
    }
    return elements.First();
  }

  public List<IHtmlElement> SelectAll(string selector, IHtmlElement el)
  {
    return SelectAll<IHtmlElement>(selector, el);
  }

  public List<T> SelectAll<T>(string selector, IHtmlElement el)
  {
    var elements = el.QuerySelectorAll(selector);
    return elements.Select(element =>
    {
      Assert.NotNull(element);
      Assert.IsAssignableFrom<T>(element);
      return (T) element;
    }).ToList();
  }

  public void None(string selector, IHtmlElement el)
  {
    var elements = el.QuerySelectorAll(selector);
    var count = elements.Count();
    if (count != 0)
    {
      Assert.Fail($"expected to find NO elements via {selector}, found {count}");
    }
  }

  //---------------------------------------------------------------------------------------------

  public IHtmlElement Flash(string expected, IHtmlElement el)
  {
    var flash = Select("[qa=flash]", el);
    Assert.Equal(expected, flash.TextContent);
    return flash;
  }

  public void NoFlash(IHtmlElement el)
  {
    None("[qa=flash]", el);
  }

  //---------------------------------------------------------------------------------------------

  public IHtmlFormElement Form(IHtmlElement el)
  {
    return Form("form", el);
  }

  public IHtmlFormElement Form(string selector, IHtmlElement el)
  {
    return Select<IHtmlFormElement>(selector, el);
  }

  public IHtmlInputElement Input(string name, IHtmlFormElement form)
  {
    return Select<IHtmlInputElement>($"input[name='{name}']", form);
  }

  //---------------------------------------------------------------------------------------------

  public void IsNotLoggedIn(IHtmlElement html)
  {
    NoMeta("userId", html);
    NoMeta("userName", html);
    NoMeta("userEmail", html);
  }

  public void IsLoggedIn(long id, string name, string email, IHtmlElement html)
  {
    Meta(id.ToString(), "userId", html);
    Meta(name, "userName", html);
    Meta(email, "userEmail", html);
  }

  public void IsLoggedIn(Account.User user, IHtmlElement html)
  {
    IsLoggedIn(user.Id, user.Name, user.Email, html);
  }

  public void IsLoggedIn(Account.AuthenticatedUser user, IHtmlElement html)
  {
    IsLoggedIn(user.Id, user.Name, user.Email, html);
  }

  public void Title(string expected, IHtmlElement html)
  {
    var title = Select<IHtmlTitleElement>("title", html);
    Assert.Equal(expected, title.Text);
  }

  public void Meta(string expected, string name, IHtmlElement html)
  {
    var selector = $"meta[name='{name}']";
    var element = Select<IHtmlMetaElement>(selector, html);
    Assert.Equal(expected, element.Content);
  }

  public void NoMeta(string name, IHtmlElement html)
  {
    var selector = $"meta[name='{name}']";
    None(selector, html);
  }

  public void FieldError(string expected, string name, IHtmlFormElement form)
  {
    var selector = $"field-error[for='{name}']";
    var el = Select<IHtmlElement>(selector, form);
    Assert.Equal(expected, el.TextContent.Trim());
  }

  public void NoFieldError(string name, IHtmlFormElement form)
  {
    var selector = $"field-error[name='{name}']";
    None(selector, form);
  }

  public void NoFieldErrors(IHtmlFormElement form)
  {
    var selector = "field-error";
    None(selector, form);
  }
}