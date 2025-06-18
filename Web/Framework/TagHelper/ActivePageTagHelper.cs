namespace Void.Platform.Web;

using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("a", Attributes = "asp-page")]
public class ActivePageTagHelper : TagHelper
{
  private Current Current { get; init; }

  public ActivePageTagHelper(Current current)
  {
    Current = current;
  }

  [HtmlAttributeName("active-class")]
  public string ActiveClass { get; set; } = "active";

  public override void Process(TagHelperContext context, TagHelperOutput output)
  {
    if (String.IsNullOrWhiteSpace(ActiveClass))
      return;

    var targetPage = context.AllAttributes["asp-page"]?.Value?.ToString() ?? "";
    if (Current.Page.Matches(targetPage))
    {
      if (output.Attributes.TryGetAttribute("class", out var classAttribute))
      {
        output.Attributes.SetAttribute("class", $"{classAttribute.Value} {ActiveClass}");
      }
      else
      {
        output.Attributes.Add("class", ActiveClass);
      }
    }
  }

  public override int Order => int.MinValue;
}