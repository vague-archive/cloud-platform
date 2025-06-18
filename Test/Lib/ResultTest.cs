namespace Void.Platform.Lib;

public class ResultTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  public const string DEFAULT_TITLE = "Example Book";
  public record Book(string Title = DEFAULT_TITLE);

  //===============================================================================================
  // TEST SIMPLE RESULT
  //===============================================================================================

  private Result PerformSimpleAction(bool success)
  {
    if (success)
      return Result.Ok();
    else
      return Result.Fail("uh oh");
  }

  [Fact]
  public void TestSimpleAction()
  {
    var result = PerformSimpleAction(true);
    Assert.Succeeded(result);

    result = PerformSimpleAction(false);
    Assert.Failed(result);
    Assert.Equal("uh oh", result.Error.Format());
  }

  //===============================================================================================
  // TEST TYPED RESULT
  //===============================================================================================

  private Result<Book> PerformTypedAction(bool success)
  {
    if (success)
      return Result.Ok(new Book("title"));
    else
      return Result.Fail("bad book");
  }

  [Fact]
  public void TestTypedAction()
  {
    var result = PerformTypedAction(true);
    Assert.Succeeded(result);
    Assert.Equal("title", result.Value.Title);

    result = PerformTypedAction(false);
    Assert.Failed(result);
    Assert.Equal("bad book", result.Error.Format());
  }

  //===============================================================================================
  // TEST TYPED RESULT WITH CUSTOM ERROR TYPE
  //===============================================================================================

  public enum CustomReason
  {
    IsMissing,
    TooSmall,
    TooLarge,
    Incomprehensible,
    Cliche,
  }

  public class CustomError : IError
  {
    public string Property { get; init; }
    public CustomReason Reason { get; init; }
    public CustomError(string property, CustomReason reason)
    {
      Property = property;
      Reason = reason;
    }
    public string Format() => $"{Property} {Lib.Format.Enum(Reason)}";
  }

  public Result<int> PerformActionWithCustomError(int value)
  {
    if (value < 10)
      return Result.Fail(new CustomError("value", CustomReason.TooSmall));
    else if (value > 100)
      return Result.Fail(new CustomError("value", CustomReason.TooLarge));
    else
      return Result.Ok(value);
  }

  [Fact]
  public void TestActionWithCustomError()
  {
    var result = PerformActionWithCustomError(50);
    Assert.Succeeded(result);
    Assert.Equal(50, result.Value);

    result = PerformActionWithCustomError(0);
    Assert.Failed(result);
    Assert.Equal("value too small", result.Error.Format());
    var error = result.Error as CustomError;
    Assert.Present(error);
    Assert.Equal("value", error.Property);
    Assert.Equal(CustomReason.TooSmall, error.Reason);

    result = PerformActionWithCustomError(1000);
    Assert.Failed(result);
    Assert.Equal("value too large", result.Error.Format());
    error = result.Error as CustomError;
    Assert.Present(error);
    Assert.Equal("value", error.Property);
    Assert.Equal(CustomReason.TooLarge, error.Reason);
  }

  //-----------------------------------------------------------------------------------------------
}