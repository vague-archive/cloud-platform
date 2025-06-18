namespace Void.Platform.Web;

public class PageFlash
{
  //-----------------------------------------------------------------------------------------------

  private ITempDataDictionary data;

  public PageFlash(ITempDataDictionary data)
  {
    this.data = data;
  }

  //-----------------------------------------------------------------------------------------------

  public string? GetString(string key)
  {
    if (data.ContainsKey(key))
      return data[key] as string;
    else
      return null;
  }

  public bool GetBool(string key, bool defaultValue = false)
  {
    var value = GetString(key);
    if (value is not null)
      return value.ToLower() == "true";
    return defaultValue;
  }

  //-----------------------------------------------------------------------------------------------

  public PageFlash Set(string key, string? value)
  {
    data[key] = value;
    return this;
  }

  public PageFlash Set(string key, bool value)
  {
    data[key] = value.ToString().ToLower();
    return this;
  }

  //-----------------------------------------------------------------------------------------------
}