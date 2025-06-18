namespace Void.Platform.Web;

using Microsoft.AspNetCore.Mvc.Filters;

//=================================================================================================
// IMPORTANT: this class is used by both
//   * /{org}/games
//   * /{org}/tools
//
// the only difference being the GamePurpose
//=================================================================================================

[LoadCurrentOrganization]
public class OrganizationGamesPage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  public required Account.GamePurpose Purpose { get; set; } = Account.GamePurpose.Game;
  public required List<Account.Game> ActiveGames { get; set; } = new List<Account.Game>();
  public required List<Account.Game> ArchivedGames { get; set; } = new List<Account.Game>();

  //-----------------------------------------------------------------------------------------------

  [BindProperty]
  public required Account.CreateGameCommand CreateGame { get; set; } = new Account.CreateGameCommand();

  //-----------------------------------------------------------------------------------------------

  public bool HasActiveGames
  {
    get
    {
      return ActiveGames.Count > 0;
    }
  }

  public bool HasArchivedGames
  {
    get
    {
      return ArchivedGames.Count > 0;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public override void OnPageHandlerSelected(PageHandlerSelectedContext context)
  {
    DerivePurpose();
    base.OnPageHandlerSelected(context);
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet()
  {
    LoadGames();
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostCreateGame()
  {
    var result = App.Account.CreateGame(Current.Organization, CreateGame);
    if (result.Failed)
    {
      Current.Page.Invalidate(result, nameof(CreateGame));
      return Partial("Games/List", this);
    }
    LoadGames();
    return Partial("Games/List", this);
  }

  public IActionResult OnPostCancelCreateGame()
  {
    LoadGames();
    return Partial("Games/List", this);
  }

  //-----------------------------------------------------------------------------------------------

  private void DerivePurpose()
  {
    if (HttpContext.Request.Path.ToString().EndsWith("tools", StringComparison.OrdinalIgnoreCase))
    {
      Purpose = Account.GamePurpose.Tool;
    }
    else
    {
      Purpose = Account.GamePurpose.Game;
    }
  }

  private void LoadGames()
  {
    CreateGame = new Account.CreateGameCommand
    {
      Purpose = Purpose,
    };
    var all = App.Account.GetGamesForOrganization(Current.Organization);
    var games = all.Where(g => g.Purpose == Purpose);
    ActiveGames = games.Where(g => !g.IsArchived).ToList();
    ArchivedGames = games.Where(g => g.IsArchived).ToList();
  }

  //-----------------------------------------------------------------------------------------------
}