namespace Void.Platform.Web;

public class LoginPageTest : TestCase
{
  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestChooseProviderPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Get("/login?origin=%2Fprofile");
      var doc = Assert.Html.Document(response, expectedTitle: "Login");
      var buttons = doc.QuerySelectorAll("[qa=providers] .btn");
      Assert.Equal(3, buttons.Length);
      Assert.Equal("/login/github?origin=%2Fprofile", buttons[0].GetAttribute("href"));
      Assert.Equal("/login/discord?origin=%2Fprofile", buttons[1].GetAttribute("href"));
      Assert.Equal("/login/password?origin=%2Fprofile", buttons[2].GetAttribute("href"));
      Assert.Html.IsNotLoggedIn(doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginWithPasswordPage()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var response = await test.Get("/login/password?origin=%2Fprofile");

      var doc = Assert.Html.Document(response, expectedTitle: "Login");
      var form = Assert.Html.Form(doc);
      var email = Assert.Html.Input("Command.Email", form);
      var password = Assert.Html.Input("Command.Password", form);

      Assert.Equal("post", form.Method);
      Assert.Equal("/login/password?origin=%2Fprofile", form.Action);
      Assert.Equal("", email.Value);
      Assert.Equal("", password.Value);

      Assert.Html.NoFieldErrors(form);
      Assert.Html.IsNotLoggedIn(doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginWithPasswordSuccess()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Command.Email", user.Email},
        {"Command.Password", "password"}
      });

      var response = await test.Post("/login", formData, redirect: true);

      var doc = Assert.Html.Document(response,
        expectedUrl: "https://localhost/atari/games",
        expectedTitle: "Games - Atari");

      Assert.Html.IsLoggedIn(user, doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginSuccessWithOrigin()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Origin", "/profile"},
        {"Command.Email", user.Email},
        {"Command.Password", "password"}
      });

      var response = await test.Post("/login", formData, redirect: true);

      var doc = Assert.Html.Document(response,
          expectedUrl: "https://localhost/profile",
          expectedTitle: "Profile");

      Assert.Html.IsLoggedIn(user, doc);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginSuccessForCLI()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Origin", "http://localhost/callback"},
        {"Command.Email", user.Email},
        {"Command.Password", "password"}
      });

      var response = await test.Post("/login?CLI=true", formData);
      var location = Assert.Http.Redirect(response);
      Assert.StartsWith("http://localhost/callback?jwt=", location);
      var qp = Http.Params(new Uri(location));
      var jwt = qp.Get("jwt");
      Assert.Present(jwt);
      Assert.LooksLikeJwt(jwt);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginForCliAlreadyLoggedIn()
  {
    using (var test = new WebIntegrationTest(this))
    {
      test.Login("active");
      var response = await test.Get("/login?CLI=true&origin=http://localhost:12345/callback");
      var location = Assert.Http.Redirect(response);
      Assert.StartsWith("http://localhost:12345/callback?jwt=", location);
      var qp = Http.Params(new Uri(location));
      var jwt = qp.Get("jwt");
      Assert.Present(jwt);
      Assert.LooksLikeJwt(jwt);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginMissingFields()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Command.Email", ""},
        {"Command.Password", ""}
      });

      var response = await test.Post("/login", formData);
      var doc = Assert.Html.Document(response,
        expectedTitle: "Login",
        expectedUrl: "https://localhost/login"
      );
      Assert.Html.IsNotLoggedIn(doc);

      var form = Assert.Html.Form(doc);
      Assert.Html.FieldError("is missing", "Command.Email", form);
      Assert.Html.FieldError("is missing", "Command.Password", form);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginInvalidEmail()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Command.Email", "not an email address"},
        {"Command.Password", ""}
      });

      var response = await test.Post("/login", formData);
      var doc = Assert.Html.Document(response,
        expectedTitle: "Login",
        expectedUrl: "https://localhost/login"
      );
      Assert.Html.IsNotLoggedIn(doc);

      var form = Assert.Html.Form(doc);
      Assert.Html.FieldError("is invalid", "Command.Email", form);
      Assert.Html.FieldError("is missing", "Command.Password", form);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginInvalidPassword()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Factory.LoadUser("active");
      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Command.Email", user.Email},
        {"Command.Password", "invalid password"}
      });

      var response = await test.Post("/login", formData);

      var doc = Assert.Html.Document(response,
        expectedTitle: "Login",
        expectedUrl: "https://localhost/login");
      Assert.Html.IsNotLoggedIn(doc);

      var form = Assert.Html.Form(doc);
      Assert.Html.NoFieldError("Command.Email", form);
      Assert.Html.FieldError("is invalid", "Command.Password", form);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginUnknownUser()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Command.Email", "unknown@somewhere.com"},
        {"Command.Password", "password"}
      });

      var response = await test.Post("/login", formData);

      var doc = Assert.Html.Document(response,
        expectedTitle: "Login",
        expectedUrl: "https://localhost/login");
      Assert.Html.IsNotLoggedIn(doc);

      var form = Assert.Html.Form(doc);
      Assert.Html.FieldError("not found", "Command.Email", form);
      Assert.Html.NoFieldError("Command.Password", form);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginDisabledUser()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var user = test.Factory.LoadUser("disabled");
      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Command.Email", user.Email},
        {"Command.Password", "password"}
      });

      var response = await test.Post("/login", formData);

      var doc = Assert.Html.Document(response,
        expectedTitle: "Login",
        expectedUrl: "https://localhost/login");
      Assert.Html.IsNotLoggedIn(doc);

      var form = Assert.Html.Form(doc);
      Assert.Html.FieldError("is disabled", "Command.Email", form);
      Assert.Html.NoFieldError("Command.Password", form);
    }
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public async Task TestLoginMissingCsrfToken()
  {
    using (var test = new WebIntegrationTest(this))
    {
      var formData = test.BuildForm(new Dictionary<string, string>
      {
        {"Command.Email", "jake@void.dev"},
        {"Command.Password", "password"}
      }, withCsrf: false);

      var response = await test.Post("/login", formData);
      Assert.Http.BadRequest(response);
    }
  }

  //-----------------------------------------------------------------------------------------------
}