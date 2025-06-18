namespace Void.Platform.Lib;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
  public static IServiceCollection RemoveService<T>(this IServiceCollection services)
  {
    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
    RuntimeAssert.Present(descriptor);
    services.Remove(descriptor);
    return services;
  }
}