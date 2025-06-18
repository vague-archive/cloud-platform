namespace Void.Platform.Web;

[LoadCurrentGame]
public class GameSettingsPage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  [BindProperty]
  public required Account.UpdateGameCommand UpdateGame { get; set; } = new Account.UpdateGameCommand();

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet()
  {
    BuildUpdateGameCommand();
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostUpdateGame()
  {
    var result = App.Account.UpdateGame(Current.Game, UpdateGame);
    if (result.Failed)
    {
      Current.Page.Invalidate(result, nameof(UpdateGame));
      return Partial("Game/Settings/Active", this);
    }

    // slug has changed, so need full page refresh
    var redirectUrl = Url.SettingsPage(Current.Organization, result.Value);
    return Response
      .Htmx()
      .Redirect(redirectUrl);
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostCancelUpdateGame()
  {
    BuildUpdateGameCommand();
    return Partial("Game/Settings/Active", this);
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostArchiveGame()
  {
    App.Account.ArchiveGame(Current.Game);
    return Response
      .Htmx()
      .Refresh();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostRestoreGame()
  {
    App.Account.RestoreGame(Current.Game);
    return Response
      .Htmx()
      .Refresh();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostDeleteGame()
  {
    App.Account.DeleteGame(Current.Game);
    return Response
      .Htmx()
      .Redirect(Url.GamesPage(Current.Organization));
  }

  //-----------------------------------------------------------------------------------------------

  private void BuildUpdateGameCommand()
  {
    UpdateGame = new Account.UpdateGameCommand
    {
      Name = Current.Game.Name,
      Slug = Current.Game.Slug,
      Description = Current.Game.Description,
    };
  }

  //-----------------------------------------------------------------------------------------------
}