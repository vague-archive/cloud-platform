namespace Void.Platform.Domain;

public class SandboxMailer : Mailer
{
  public SandboxMailer(MailerConfig config, ILogger logger) : base(config, logger)
  {
  }

  protected async override Task<Result> DeliverNow(MailTemplate mail)
  {
    History.Push(mail);
    await Task.CompletedTask;
    return Result.Ok();
  }

  public readonly Stack<MailTemplate> History = new Stack<MailTemplate>();
}