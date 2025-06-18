namespace Void.Platform.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class PageFlashTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestFlash()
  {
    var tdd = BuildTempDataDictionary();
    var flash = new PageFlash(tdd);

    Assert.Null(tdd.Peek("key1"));
    Assert.Null(tdd.Peek("key2"));

    flash.Set("key1", "value1");
    flash.Set("key2", "value2");

    tdd.Save();
    Assert.Equal("value1", tdd.Peek("key1"));
    Assert.Equal("value2", tdd.Peek("key2"));

    Assert.Equal("value1", flash.GetString("key1"));

    tdd.Save();
    Assert.Null(tdd.Peek("key1"));
    Assert.Equal("value2", tdd.Peek("key2"));

    Assert.Equal("value2", flash.GetString("key2"));

    tdd.Save();
    Assert.Null(flash.GetString("key1"));
    Assert.Null(flash.GetString("key2"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestFlashBool()
  {
    var tdd = BuildTempDataDictionary();
    var flash = new PageFlash(tdd);
    flash.Set("isTrue", true);
    flash.Set("isFalse", false);

    Assert.True(flash.GetBool("isTrue"));
    Assert.True(flash.GetBool("isTrue", false));
    Assert.True(flash.GetBool("isTrue", true));

    Assert.False(flash.GetBool("isFalse"));
    Assert.False(flash.GetBool("isFalse", false));
    Assert.False(flash.GetBool("isFalse", true));

    Assert.False(flash.GetBool("isMissing"));
    Assert.False(flash.GetBool("isMissing", false));
    Assert.True(flash.GetBool("isMissing", true));
  }

  //-----------------------------------------------------------------------------------------------

  private TempDataDictionary BuildTempDataDictionary()
  {
    var context = new DefaultHttpContext();
    var provider = new StubDataProvider();
    return new TempDataDictionary(context, provider);
  }

  private class StubDataProvider : ITempDataProvider
  {
    private IDictionary<string, object> data = new Dictionary<string, object>();

    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
      return data;
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
      data = values;
    }
  }

  //-----------------------------------------------------------------------------------------------
}