namespace Void.Platform.Test;

public class TestLogger : Logger
{
  private ITestOutputHelper? output;

  public TestLogger(ITestOutputHelper? output = null) : base(LogLevel.Fatal)
  {
    this.output = output;
  }

  public override void TestOutput(string message, params object?[]? values)
  {
    if (output is not null && values is not null)
      output.WriteLine(message, values.Select(v => v is null ? "[NULL]" : v));
    else if (output is not null)
      output.WriteLine(message);
    else
      base.TestOutput(message, values);
  }
}