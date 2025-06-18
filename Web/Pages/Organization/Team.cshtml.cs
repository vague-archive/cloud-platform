namespace Void.Platform.Web;

[LoadCurrentOrganization]
public class OrganizationTeamPage : BasePage
{
  //-----------------------------------------------------------------------------------------------

  public required List<Account.Member> Members { get; set; }
  public required List<Account.Token> Invites { get; set; }

  [BindProperty]
  public required Account.SendInviteCommand SendInvite { get; set; } = new Account.SendInviteCommand();

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnGet()
  {
    LoadMembers();
    LoadInvites();
    PrepareSendInviteCommand();
    return Page();
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostDisconnectMember(long userId)
  {
    var user = App.Account.GetUserById(userId);
    if (user is null)
      return new NotFoundResult();

    App.Account.DisconnectMember(Current.Organization, user);

    LoadMembers();
    return Partial("Team/Members", this);
  }

  //-----------------------------------------------------------------------------------------------

  public async Task<IActionResult> OnPostSendInvite()
  {
    SendInvite.ActionUrl = Url.JoinPage(full: true);
    var result = await App.Account.SendInvite(Current.Organization, SendInvite);
    if (result.Failed)
    {
      Current.Page.Invalidate(result, nameof(SendInvite));
    }
    else
    {
      PrepareSendInviteCommand();
    }

    LoadInvites();
    return Partial("Team/Invites", this);
  }

  public IActionResult OnPostSendInviteCancel()
  {
    LoadInvites();
    return Partial("Team/Invites", this);
  }

  //-----------------------------------------------------------------------------------------------

  public IActionResult OnPostRetractInvite(long inviteId)
  {
    var invite = App.Account.GetInviteById(inviteId);
    if (invite is null)
      return NotFound();

    var result = App.Account.RetractInvite(invite);
    RuntimeAssert.True(result.Succeeded);

    PrepareSendInviteCommand();
    LoadInvites();
    return Partial("Team/Invites", this);
  }

  //-----------------------------------------------------------------------------------------------

  private void LoadMembers()
  {
    Members = App.Account.GetOrganizationMembers(Current.Organization);
  }

  private void LoadInvites()
  {
    Invites = App.Account.GetInvitesFor(Current.Organization);
  }

  private void PrepareSendInviteCommand()
  {
    SendInvite = new Account.SendInviteCommand();
  }

  //-----------------------------------------------------------------------------------------------
}