namespace Void.Platform.Lib;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog; // a private implementation detail - using strangler pattern to encapsulate serilog here
using Sentry.AspNetCore; // a private implementation detail - using strangler pattern to encapsulate sentry here

//-------------------------------------------------------------------------------------------------

public enum LogLevel
{
  Verbose,
  Debug,
  Information,
  Warning,
  Error,
  Fatal
}

public interface ILogger
{
  void Report(Exception exception, string message, params object?[]? values);
  void Error(string message, params object?[]? values);
  void Warning(string message, params object?[]? values);
  void Information(string message, params object?[]? values);
  void Debug(string message, params object?[]? values);
  void TestOutput(string message, params object?[]? values);
}

//-------------------------------------------------------------------------------------------------

public class Logger : ILogger
{
  private Serilog.ILogger serilog;

  public Logger(Serilog.ILogger serilog)
  {
    this.serilog = serilog;
  }

  public Logger(LogLevel level = LogLevel.Information)
  {
    this.serilog = BuildSerilogLogger(level);
  }

  public void Report(Exception exception, string message, params object?[]? values)
  {
    serilog.Error(exception, message, values);
    SentrySdk.CaptureException(exception);
  }

  public void Error(string message, params object?[]? values)
    => serilog.Error(message, values);

  public void Warning(string message, params object?[]? values)
    => serilog.Warning(message, values);

  public void Information(string message, params object?[]? values)
    => serilog.Information(message, values);

  public void Debug(string message, params object?[]? values)
    => serilog.Debug(message, values);

  //-----------------------------------------------------------------------------------------------

  public virtual void TestOutput(string message, params object?[]? values)
    => serilog.Debug(message, values);

  //-----------------------------------------------------------------------------------------------

  public Serilog.ILogger AsSerilog
  {
    get
    {
      return serilog;
    }
  }

  private static Logger? none;
  public static Logger None
  {
    get
    {
      return none ??= new Logger(Serilog.Core.Logger.None);
    }
  }

  public static Serilog.Events.LogEventLevel ToSerilog(LogLevel level)
  {
    switch (level)
    {
      case LogLevel.Verbose: return Serilog.Events.LogEventLevel.Verbose;
      case LogLevel.Debug: return Serilog.Events.LogEventLevel.Debug;
      case LogLevel.Information: return Serilog.Events.LogEventLevel.Information;
      case LogLevel.Warning: return Serilog.Events.LogEventLevel.Warning;
      case LogLevel.Error: return Serilog.Events.LogEventLevel.Error;
      case LogLevel.Fatal: return Serilog.Events.LogEventLevel.Fatal;
      default:
        throw new InvalidOperationException();
    }
  }

  private static Serilog.ILogger BuildSerilogLogger(LogLevel level)
  {
    return new Serilog.LoggerConfiguration()
      .MinimumLevel.Is(ToSerilog(level))
      .Filter.ByExcluding(SuppressLogEvent)
      .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", ToSerilog(LogLevel.Warning))
      .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", ToSerilog(LogLevel.Warning))
      .MinimumLevel.Override("Microsoft.AspNetCore.Routing", ToSerilog(LogLevel.Warning))
      .MinimumLevel.Override("Microsoft.AspNetCore.StaticFiles", ToSerilog(LogLevel.Warning))
      .MinimumLevel.Override("System.Net.Http.HttpClient", ToSerilog(LogLevel.Warning))
      .WriteTo.Console()
      .CreateLogger();
  }

  private static int pingCount = 0;
  private const int MAX_PING = 30;
  public static bool SuppressLogEvent(Serilog.Events.LogEvent logEvent)
  {
    if (logEvent.Properties.TryGetValue("RequestMethod", out var methodProperty))
    {
      var method = methodProperty.ToString().Trim('"');
      if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        return true;
    }

    if (logEvent.Properties.TryGetValue("RequestPath", out var pathProperty))
    {
      var path = pathProperty.ToString().Trim('"');
      if (path.StartsWith("/ping") && pingCount++ >= MAX_PING)
      {
        if (pingCount == MAX_PING + 1)
        {
          Console.WriteLine("suppressing further pings");
        }
        return true;
      }
    }
    return false;
  }
}

//-------------------------------------------------------------------------------------------------

public static class LoggerExtensions
{
  public static IWebHostBuilder ConfigureVoidLogger(this IWebHostBuilder builder, string? sentryEndpoint)
  {
    if (sentryEndpoint is not null)
    {
      builder.UseSentry((SentryAspNetCoreOptions options) =>
      {
        options.Dsn = sentryEndpoint;
        options.Debug = false;
        options.SendDefaultPii = true;
      });
    }
    return builder;
  }

  public static IServiceCollection AddVoidLogger(this IServiceCollection sp, LogLevel level)
  {
    var logger = new Logger(level);
    return sp
      .AddSerilog(logger.AsSerilog)
      .AddSingleton<ILogger>(logger);
  }

  public static IApplicationBuilder UseVoidRequestLogging(this IApplicationBuilder builder)
  {
    builder.UseSerilogRequestLogging(options =>
    {
      var logger = builder.ApplicationServices.GetRequiredService<Serilog.ILogger>();
      RuntimeAssert.Present(logger);
      options.MessageTemplate = "{RequestMethod} {RequestPath}{RequestQueryString} {StatusCode} in {Elapsed:0.0000} ms";
      options.Logger = logger;

      options.GetLevel = (httpContext, elapsed, ex) =>
      {
        return Serilog.Events.LogEventLevel.Information;
      };

      options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
      {
        diagnosticContext.Set("RequestPath", httpContext.Request.Path);
        diagnosticContext.Set("RequestQueryString", httpContext.Request.QueryString.HasValue
          ? httpContext.Request.QueryString.Value
          : "");
      };
    });
    return builder;
  }
}