namespace Void.Platform.Web;

public class HomePageTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Login("active");
      var response = await test.Get("/");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("/atari", location);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestGetPageAnonymous()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Anonymous();
      var response = await test.Get("/");
      var location = Assert.Http.Redirect(response);
      Assert.Equal("https://localhost/login?origin=%2F", location);
    }
  }

  //-----------------------------------------------------------------------------------------------
}