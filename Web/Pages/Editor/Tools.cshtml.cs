namespace Void.Platform.Web;

[AllowAnonymous]
public class EditorToolsPage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  public required List<Account.Game> Tools { get; set; } = new List<Account.Game>();

  public bool HasTools { get { return Tools.Count > 0; } }
  public bool HasNoTools { get { return !HasTools; } }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet()
  {
    Tools = LoadTools();
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  private List<Account.Game> LoadTools()
  {
    return App.Account.GetPublicTools();
  }

  //-----------------------------------------------------------------------------------------------
}