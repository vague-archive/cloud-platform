namespace Void.Platform.Web.Htmx;

using Microsoft.AspNetCore.Razor.TagHelpers;

public class HxBoostTagHelperTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestHxBoostTagHelper()
  {
    var tagHelperContext = BuildTagHelperContext();
    var tagHelperOutput = BuildTagHelperOutput();

    var tagHelper = new HxBoostTagHelper();

    tagHelper.Boost = true;
    tagHelper.Process(tagHelperContext, tagHelperOutput);

    Assert.Equal("true", tagHelperOutput.Attributes["hx-boost"].Value?.ToString());

    tagHelper.Boost = false;
    tagHelper.Process(tagHelperContext, tagHelperOutput);

    Assert.Null(tagHelperOutput.Attributes["hx-boost"]);
  }

  //-----------------------------------------------------------------------------------------------

  private TagHelperContext BuildTagHelperContext(string uniqueId = "test")
  {
    return new TagHelperContext(
      new TagHelperAttributeList(),
      new Dictionary<object, object>(),
      uniqueId
    );
  }

  private TagHelperOutput BuildTagHelperOutput(string tagName = "div")
  {
    return new TagHelperOutput(
      tagName,
      attributes: new TagHelperAttributeList(),
      getChildContentAsync: (useCachedResult, encoder) =>
        Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetContent("will never see this"))
      );
  }

  //-----------------------------------------------------------------------------------------------
}