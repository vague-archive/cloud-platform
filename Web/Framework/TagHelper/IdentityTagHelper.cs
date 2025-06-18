namespace Void.Platform.Web.TagHelpers;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("identity-label")]
public class IdentityTagHelper : TagHelper
{
  [HtmlAttributeName("provider")]
  public Account.IdentityProvider Provider { get; set; }

  [HtmlAttributeName("for")]
  public Account.Identity? Identity { get; set; }

  [HtmlAttributeName("class")]
  public string? Classes { get; set; }

  public override void Process(TagHelperContext context, TagHelperOutput output)
  {
    output.TagName = "span";
    output.TagMode = TagMode.StartTagAndEndTag;
    output.Attributes.SetAttribute("class", $"{Classes} flex whitespace-nowrap items-center gap-2");

    var icon = new TagBuilder("i");
    icon.AddCssClass(GetIcon());
    icon.AddCssClass("text-20 border bg-gray-100 p-1 rounded-full");

    output.Content.Clear();
    output.Content.AppendHtml(icon);

    if (HasUserName)
    {
      var userName = new TagBuilder("span");
      userName.AddCssClass("overflow-hidden text-gray-600");
      userName.InnerHtml.Append(GetUserName());
      output.Content.AppendHtml(userName);
    }
  }

  private bool HasUserName
  {
    get
    {
      return Identity is not null;
    }
  }

  private string GetUserName()
  {
    RuntimeAssert.Present(Identity);
    return Identity.UserName;
  }

  private string GetIcon()
  {
    if (Identity is not null)
      return GetIcon(Identity.Provider);
    else
      return GetIcon(Provider);
  }

  private static string GetIcon(Account.IdentityProvider provider)
  {
    switch (provider)
    {
      case Account.IdentityProvider.GitHub: return "iconoir-github";
      case Account.IdentityProvider.Discord: return "iconoir-discord";
      default:
        throw new NotSupportedException();
    }
  }
}