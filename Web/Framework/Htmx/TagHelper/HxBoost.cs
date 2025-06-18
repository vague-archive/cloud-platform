namespace Void.Platform.Web.Htmx;

using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("a", Attributes = "asp-page")]
public class HxBoostTagHelper : TagHelper
{
  public override void Process(TagHelperContext context, TagHelperOutput output)
  {
    if (Boost)
    {
      output.Attributes.SetAttribute("hx-boost", "true");
    }
    else
    {
      output.Attributes.RemoveAll("hx-boost");
    }
  }

  [HtmlAttributeName("boost")]
  public bool Boost { get; set; } = true;
}