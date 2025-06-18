namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class LoadCurrent : Attribute, IActionFilter, IPageFilter
{
  //-----------------------------------------------------------------------------------------------

  public void OnActionExecuting(ActionExecutingContext ctx)
  {
    var current = GetService<Current>(ctx.HttpContext);
    if (ctx.Controller is PageController pageController)
      current.For(pageController);
    else if (ctx.Controller is ApiController apiController)
      current.For(apiController);
    else
      RuntimeAssert.Fail("unexpected controller");
  }

  public void OnActionExecuted(ActionExecutedContext ctx)
  {
  }

  //-----------------------------------------------------------------------------------------------

  public void OnPageHandlerSelected(PageHandlerSelectedContext ctx)
  {
    var current = GetService<Current>(ctx.HttpContext);
    if (ctx.HandlerInstance is BasePage page)
      current.For(page);
    else
      RuntimeAssert.Fail("unexpected page handler");
  }

  public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
  {
  }

  public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
  {
  }

  //-----------------------------------------------------------------------------------------------

  private T GetService<T>(HttpContext ctx) where T : class
  {
    return ctx.RequestServices.GetRequiredService<T>();
  }

  //-----------------------------------------------------------------------------------------------
}