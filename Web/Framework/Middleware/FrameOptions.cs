public class FrameOptions
{
  private const string ItemKey = "FrameOptionsAllowed";
  private RequestDelegate Next { get; init; }

  public FrameOptions(RequestDelegate next)
  {
    Next = next;
  }

  public static void AllowFrames(HttpContext ctx, bool allowed = true)
  {
    ctx.Items[ItemKey] = allowed;
  }

  public static bool AreFramesAllowed(HttpContext ctx)
  {
    if (ctx.Items.TryGetValue(ItemKey, out var value) && (value is bool allowed))
      return allowed;
    else
      return false;
  }

  public async Task Invoke(HttpContext ctx)
  {
    ctx.Response.OnStarting(() =>
    {
      if (AreFramesAllowed(ctx))
      {
        ctx.Response.Headers.Remove(Http.Header.XFrameOptions); // was previously added by AddAntiforgery
      }
      return Task.CompletedTask;
    });
    await Next(ctx);
  }
}

public static class FrameOptionsExtensions
{
  public static IApplicationBuilder UseVoidFrameOptions(this IApplicationBuilder builder)
  {
    return builder.UseMiddleware<FrameOptions>();
  }
}