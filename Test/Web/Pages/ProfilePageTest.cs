namespace Void.Platform.Web;

public class ProfilePageTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/profile");
      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/profile",
          expectedTitle: "Profile");
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Get("/profile");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2Fprofile", location);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestUpdateProfile()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");

      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"UpdateProfile.Name", "New Name"},
        {"UpdateProfile.TimeZone", "Europe/Rome"},
        {"UpdateProfile.Locale", "en-GB"},
      });

      var response = await test.HxPost("/profile?handler=UpdateProfile", formData);
      Assert.Http.Refresh(response);

      var reloaded = test.Factory.LoadUser(user.Id);
      Assert.Equal("New Name", reloaded.Name);
      Assert.Equal("Europe/Rome", reloaded.TimeZone);
      Assert.Equal("en-GB", reloaded.Locale);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestCancelUpdateProfile()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var response = await test.HxPost("/profile?handler=CancelUpdateProfile");
      var doc = Assert.Html.Partial(response);
      Assert.Html.Select("[qa=my-profile]", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGenerateAccessToken()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var response = await test.HxPost("/profile?handler=GenerateAccessToken");
      var doc = Assert.Html.Partial(response);
      Assert.Html.Select("[qa=access-tokens] [qa=generated-token]", doc);
    }
  }

  //-----------------------------------------------------------------------------------------------
}