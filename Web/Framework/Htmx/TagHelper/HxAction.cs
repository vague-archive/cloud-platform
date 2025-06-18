namespace Void.Platform.Web.Htmx;

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("*", Attributes = "hx-get")]
[HtmlTargetElement("*", Attributes = "hx-post")]
[HtmlTargetElement("*", Attributes = "hx-delete")]
[HtmlTargetElement("*", Attributes = "hx-put")]
[HtmlTargetElement("*", Attributes = "hx-patch")]
public class HxActionTagHelper : TagHelper
{
  //-----------------------------------------------------------------------------------------------

  private UrlGenerator urlGenerator;

  public HxActionTagHelper(UrlGenerator urlGenerator)
  {
    this.urlGenerator = urlGenerator;
  }

  //-----------------------------------------------------------------------------------------------

  private const string PageAttributeName = "hx-page";
  private const string PageValuesPrefix = "hx-page-";
  private const string PageValuesDictionaryName = "hx-all-page-data";
  private const string PageHandlerAttributeName = "hx-page-handler";

  [HtmlAttributeName(PageAttributeName)]
  public string? Page { get; set; }

  [HtmlAttributeName(PageHandlerAttributeName)]
  public string? PageHandler { get; set; }

  [HtmlAttributeName(PageValuesDictionaryName, DictionaryAttributePrefix = PageValuesPrefix)]
  public IDictionary<string, string?> PageValues { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

  [HtmlAttributeNotBound]
  [ViewContext]
  public ViewContext? ViewContext { get; set; }

  //-----------------------------------------------------------------------------------------------

  public override void Process(TagHelperContext context, TagHelperOutput output)
  {
    RuntimeAssert.Present(context);
    RuntimeAssert.Present(output);

    var attrs = context.AllAttributes.Where(a => Methods.Contains(a.Name.ToLower())).ToList();
    if (attrs.Count > 1)
      throw new InvalidOperationException($"Too many htmx method attributes found. Use only one of {String.Join(", ", Methods)}");

    var attr = attrs.First();

    if (attr.Value is HtmlString s && !string.IsNullOrWhiteSpace(s.Value))
      return;

    RuntimeAssert.Present(Page);

    string? href = urlGenerator.Page(Page, PageHandler, PageValues);
    if (href is null)
      throw new InvalidOperationException("page not found");

    output.Attributes.RemoveAll(attr.Name);
    output.Attributes.Add(attr.Name, href);
  }

  private static readonly List<string> Methods = new()
  {
    "hx-get",
    "hx-post",
    "hx-delete",
    "hx-put",
    "hx-patch"
  };

  public override int Order => int.MinValue;
}