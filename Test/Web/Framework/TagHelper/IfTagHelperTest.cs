namespace Void.Platform.Web.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;

public class IfTagHelperTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestIfTagHelperVisible()
  {
    var tagHelperContext = BuildTagHelperContext();
    var tagHelperOutput = BuildTagHelperOutput();

    var tagHelper = new IfTagHelper { Visible = true };
    tagHelper.Process(tagHelperContext, tagHelperOutput);

    Assert.Equal("div", tagHelperOutput.TagName);
    Assert.Equal("hello world", tagHelperOutput.Content.GetContent());
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestIfTagHelperNotVisible()
  {
    var tagHelperContext = BuildTagHelperContext();
    var tagHelperOutput = BuildTagHelperOutput();

    var tagHelper = new IfTagHelper { Visible = false };
    tagHelper.Process(tagHelperContext, tagHelperOutput);

    Assert.Null(tagHelperOutput.TagName);
    Assert.Equal(String.Empty, tagHelperOutput.Content.GetContent());
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

  private TagHelperOutput BuildTagHelperOutput(string tagName = "div", string content = "hello world")
  {
    return new TagHelperOutput(
      tagName,
      attributes: new TagHelperAttributeList(),
      getChildContentAsync: (useCachedResult, encoder) =>
        Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetContent("will never see this"))
      )
    {
      Content = new DefaultTagHelperContent().SetContent(content)
    };
  }

  //-----------------------------------------------------------------------------------------------
}