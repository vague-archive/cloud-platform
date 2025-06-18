namespace Void.Platform.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class PageViewTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  const string ExampleTitle = "Hello World";

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestPageTitle()
  {
    var page = BuildPageView();
    Assert.Equal("Welcome", page.Title);
    page.Title = ExampleTitle;
    Assert.Equal(ExampleTitle, page.Title);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestPageHasError()
  {
    var page = BuildPageView();

    Assert.True(page.HasNoErrors);
    Assert.False(page.HasError("Name"));
    Assert.False(page.HasError("Email"));
    Assert.False(page.HasError("Age"));

    page.Invalidate("Name", "Is Missing");

    Assert.False(page.HasNoErrors);
    Assert.True(page.HasError("Name"));
    Assert.False(page.HasError("Email"));
    Assert.False(page.HasError("Age"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestPageHasMultipleErrors()
  {
    var page = BuildPageView();

    Assert.True(page.HasNoErrors);
    Assert.False(page.HasError("Name"));
    Assert.False(page.HasError("Email"));
    Assert.False(page.HasError("Age"));

    page.Invalidate(new Validation.Errors
    {
      new Validation.Error("Name", "is missing"),
      new Validation.Error("Email", "is invalid"),
    });

    Assert.False(page.HasNoErrors);
    Assert.True(page.HasError("Name"));
    Assert.True(page.HasError("Email"));
    Assert.False(page.HasError("Age"));

    Assert.Equal("is missing", page.GetFirstError("Name"));
    Assert.Equal("is invalid", page.GetFirstError("Email"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestPageHasMultipleErrorsWithPrefix()
  {
    var page = BuildPageView();

    Assert.True(page.HasNoErrors);
    Assert.False(page.HasError("Command.Name"));
    Assert.False(page.HasError("Command.Email"));
    Assert.False(page.HasError("Command.Age"));

    page.Invalidate(new Validation.Errors
    {
      new Validation.Error("Name", "is missing"),
      new Validation.Error("Email", "is invalid"),
    }, "Command.");

    Assert.False(page.HasNoErrors);
    Assert.True(page.HasError("Command.Name"));
    Assert.True(page.HasError("Command.Email"));
    Assert.False(page.HasError("Command.Age"));

    Assert.Equal("is missing", page.GetFirstError("Command.Name"));
    Assert.Equal("is invalid", page.GetFirstError("Command.Email"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGetCurrentRoute()
  {
    var ctx = new DefaultHttpContext();

    Assert.RuntimeAssertion("cannot determine current route", () =>
    {
      PageView.GetCurrentRoute(ctx);
    });

    ctx.Request.RouteValues = new Microsoft.AspNetCore.Routing.RouteValueDictionary
    {
      { "page", "/Foo/Bar" },
    };
    Assert.Equal("/Foo/Bar", PageView.GetCurrentRoute(ctx));

    ctx.Request.RouteValues = new Microsoft.AspNetCore.Routing.RouteValueDictionary
    {
      { "controller", "MyController" },
      { "action", "MyAction" },
    };
    Assert.Equal("/MyController/MyAction", PageView.GetCurrentRoute(ctx));
  }

  //-----------------------------------------------------------------------------------------------

  private PageView BuildPageView()
  {
    var ctx = new DefaultHttpContext();
    var provider = new EmptyModelMetadataProvider();
    var msd = new ModelStateDictionary();
    var vdd = new ViewDataDictionary(provider, msd);
    return new PageView(ctx, vdd, msd);
  }

  //-----------------------------------------------------------------------------------------------
}