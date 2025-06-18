namespace Void.Platform.Web.Alpine;

using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("*", Attributes = "x-data-from")]
public class XDataFromTagHelper : TagHelper
{
  [HtmlAttributeName("x-data-from")]
  public object? XData { get; set; }

  public override void Process(TagHelperContext context, TagHelperOutput output)
  {
    if (context.AllAttributes.Where(a => a.Name.ToLower() == "x-data").Count() > 0)
      throw new InvalidOperationException("use only one of x-data and x-data-from, not both");

    if (XData is null)
      throw new InvalidOperationException("missing x-data-from value");

    string jsonData = Json.Serialize(XData);
    string scriptTag = $"<script type=\"application/json\">{jsonData}</script>";
    output.PreContent.SetHtmlContent(scriptTag + output.PreContent.GetContent());
    output.Attributes.SetAttribute("x-data", "script");
  }
}