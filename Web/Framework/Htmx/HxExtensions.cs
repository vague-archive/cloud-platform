namespace Void.Platform.Web.Htmx;

public static class HxExtensions
{
  public static bool IsHtmx(this HttpRequest? request)
  {
    return request?.Headers.ContainsKey(HxRequestHeaders.Key.Request) is true;
  }

  public static bool IsHtmx(this HttpRequest? request, out HxRequestHeaders? values)
  {
    var isHtmx = request.IsHtmx();
    values = request is not null && isHtmx ? new HxRequestHeaders(request.Headers) : null;
    return isHtmx;
  }

  public static bool IsHtmx(this HttpResponseMessage response)
  {
    return response.Headers.Contains(HxResponseHeaders.Key.Response) is true;
  }

  public static bool IsHtmxBoosted(this HttpRequest? request)
  {
    return request?.Headers.GetValueOrDefault(HxRequestHeaders.Key.Boosted, false) is true;
  }

  public static bool IsHtmxNonBoosted(this HttpRequest? request)
  {
    return request?.IsHtmx() is true && !request.IsHtmxBoosted();
  }

  public static HxResponseHeaders Htmx(this HttpResponse response)
  {
    return new HxResponseHeaders(response.Headers);
  }

  internal static T GetValueOrDefault<T>(this IHeaderDictionary headers, string key, T @default)
  {
    if (headers.TryGetValue(key, out var values))
    {
      var value = Convert.ChangeType(values.First(), typeof(T));
      if (value is T)
        return (T) value;
    }
    return @default;
  }
}