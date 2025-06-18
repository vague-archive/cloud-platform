namespace Void.Platform.Web.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement(Attributes = "if")]
public class IfTagHelper : TagHelper
{
  [HtmlAttributeName("if")]
  public bool Visible { get; set; } = true;

  public override void Process(TagHelperContext context, TagHelperOutput output)
  {
    if (Visible == false)
    {
      output.SuppressOutput();
    }
  }

  public override int Order => int.MinValue;
}