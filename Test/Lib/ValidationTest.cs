namespace Void.Platform.Lib;

using FluentValidation;
using ValidationResult = FluentValidation.Results.ValidationResult;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

public class ValidationTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestMessages()
  {
    Assert.Equal("is missing", Validation.IsMissing);
    Assert.Equal("is invalid", Validation.IsInvalid);
    Assert.Equal("not found", Validation.NotFound);
    Assert.Equal("must be less than 42 characters", Validation.TooLong(42));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestExampleValidation()
  {
    var cmd = new ExampleCommand();
    var validator = new ExampleValidator();
    var result = validator.Validate(cmd);

    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
    Assert.Equal("email", result.Errors[0].PropertyName);
    Assert.Equal("is missing", result.Errors[0].ErrorMessage);
  }

  //===============================================================================================
  // TEST TYPED RESULT WITH SINGLE PROPERTY ERROR
  //===============================================================================================

  public Result<int> PerformActionWithPropertyError(bool success)
  {
    if (success)
      return Result.Ok(42);
    else
      return Validation.Fail("title", "was boring");
  }

  [Fact]
  public void TestActionWithPropertyError()
  {
    var result = PerformActionWithPropertyError(true);
    Assert.Succeeded(result);
    Assert.Equal(42, result.Value);

    result = PerformActionWithPropertyError(false);
    var errors = Assert.FailedValidation(result);
    Assert.Equal("title was boring", errors.Format());
    var error = Assert.Present(errors[0]);
    Assert.Equal("title", error.Property);
    Assert.Equal("was boring", error.Message);
  }

  //===============================================================================================
  // TEST TYPED RESULT WITH FULL FLUENT VALIDATION RESULT
  //===============================================================================================

  public Result<int> PerformActionWithFluentValidationResult(bool success)
  {
    if (success)
      return Result.Ok(42);
    else
    {
      var result = new ValidationResult
      {
        Errors = new List<ValidationFailure>
        {
          new ValidationFailure("email", "is missing"),
          new ValidationFailure("password", "is missing"),
          new ValidationFailure("name", "is silly"),
        },
      };
      return Validation.Fail(result);
    }
  }

  [Fact]
  public void TestActionWithFluentValidationResult()
  {
    var result = PerformActionWithFluentValidationResult(true);
    Assert.Succeeded(result);
    Assert.Equal(42, result.Value);

    result = PerformActionWithFluentValidationResult(false);
    var errors = Assert.FailedValidation(result);
    Assert.Equal("email is missing, password is missing, name is silly", errors.Format());

    Assert.Equal(3, errors.Count);

    var error1 = Assert.Present(errors[0]);
    var error2 = Assert.Present(errors[1]);
    var error3 = Assert.Present(errors[2]);

    Assert.Equal("email is missing", error1.Format());
    Assert.Equal("password is missing", error2.Format());
    Assert.Equal("name is silly", error3.Format());

    Assert.Equal("email", error1.Property);
    Assert.Equal("password", error2.Property);
    Assert.Equal("name", error3.Property);

    Assert.Equal("is missing", error1.Message);
    Assert.Equal("is missing", error2.Message);
    Assert.Equal("is silly", error3.Message);
  }

  //-----------------------------------------------------------------------------------------------

  private class ExampleCommand
  {
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
  }

  private class ExampleValidator : AbstractValidator<ExampleCommand>
  {
    public ExampleValidator()
    {
      RuleFor(cmd => cmd.Email)
        .NotEmpty().WithMessage(Validation.IsMissing);

      RuleFor(cmd => cmd.Email)
        .EmailAddress()
        .When(cmd => !string.IsNullOrWhiteSpace(cmd.Email))
        .WithMessage(Validation.IsInvalid);

      RuleFor(cmd => cmd.Password)
        .NotEmpty().WithMessage(Validation.IsMissing);
    }
  }

  //-----------------------------------------------------------------------------------------------
}