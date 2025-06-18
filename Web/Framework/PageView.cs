namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc.ModelBinding;

public class PageView
{
  //-----------------------------------------------------------------------------------------------

  private HttpContext ctx;
  private ViewDataDictionary data;
  private ModelStateDictionary state;

  public PageView(HttpContext ctx, ViewDataDictionary data, ModelStateDictionary state)
  {
    this.ctx = ctx;
    this.data = data;
    this.state = state;
  }

  //===============================================================================================
  // VIEW DATA HELPERS
  //===============================================================================================

  public string Title
  {
    get
    {
      return GetOptionalString(Key.Title) ?? "Welcome";
    }
    set
    {
      Set(Key.Title, value);
    }
  }

  //-----------------------------------------------------------------------------------------------

  public bool HasHeaderPartial
  {
    get
    {
      return Has(Key.HeaderPartial);
    }
  }

  public bool HasHeaderTitle
  {
    get
    {
      return Has(Key.HeaderTitle) && !Has(Key.HeaderPartial);
    }
  }

  public string HeaderPartial
  {
    get
    {
      return GetString(Key.HeaderPartial);
    }
    set
    {
      Set(Key.HeaderPartial, value);
    }
  }

  public string HeaderTitle
  {
    get
    {
      return GetString(Key.HeaderTitle);
    }
    set
    {
      Set(Key.HeaderTitle, value);
    }
  }

  //===============================================================================================
  // PAGE ROUTE HELPERS
  //===============================================================================================

  private string? route;
  public string Route
  {
    get
    {
      return route ??= GetCurrentRoute(ctx);
    }
  }

  public bool Matches(string target)
  {
    return Matches(Route, target);
  }

  //===============================================================================================
  // PAGE MODEL STATE ERRORS
  //===============================================================================================

  public bool HasErrors
  {
    get
    {
      return !HasNoErrors;
    }
  }

  public bool HasNoErrors
  {
    get
    {
      return state.ErrorCount == 0;
    }
  }

  public bool HasError(string field)
  {
    return (state[field]?.Errors?.Count ?? 0) > 0;
  }

  public string GetFirstError(string field)
  {
    var error = state[field]?.Errors?.FirstOrDefault()?.ErrorMessage;
    RuntimeAssert.Present(error);
    return error;
  }

  //-----------------------------------------------------------------------------------------------

  public void Invalidate<T>(Result<T> result, string? prefix = null)
  {
    if (result.Failed)
    {
      switch (result.Error)
      {
        case Validation.Errors errors:
          Invalidate(errors, prefix);
          break;
        default:
          throw new Exception($"unexpected {result.Error.GetType()}");
      }
    }
  }

  public void Invalidate(Validation.Errors errors, string? prefix = null)
  {
    prefix = prefix is not null ? $"{prefix}." : "";
    foreach (var error in errors)
    {
      Invalidate($"{prefix}{error.Property}", error.Message);
    }
  }

  public void Invalidate(string property, string error)
  {
    state.AddModelError(property, error);
  }

  //===============================================================================================
  // GENERAL PURPOSE STATIC HELPER METHODS
  //===============================================================================================

  public static bool Matches(string currentRoute, string targetRoute)
  {
    return currentRoute.StartsWith(targetRoute, StringComparison.OrdinalIgnoreCase);
  }

  public static string GetCurrentRoute(HttpContext ctx)
  {
    if (IsRazorPage(ctx, out var page))
      return page;
    else if (IsControllerAction(ctx, out var action))
      return action;
    else
      throw RuntimeAssert.Failure("cannot determine current route");
  }

  private static bool IsRazorPage(HttpContext ctx, out string route)
  {
    var page = ctx.GetRouteValue("page")?.ToString();
    if (page is not null)
    {
      route = page;
      return true;
    }
    else
    {
      route = "unknown";
      return false;
    }
  }

  private static bool IsControllerAction(HttpContext ctx, out string route)
  {
    var controller = ctx.GetRouteValue("controller")?.ToString();
    var action = ctx.GetRouteValue("action")?.ToString();
    if (controller is not null && action is not null)
    {
      route = $"/{controller}/{action}";
      return true;
    }
    else
    {
      route = "unknown";
      return false;
    }
  }

  //===============================================================================================
  // PRIVATE HELPERS
  //===============================================================================================

  private static class Key
  {
    public static string Title = "Title";
    public static string HeaderPartial = "HeaderPartial";
    public static string HeaderTitle = "HeaderTitle";
  }

  private string? GetOptionalString(string key)
  {
    return data[key] as string;
  }

  private string GetString(string key)
  {
    var value = GetOptionalString(key);
    RuntimeAssert.Present(value);
    return value;
  }

  private void Set(string key, string value)
  {
    data[key] = value;
  }

  private bool Has(string key)
  {
    return data.ContainsKey(key);
  }

  //-----------------------------------------------------------------------------------------------
}