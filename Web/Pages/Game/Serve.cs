namespace Void.Platform.Web;

[HasPageViews]
public class ServeGame : PageController
{
  //-----------------------------------------------------------------------------------------------

  private ILogger Logger { get; init; }
  private IClock Clock { get; init; }
  private Current Current { get; init; }
  private Application App { get; init; }
  private new UrlGenerator Url { get; init; }
  private IFileVersionProvider FileVersionProvider { get; init; }

  public ServeGame(ILogger logger, IClock clock, Current current, Application app, UrlGenerator url, IFileVersionProvider fileVersionProvider)
  {
    Logger = logger;
    Clock = clock;
    Current = current;
    App = app;
    Url = url;
    FileVersionProvider = fileVersionProvider;
  }

  //===============================================================================================
  // SERVE INDEX PAGE
  //===============================================================================================

  [HttpGet("{org}/{game}/preview/{slug}")]   // for backward compatibility with old deno app
  [AllowAnonymous]
  [LoadCurrentGame]
  public IActionResult Preview(string slug)
  {
    var branch = GetBranch(slug);
    if (branch is null)
      return NotFound();
    AddToolHeaders(Current.Game.Purpose == Account.GamePurpose.Tool);
    return Redirect(ServeUrl(branch));
  }

  //-----------------------------------------------------------------------------------------------

  [HttpGet("{org}/{game}/serve/{slug}")]
  [AllowAnonymous]
  [EnforceTrailingSlash] // VERY IMPORTANT for relative asset paths
  [LoadCurrentGame]
  public async Task<IActionResult> Index(string slug)
  {
    var branch = GetBranch(slug);
    var deploy = GetActiveDeploy(branch);
    if (branch is null || deploy is null)
      return NotFound();

    if (PasswordIsInvalid(branch))
      return Redirect(ServePasswordUrl(branch));

    using var stream = await App.FileStore.Load(Path.Combine(deploy.Path, "index.html"));
    if (stream is null)
    {
      Logger.Warning($"[SERVE] no index.html found for {Current.Organization.Slug}/{Current.Game.Slug}/{slug}");
      return NotFound();
    }

    // pre-emptively set cached info so assets can load without DB hit
    await App.Share.SetCachedDeployInfo(Current.Organization, Current.Game, branch, deploy);

    using var reader = new StreamReader(stream);
    var layout = await reader.ReadToEndAsync();

    var meta = @$"
      <meta name=""void:platform:csrf""         content=""{Current.CsrfToken}"" />
      <meta name=""void:platform:organization"" content=""{Current.Organization.Slug}"" />
      <meta name=""void:platform:game""         content=""{Current.Game.Slug}"" />
      <meta name=""void:platform:branch""       content=""{branch.Slug}"" />
      <script defer src=""{GetServeScript()}""></script>
    ";
    layout = layout.Replace("</head>", meta);

    AddCacheControlHeaders();
    AddToolHeaders(Current.Game.Purpose == Account.GamePurpose.Tool);

    return Content(layout, Http.ContentType.Html);
  }

  //===============================================================================================
  // PROMPT FOR GAME PASSWORD
  //===============================================================================================

  [HttpGet("{org}/{game}/serve/{slug}/password")]
  [AllowAnonymous]
  [LoadCurrentGame]
  public IActionResult Password(string slug, string password)
  {
    var branch = GetBranch(slug);
    if (branch is null)
      return NotFound();
    return PasswordPageFor(branch);
  }

  //-----------------------------------------------------------------------------------------------

  [HttpPost("{org}/{game}/serve/{slug}/password")]
  [AllowAnonymous]
  [LoadCurrentGame]
  public IActionResult SubmitPassword(string slug, string password = "")
  {
    var branch = GetBranch(slug);
    if (branch is null)
      return NotFound();

    if (PasswordIsInvalid(branch, password))
      return PasswordPageFor(branch, password, "password is invalid");

    SetPasswordInSession(branch, password);

    return Response
      .Htmx()
      .Redirect(ServeUrl(branch));
  }

  [HttpGet("{org}/{game}/serve/{slug}/password-check")]
  [AllowAnonymous]
  [LoadCurrentGame]
  public IActionResult CheckPassword(string slug)
  {
    var branch = GetBranch(slug);
    if (branch is null)
      return NotFound();

    if (branch.HasPassword)
    {
      var deployPassword = branch.DecryptPassword(App.Encryptor);
      var userPassword = GetPasswordFromSession(branch);
      if (userPassword != deployPassword)
        return StatusCode(StatusCodes.Status403Forbidden);
    }

    return Ok("password is valid");
  }

  //-----------------------------------------------------------------------------------------------

  public record PasswordPage
  {
    public string? Password { get; init; }
    public string? Error { get; init; }
    public required string SubmitUrl { get; init; }
    public bool HasError { get { return Error is not null; } }
  }

  private IActionResult PasswordPageFor(Share.Branch branch, string? password = null, string? error = null)
  {
    return View("Serve/Password", new PasswordPage
    {
      Password = password,
      Error = error,
      SubmitUrl = Url.ServeGamePassword(Current.Organization, Current.Game, branch) + HttpContext.Request.QueryString.Value,
    });
  }

  //-----------------------------------------------------------------------------------------------

  private bool PasswordIsInvalid(Share.Branch branch, string? password = null)
  {
    password = password ?? GetPasswordFromSession(branch);
    return branch.HasPassword && (password != branch.DecryptPassword(App.Encryptor));
  }

