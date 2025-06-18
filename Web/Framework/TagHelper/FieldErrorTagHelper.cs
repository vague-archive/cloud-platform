namespace Void.Platform.Web.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;

[HtmlTargetElement("field-error", Attributes = "for")]
public class FieldErrorTagHelper : TagHelper
{
  private Current Current { get; init; }

  public FieldErrorTagHelper(Current current)
  {
    Current = current;
  }

  [HtmlAttributeName("for")]
  public string FieldName { get; set; } = "";

  [HtmlAttributeName("model")]
  public object? Model { get; set; }

  public override void Process(TagHelperContext context, TagHelperOutput output)
  {
    if (Current.Page.HasError(FieldName))
    {
      var error = Current.Page.GetFirstError(FieldName);
      var span = new TagBuilder("span");
      span.InnerHtml.Append(error);
      output.Content.AppendHtml(span);
      output.Attributes.Add("for", FieldName);
    }
    else
    {
      output.SuppressOutput();
    }
  }

  public override int Order => int.MinValue;
}