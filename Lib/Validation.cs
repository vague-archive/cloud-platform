namespace Void.Platform.Lib;

using FluentResult = FluentValidation.Results.ValidationResult;

public static class Validation
{
  //===============================================================================================
  // ERROR MESSAGES
  //===============================================================================================

  public static readonly string IsMissing = "is missing";
  public static readonly string IsInvalid = "is invalid";
  public static readonly string IsDisabled = "is disabled";
  public static readonly string NotFound = "not found";

  public static string TooLong(int max)
  {
    return $"must be less than {max} characters";
  }

  //===============================================================================================
  // ERRORS suitable for a Result<T>
  //===============================================================================================

  public static Result Fail(FluentResult result) =>
    new Result(new Errors(result));

  public static Result Fail(List<Validation.Error> errors) =>
    new Result(new Errors(errors));

  public static Result Fail(string property, string message) =>
    new Result(new Errors(property, message));

  //-----------------------------------------------------------------------------------------------

  public record Error(string Property, string Message) : IError
  {
    public string Format() =>
      $"{Property.ToLower()} {Message}";
  }

  //-----------------------------------------------------------------------------------------------

  public record Errors : IError, IEnumerable<Validation.Error>
  {
    private List<Validation.Error> errors;

    public Errors()
    {
      errors = new List<Validation.Error>();
    }

    public Errors(List<Validation.Error> errors)
    {
      this.errors = errors;
    }

    public Errors(string property, string message)
    {
      errors = new List<Validation.Error>
      {
        new Validation.Error(property, message),
      };
    }

    public Errors(FluentResult result)
    {
      errors = result.Errors.Select(e => new Validation.Error(e.PropertyName, e.ErrorMessage)).ToList();
    }

    public string Format() =>
      String.Join(", ", errors.Select(e => e.Format()));

    public IEnumerator<Validation.Error> GetEnumerator() =>
      errors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
      GetEnumerator();

    public int Count =>
      errors.Count;

    public Validation.Error this[int index] =>
      errors[index];

    public void Add(Validation.Error error) =>
      errors.Add(error);
  }

  //===============================================================================================
  // STATIC CONSTRUCTOR
  //===============================================================================================

  static Validation()
  {
    FluentValidation.ValidatorOptions.Global.PropertyNameResolver = (type, memberInfo, expression) =>
    {
      return memberInfo?.Name.ToLowerInvariant();
    };
  }
}