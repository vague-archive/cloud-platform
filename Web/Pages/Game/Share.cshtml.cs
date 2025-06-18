namespace Void.Platform.Web;

[LoadCurrentGame]
public class ShareGamePage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  private Crypto.Encryptor Encryptor { get; init; }

  public ShareGamePage(Crypto.Encryptor encryptor)
  {
    Encryptor = encryptor;
  }

  //-----------------------------------------------------------------------------------------------

  public List<Share.Branch> Branches { get; set; } = new List<Share.Branch>();
  public bool MultipleDeployers { get; set; } = true;

  public bool HasBranches
  {
    get
    {
      return Branches.Count > 0;
    }
  }

  public bool HasDescription
  {
    get
    {
      return Current.Game.Description is not null;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet()
  {
    LoadBranches();
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostPinBranch(string slug, bool isPinned)
  {
    var branch = App.Share.GetBranch(Current.Game, slug);
    if (branch is not null)
    {
      var result = App.Share.PinBranch(branch, isPinned);
      RuntimeAssert.Succeeded(result);
      return Partial("Share/PinButton", result.Value);
    }
    return NotFound();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGetBranchPasswordDialog(string slug)
  {
    var branch = App.Share.GetBranch(Current.Game, slug);
    if (branch is not null)
    {
      branch.DecryptPassword(Encryptor);
      return Partial("Share/PasswordDialog", branch);
    }
    return NotFound();
  }

  public IActionResult OnPostSetBranchPassword(string slug, bool enabled, string password)
  {
    var branch = App.Share.GetBranch(Current.Game, slug);
    if (branch is not null)
    {
      if (enabled && password is not null)
      {
        App.Share.SetBranchPassword(branch, password);
      }
      else
      {
        App.Share.ClearBranchPassword(branch);
      }
      return Partial("Share/PasswordButton", branch);
    }
    return NotFound();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostDeleteBranch(string slug)
  {
    var branch = App.Share.GetBranch(Current.Game, slug);
    if (branch is not null)
    {
      App.Share.DeleteBranch(branch);
      LoadBranches();
      return Partial("Share/BranchesCard", this);
    }
    return NotFound();
  }

  //-----------------------------------------------------------------------------------------------

  public void LoadBranches()
  {
    Branches = App.Share.GetActiveBranchesForGame(Current.Game);
    MultipleDeployers = Branches.Select(b => b.ActiveDeploy!.DeployedBy).Distinct().Count() > 1;
  }

  //-----------------------------------------------------------------------------------------------
}