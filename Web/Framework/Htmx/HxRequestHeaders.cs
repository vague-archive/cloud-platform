namespace Void.Platform.Web.Htmx;

public class HxRequestHeaders
{
  public static class Key
  {
    public const string Boosted = "HX-Boosted";
    public const string CurrentUrl = "HX-Current-URL";
    public const string Prompt = "HX-Prompt";
    public const string Request = "HX-Request";
    public const string Target = "HX-Target";
    public const string Trigger = "HX-Trigger";
    public const string TriggerName = "HX-Trigger-Name";
  }

  public string CurrentUrl { get; }
  public string Prompt { get; }
  public string Target { get; }
  public string TriggerName { get; }
  public string Trigger { get; }
  public bool Boosted { get; }

  public HxRequestHeaders(IHeaderDictionary headers)
  {
    CurrentUrl = headers.GetValueOrDefault(Key.CurrentUrl, string.Empty);
    Prompt = headers.GetValueOrDefault(Key.Prompt, string.Empty);
    Target = headers.GetValueOrDefault(Key.Target, string.Empty);
    TriggerName = headers.GetValueOrDefault(Key.TriggerName, string.Empty);
    Trigger = headers.GetValueOrDefault(Key.Trigger, string.Empty);
    Boosted = headers.GetValueOrDefault(Key.Boosted, false);
  }
}