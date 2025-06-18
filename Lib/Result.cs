namespace Void.Platform.Lib;

//=================================================================================================
// ERROR TYPES
//=================================================================================================

public interface IError
{
  string Format();
}

public record Error(string Message) : IError
{
  public string Format() =>
    Message;
}

//=================================================================================================
// SIMPLE RESULT
//=================================================================================================

public class Result
{
  private IError? error;

  internal Result() { }
  internal Result(IError error) { this.error = error; }

  public bool Succeeded => !Failed;
  public bool Failed => error is not null;
  public IError Error => RuntimeAssert.Present(error);

  public static Result Ok() => new Result();
  public static Result<T> Ok<T>(T value) => new Result<T>(value);
  public static Result Fail(string message) => new Result(new Error(message));
  public static Result Fail(IError error) => new Result(error);
}

//=================================================================================================
// TYPED RESULT
//=================================================================================================

public class Result<T>
{
  private T? value;
  private IError? error;

  internal Result(T value) { this.value = value; }
  internal Result(IError error) { this.error = error; }

  public bool Succeeded => !Failed;
  public bool Failed => error is not null;
  public T Value => RuntimeAssert.Present(value);
  public IError Error => RuntimeAssert.Present(error);

  public static implicit operator Result<T>(Result result)
  {
    RuntimeAssert.True(result.Failed);
    return new Result<T>(result.Error);
  }
}