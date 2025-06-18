namespace Void.Platform.Test;

using System.Diagnostics.CodeAnalysis;

public class CustomAssert : Xunit.Assert
{
  //-----------------------------------------------------------------------------------------------

  public static AssertHttp Http = new AssertHttp();
  public static AssertHtml Html = new AssertHtml();
  public static AssertJson Json = new AssertJson();
  public static AssertDomain Domain = new AssertDomain();

  //-----------------------------------------------------------------------------------------------

  public class Preconditions : CustomAssert
  {
    // convenience to make precondition assertions stand out from postcondition (post ACT) assertions
    // e.g.
    //   Assert.Precondition.Present(input)
    // vs
    //   Assert.Present(output)
    //
  }

  //-----------------------------------------------------------------------------------------------

  public static T Present<T>([NotNull] T? value) where T : class
  {
    Assert.NotNull(value);
    return value;
  }

  public static T Present<T>([NotNull] T? value) where T : struct
  {
    return Assert.NotNull(value);
  }

  public static void Absent([MaybeNull] object? value)
  {
    Assert.Null(value);
  }

  //-----------------------------------------------------------------------------------------------

  public static void Equal(string expected, Uri actual)
  {
    Assert.Equal(expected, actual.ToString());
  }

  //-----------------------------------------------------------------------------------------------

  public static void Equal(Instant expected, Instant? actual)
  {
    Assert.Equal(
      expected.ToIso8601(),
      actual?.ToIso8601());
  }

  public static void CloseEnough(Instant expected, Instant actual, int milliseconds = 1000)
  {
    var gap = Math.Abs((actual - expected).TotalMilliseconds);
    if (gap > milliseconds)
    {
      Assert.Equal(expected, actual);
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static void Succeeded(Result result)
  {
    Assert.True(result.Succeeded);
    Assert.False(result.Failed);
  }

  public static T Succeeded<T>(Result<T> result)
  {
    if (result.Failed)
    {
      Assert.Fail($"expected result to succeed, but was: {result.Error.Format()}");
    }
    Assert.True(result.Succeeded);
    Assert.False(result.Failed);
    return result.Value;
  }

  public static IError Failed(Result result)
  {
    Assert.True(result.Failed);
    Assert.False(result.Succeeded);
    return result.Error;
  }

  public static IError Failed<T>(Result<T> result)
  {
    Assert.True(result.Failed);
    Assert.False(result.Succeeded);
    return result.Error;
  }

  public static Validation.Errors FailedValidation<T>(Result<T> result)
  {
    var error = Failed(result);
    return Assert.IsType<Validation.Errors>(error);
  }

  //-----------------------------------------------------------------------------------------------

  public static void RuntimeAssertion(string expected, Action fn)
  {
    var ex = Assert.Throws<RuntimeAssertion>(fn);
    Assert.Equal(expected, ex.Message);
  }

  //-----------------------------------------------------------------------------------------------

  public static MailTemplate Mailed(SandboxMailer mailer, string template, string to)
  {
    Assert.True(mailer.History.Count > 0);
    var mail = mailer.History.Pop();
    Assert.Equal(template, mail.Template);
    Assert.Equal(to, mail.To);
    Assert.Equal(TestConfig.SupportEmail, mail.From);
    Assert.Equal(TestConfig.ProductName, mail.Data["product_name"]);
    Assert.Equal(TestConfig.PublicUrl, mail.Data["product_url"]);
    Assert.Equal(TestConfig.SupportEmail, mail.Data["support_email"]);
    return mail;
  }

  public static void NothingMailed(SandboxMailer mailer)
  {
    Assert.Empty(mailer.History);
  }

  //-----------------------------------------------------------------------------------------------

  public static void LooksLikeJwt(string value)
  {
    Assert.Equal(3, value.Split(".").Length); // header.payload.signature
  }

  //-----------------------------------------------------------------------------------------------
}