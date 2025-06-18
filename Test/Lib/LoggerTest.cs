namespace Void.Platform.Lib;

public class LoggerTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestLogLevel()
  {
    Assert.Equal(Serilog.Events.LogEventLevel.Verbose, Lib.Logger.ToSerilog(LogLevel.Verbose));
    Assert.Equal(Serilog.Events.LogEventLevel.Debug, Lib.Logger.ToSerilog(LogLevel.Debug));
    Assert.Equal(Serilog.Events.LogEventLevel.Information, Lib.Logger.ToSerilog(LogLevel.Information));
    Assert.Equal(Serilog.Events.LogEventLevel.Warning, Lib.Logger.ToSerilog(LogLevel.Warning));
    Assert.Equal(Serilog.Events.LogEventLevel.Error, Lib.Logger.ToSerilog(LogLevel.Error));
    Assert.Equal(Serilog.Events.LogEventLevel.Fatal, Lib.Logger.ToSerilog(LogLevel.Fatal));

    Assert.Equal((int) Serilog.Events.LogEventLevel.Verbose, (int) LogLevel.Verbose);
    Assert.Equal((int) Serilog.Events.LogEventLevel.Debug, (int) LogLevel.Debug);
    Assert.Equal((int) Serilog.Events.LogEventLevel.Information, (int) LogLevel.Information);
    Assert.Equal((int) Serilog.Events.LogEventLevel.Warning, (int) LogLevel.Warning);
    Assert.Equal((int) Serilog.Events.LogEventLevel.Error, (int) LogLevel.Error);
    Assert.Equal((int) Serilog.Events.LogEventLevel.Fatal, (int) LogLevel.Fatal);

    Assert.Equal(LogLevel.Verbose, "Verbose".ToEnum<LogLevel>());
    Assert.Equal(LogLevel.Debug, "Debug".ToEnum<LogLevel>());
    Assert.Equal(LogLevel.Information, "Information".ToEnum<LogLevel>());
    Assert.Equal(LogLevel.Warning, "Warning".ToEnum<LogLevel>());
    Assert.Equal(LogLevel.Error, "Error".ToEnum<LogLevel>());
    Assert.Equal(LogLevel.Fatal, "Fatal".ToEnum<LogLevel>());
  }

  //-----------------------------------------------------------------------------------------------
}