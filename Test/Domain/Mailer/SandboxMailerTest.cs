namespace Void.Platform.Domain;

public class SandboxMailerTest : TestCase
{
  [Fact]
  public async Task SandboxMailer()
  {
    var mailer = new SandboxMailer(TestConfig.MailerConfig, Logger);

    Assert.Null(mailer.Config.ApiToken);
    Assert.Equal(TestConfig.ProductName, mailer.Config.ProductName);
    Assert.Equal(TestConfig.PublicUrl, mailer.Config.ProductUrl);
    Assert.Equal(TestConfig.SupportEmail, mailer.Config.SupportEmail);

    var template = "example";
    var to1 = "jake@void.dev";
    var to2 = "scarlett@void.dev";

    await mailer.Deliver(template, to1, new
    {
      message = "first",
    });

    await mailer.Deliver(template, to2, new
    {
      message = "second",
    });

    Assert.Equal(2, mailer.History.Count);

    var sent = mailer.History.Pop();
    Assert.Equal(template, sent.Template);
    Assert.Equal(mailer.Config.SupportEmail, sent.From);
    Assert.Equal(to2, sent.To);
    Assert.Equal("second", sent.Data["message"]);
    Assert.Equal(TestConfig.ProductName, sent.Data["product_name"]);
    Assert.Equal(TestConfig.PublicUrl, sent.Data["product_url"]);
    Assert.Equal(TestConfig.SupportEmail, sent.Data["support_email"]);

    sent = mailer.History.Pop();
    Assert.Equal(template, sent.Template);
    Assert.Equal(mailer.Config.SupportEmail, sent.From);
    Assert.Equal(to1, sent.To);
    Assert.Equal("first", sent.Data["message"]);
    Assert.Equal(TestConfig.ProductName, sent.Data["product_name"]);
    Assert.Equal(TestConfig.PublicUrl, sent.Data["product_url"]);
    Assert.Equal(TestConfig.SupportEmail, sent.Data["support_email"]);
  }
}