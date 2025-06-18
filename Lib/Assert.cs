namespace Void.Platform.Lib;

using System.Diagnostics.CodeAnalysis;

public class RuntimeAssertion : Exception
{
  public RuntimeAssertion(string message) : base(message)
  {
  }
}

public class RuntimeAssert
{
  //-----------------------------------------------------------------------------------------------

  public static void Fail(string message)
  {
    throw Failure(message);
  }

  public static RuntimeAssertion Failure(string message)
  {
    return new RuntimeAssertion(message);
  }

  //-----------------------------------------------------------------------------------------------

  public static void True(bool value, string? message = null)
  {
    if (value == false)
    {
      throw new RuntimeAssertion(message ?? "value is not true");
    }
  }

  public static void False(bool value, string? message = null)
  {
    if (value == true)
    {
      throw new RuntimeAssertion(message ?? "value expected to be true, but was false");
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static void Equal<T>(T expected, T value, string? message = null)
  {
    if (!EqualityComparer<T>.Default.Equals(expected, value))
    {
      throw new RuntimeAssertion(message ?? $"{value} is not equal to {expected}");
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static T Present<T>([NotNull] T? value, string? message = null)
  {
    if (value is null)
      throw new RuntimeAssertion(message ?? "value is missing");
    return value;
  }

  public static void Absent([MaybeNull] object? value, string? message = null)
  {
    if (value is not null)
      throw new RuntimeAssertion(message ?? "value is unexpectedly present");
  }

  //-----------------------------------------------------------------------------------------------

  public static T Succeeded<T>(Result<T> result)
  {
    RuntimeAssert.True(result.Succeeded);
    return result.Value;
  }

  //-----------------------------------------------------------------------------------------------
}