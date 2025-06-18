namespace Void.Platform.Domain;

using PostmarkDotNet;

public class PostmarkMailer : Mailer
{
  public PostmarkMailer(MailerConfig config, ILogger logger) : base(config, logger)
  {
  }

  public bool Enabled
  {
    get
    {
      return Config.ApiToken is not null;
    }
  }

  protected async override Task<Result> DeliverNow(MailTemplate mail)
  {
    if (Enabled)
    {
      var message = new TemplatedPostmarkMessage()
      {
        To = mail.To,
        From = mail.From,
        TemplateAlias = mail.Template,
        TemplateModel = mail.Data,
      };
      var client = new PostmarkClient(Config.ApiToken);
      var response = await client.SendMessageAsync(message);
      if (response.ErrorCode == 0)
      {
        return Result.Ok();
      }
      else
      {
        return Result.Fail($"postmark {response.ErrorCode}: {response.Message}");
      }
    }
    else
    {
      return Result.Ok();
    }
  }
}