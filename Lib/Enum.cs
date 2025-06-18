namespace Void.Platform.Lib;

public static class EnumExtensions
{
  public static T ToEnum<T>(this string value, bool ignoreCase = true) where T : struct, Enum
  {
    return (T) Enum.Parse(typeof(T), value, ignoreCase);
  }

  public static List<T> ToEnumList<T>(this IEnumerable<string> values, bool ignoreCase = true) where T : struct, Enum
  {
    return values.Select(v => v.ToEnum<T>(ignoreCase)).ToList();
  }
}