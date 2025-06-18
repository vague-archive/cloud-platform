using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class MaxRequestBodySizeAttribute : ActionFilterAttribute
{
  public long MaxSize { get; init; }

  public MaxRequestBodySizeAttribute(long maxSize)
  {
    MaxSize = maxSize;
  }

  public override void OnActionExecuting(ActionExecutingContext ctx)
  {
    var feature = ctx.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (feature is not null)
    {
      RuntimeAssert.False(feature.IsReadOnly);
      feature.MaxRequestBodySize = MaxSize;
    }
  }
}