  private string? GetPasswordFromSession(Share.Branch branch) =>
    HttpContext.Session.GetString(PasswordSessionKey(branch));

  private void SetPasswordInSession(Share.Branch branch, string password) =>
    HttpContext.Session.SetString(PasswordSessionKey(branch), password);

  private string PasswordSessionKey(Share.Branch branch) =>
    CacheKey.ForGameServePassword(branch);

  //===============================================================================================
  // SERVE ASSETS
  //===============================================================================================

  [HttpGet("{org}/{game}/serve/{slug}/{*asset}")]
  [HttpGet("{org}/{game}/preview/{slug}/{*asset}")] // for backward compatibility with DENO version
  [AllowAnonymous]
  public async Task<IActionResult> Asset(string org, string game, string slug, string asset)
  {
    var info = await App.Share.GetCachedDeployInfo(org, game, slug);
    if (info is null)
      return NotFound();

    var assetPath = Path.Combine(info.FilePath, asset);
    var contentType = Http.DeriveContentType(assetPath);
    var isHtml = contentType.StartsWith(Http.ContentType.Html);
    var isWasm = contentType.StartsWith(Http.ContentType.Wasm);

    if (await NotModifiedSince(assetPath, HttpContext) && !isHtml)
      return StatusCode(Http.StatusCode.NotModified);

    var stream = await App.FileStore.Load(assetPath); // NO 'using' because .NET takes ownership when returning File(stream) below
    if (stream is null)
      return NotFound();

    Response.Headers[Http.Header.ContentType] = contentType;
    AddCacheControlHeaders();
    AddToolHeaders(info.Purpose == Account.GamePurpose.Tool);

    if (isWasm)
    {
      // browser loading WASM without a ContentLength is VERY VERY SLOW
      // so preload it into memory in order to return it with a valid ContentLength
      return await Preload(stream, contentType);
    }
    else if (isHtml)
    {
      Response.Headers[Http.Header.ContentDisposition] = "inline";
      return File(stream, contentType);
    }
    else
    {
      return File(stream, contentType);
    }
  }

  private async Task<IActionResult> Preload(Stream stream, string contentType)
  {
    byte[] buffer;
    using (var ms = new MemoryStream())
    {
      await stream.CopyToAsync(ms);
      buffer = ms.ToArray();
    }
    await stream.DisposeAsync(); // important to dispose of stream because we're no longer relying on .NET to take ownership of it
    return File(buffer, contentType);
  }

  //===============================================================================================
  // PRIVATE IMPLEMENTATION
  //===============================================================================================

  private string GetServeScript()
  {
    // TODO: push this into UrlGenerator ? (after it's been simplified)
    var url = Url.Content("/serve.js");
    var versionedUrl = FileVersionProvider.AddFileVersionToPath(null, url);
    return versionedUrl;
  }

  private void AddToolHeaders(bool enabled)
  {
    if (enabled)
    {
      FrameOptions.AllowFrames(HttpContext);
      Response.Headers[Http.Header.CrossOriginOpenerPolicy] = Http.CORS.SameOrigin;
      Response.Headers[Http.Header.CrossOriginEmbedderPolicy] = Http.CORS.RequireCorp;
      Response.Headers[Http.Header.CrossOriginResourcePolicy] = Http.CORS.CrossOrigin;
      Response.Headers[Http.Header.AccessControlAllowOrigin] = "*";
    }
  }

  //-----------------------------------------------------------------------------------------------

  private void AddCacheControlHeaders()
  {
    Response.Headers[Http.Header.CacheControl] = Http.CacheControl.MustRevalidateStrict;
    Response.Headers[Http.Header.LastModified] = Clock.Now.ToRfc9110();
  }

  private async Task<bool> NotModifiedSince(string assetPath, HttpContext ctx)
  {
    if (ctx.Request.Headers.TryGetValue(Http.Header.IfModifiedSince, out var headerValue))
    {
      if (DateTimeOffset.TryParse(headerValue, out var dt))
      {
        var ifModifiedSince = Instant.FromDateTimeOffset(dt);
        var stat = await App.FileStore.Stat(assetPath);
        return stat is not null && (stat.LastModifiedOn <= ifModifiedSince);
      }
    }
    return false;
  }

  //-----------------------------------------------------------------------------------------------

  private string ServeUrl(Share.Branch branch)
  {
    return Url.ServeGame(Current.Organization, Current.Game, branch) + HttpContext.Request.QueryString.Value;
  }

  private string ServePasswordUrl(Share.Branch branch)
  {
    return Url.ServeGamePassword(Current.Organization, Current.Game, branch) + HttpContext.Request.QueryString.Value;
  }

  //-----------------------------------------------------------------------------------------------

  private Share.Branch? GetBranch(string slug)
  {
    var branch = App.Share.GetBranch(Current.Game, slug);
    if (branch is null)
      Logger.Warning($"[SERVE] missing branch {Current.Organization.Slug}/{Current.Game.Slug}/{slug}");
    return branch;
  }

  private Share.Deploy? GetActiveDeploy(Share.Branch? branch)
  {
    if (branch is null)
      return null;
    var deploy = branch.ActiveDeploy;
    if (deploy is null)
      Logger.Warning($"[SERVE] missing branch {Current.Organization.Slug}/{Current.Game.Slug}/{branch.Slug}");
    return deploy;
  }

  //-----------------------------------------------------------------------------------------------
}