namespace Void.Platform.Domain;

//-------------------------------------------------------------------------------------------------

public interface IMailer
{
  public MailTemplate Build<T>(string template, string to, T data) where T : class;
  public Task Deliver<T>(string template, string to, T data) where T : class;
  public Task Deliver(MailTemplate mail);
  public Task MechanicalTurk(string message);
}

//-------------------------------------------------------------------------------------------------

public record MailerConfig
{
  public string? ApiToken { get; init; }
  public required string ProductName { get; init; }
  public required Uri ProductUrl { get; init; }
  public required string SupportEmail { get; init; }
}

public record MailTemplate
{
  public required string From { get; init; }
  public required string To { get; init; }
  public required string Template { get; init; }
  public required MailData Data { get; init; }
}

public class MailData : Dictionary<string, object>
{
}

//-------------------------------------------------------------------------------------------------

public abstract class Mailer : IMailer
{
  public static class TemplateKey
  {
    public const string ProductName = "product_name";
    public const string ProductUrl = "product_url";
    public const string SupportEmail = "support_email";
  }
  public ILogger Logger { get; init; }
  public MailerConfig Config { get; init; }

  public Mailer(MailerConfig config, ILogger logger)
  {
    Config = config;
    Logger = logger;
  }

  public MailTemplate Build<T>(string template, string to, T data) where T : class
  {
    var mailData = new MailData();
    mailData.Add(TemplateKey.ProductName, Config.ProductName);
    mailData.Add(TemplateKey.ProductUrl, Config.ProductUrl.ToString());
    mailData.Add(TemplateKey.SupportEmail, Config.SupportEmail);

    foreach (var property in data.GetType().GetProperties())
    {
      var value = property.GetValue(data);
      if (value is not null)
        mailData.Add(property.Name, value);
    }

    return new MailTemplate
    {
      From = Config.SupportEmail,
      To = to,
      Template = template,
      Data = mailData,
    };
  }

  public async Task Deliver<T>(string template, string to, T data) where T : class
  {
    await Deliver(Build(template, to, data));
  }

  public async Task Deliver(MailTemplate mail)
  {
    Logger.Information($"[DELIVERING MAIL] {Json.Serialize(mail)}");
    var result = await DeliverNow(mail);
    if (result.Failed)
      throw new Exception(result.Error.Format());
  }

  public Task MechanicalTurk(string message)
  {
    Logger.Warning("[MECHANICAL TURK] {message}", message);
    return Deliver("mechanical-turk", "jake@void.dev", new
    {
      message,
    });
  }

  protected abstract Task<Result> DeliverNow(MailTemplate mail);
}

//-------------------------------------------------------------------------------------------------