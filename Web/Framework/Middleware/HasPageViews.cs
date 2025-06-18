namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Runtime.CompilerServices;

//=================================================================================================
//
// [HasPageViews] is a filter attribute that needs to be attached to any MVC controller that wants
// to render a View from our path based /Pages directory
//
// It derives the PageRoot from the location of the controller source code module (just like Razor Pages)
// and saves it into a variable that can be injected into the HttpContext for the current request
//
// ... which will then be used by our custom PageViewExpander (see below)
//
//=================================================================================================

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class HasPageViews : ActionFilterAttribute
{
  public string PageRoot { get; }

  public HasPageViews(string? pageRoot = null, [CallerFilePath] string callerFilePath = "")
  {
    PageRoot = pageRoot ?? DerivePageRoot(callerFilePath);
  }

  public override void OnActionExecuting(ActionExecutingContext ctx)
  {
    PageViewExpander.Save(ctx.HttpContext, PageRoot);
  }

  private string DerivePageRoot(string callerFilePath)
  {
    var keyword = "/Pages";
    int index = callerFilePath.IndexOf(keyword);
    RuntimeAssert.True(index >= 0);
    return Path.GetDirectoryName(callerFilePath.Substring(index + keyword.Length))!;
  }
}

//=================================================================================================
//
// The PageViewExpander is a custom view location expander that will allow any MVC controller that
// has been decorated with a [HasPageViews] attribute to perform its view lookup in the style
// of Razor Pages (e.g. based on the location on disk of the source code file)
//
// This allows us to mix both Razor Pages and MVC Controllers in the same "/Pages" directory and
// have them share layouts and partials
//
//=================================================================================================

public class PageViewExpander : IViewLocationExpander
{
  public void PopulateValues(ViewLocationExpanderContext context)
  {
  }

  public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
  {
    var pageRoot = Load(context.ActionContext.HttpContext);

    foreach (var location in viewLocations)
    {
      if (pageRoot is not null)
      {
        yield return location.Replace("{1}", pageRoot);
      }
      else
      {
        yield return location;
      }
    }
  }

  public const string Key = "PageRoot";

  public static void Save(HttpContext ctx, string pageRoot)
  {
    ctx.Items[Key] = pageRoot;
  }

  public static string? Load(HttpContext ctx)
  {
    return ctx.Items[Key] as string;
  }
}