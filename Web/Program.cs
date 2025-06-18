namespace Void.Platform.Web;

using DotNetEnv.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

public class Program
{
  //-----------------------------------------------------------------------------------------------

  public static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
      Args = args
    });

    var env = builder.Environment;
    var config = BuildConfig(env, args);

    builder.WebHost
      .ConfigureVoidWebHost(config)
      .ConfigureVoidLogger(config.Web.SentryEndpoint);

    builder.Services
      .ConfigureVoidRouteOptions()
      .ConfigureVoidJsonOptions();

    builder.Services
      .AddVoidConfig(config)
      .AddVoidFirewall(config.Enable.Firewall)
      .AddVoidLogger(config.Web.LogLevel)
      .AddVoidClock()
      .AddVoidRandom()
      .AddVoidDatabase(config)
      .AddVoidCache(config)
      .AddVoidMailer(config)
      .AddVoidFileStore(config.FileStore.Path, config.FileStore.Bucket)
      .AddVoidAws(config)
      .AddVoidDataProtection(config)
      .AddVoidDomain(config)
      .AddVoidHttpClients()
      .AddVoidHttpContext()
      .AddVoidUrlGenerator()
      .AddVoidFormatter()
      .AddVoidControllers()
      .AddVoidPages()
      .AddVoidCsrfProtection()
      .AddVoidSession(config)
      .AddVoidFlash()
      .AddVoidAuthentication(config)
      .AddVoidAuthorization(config)
      .AddVoidSocialProviders(config)
      .AddVoidHealthChecks()
      .AddVoidSwagger(env)
      .AddVoidMinions();

    using var app = builder.Build();

    app
      .UseVoidForwardedHeaders(config.Web.VpcCidr)
      .UseVoidFirewall()
      .UseVoidRequestLogging()
      .UseVoidErrorHandler(env)
      .UseVoidRouting()
      .UseVoidAuth()
      .UseVoidSession()
      .UseVoidFrameOptions()
      .UseVoidSwagger(env);

    app
      .MapVoidEndpoints()
      .OnStart(config)
      .Run();
  }

  //-----------------------------------------------------------------------------------------------

  private static Config BuildConfig(IWebHostEnvironment env, string[] args)
  {
    if (IsTest(env))
    {
      // in TEST all config is provided as args via TestWebApplication.CreateHost
      return new Config(Env.Test, new ConfigurationBuilder()
        .AddCommandLine(args)
        .Build()
      );
    }
    else if (IsDevelopment(env))
    {
      // in DEV allow .env file, environment variables, or command line
      return new Config(Env.Development, new ConfigurationBuilder()
        .AddDotNetEnv(".env", DotNetEnv.Env.NoEnvVars().TraversePath())
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build()
      );
    }
    else if (IsProduction(env))
    {
      // in PROD only environment variables or command line options are valid
      return new Config(Env.Production, new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build()
      );
    }
    else
    {
      throw new Exception($"unknown environment {env.EnvironmentName}");
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static bool IsDevelopment(IHostEnvironment env) => env.IsDevelopment();
  public static bool IsProduction(IHostEnvironment env) => env.IsProduction();
  public static bool IsTest(IHostEnvironment env) => env.IsEnvironment(Config.TestEnvironmentName);
}

public static class ProgramExtensions
{
  //-----------------------------------------------------------------------------------------------

  public static IWebHostBuilder ConfigureVoidWebHost(this IWebHostBuilder builder, Config config)
  {
    builder.UseUrls($"http://{config.Web.Host}:{config.Web.Port}");
    builder.UseWebRoot(config.Web.PublicRoot);
    return builder;
  }

  //-----------------------------------------------------------------------------------------------

  public static IServiceCollection ConfigureVoidRouteOptions(this IServiceCollection services)
  {
    services.Configure<RouteOptions>(opts =>
    {
      opts.AppendTrailingSlash = false;
    });
    return services;
  }

  public static IServiceCollection ConfigureVoidJsonOptions(this IServiceCollection services)
  {
    // for MVC controller APIs
    services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
    {
      options.JsonSerializerOptions.ConfigureForVoid();
    });
    // for minimal APIs
    services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
    {
      options.SerializerOptions.ConfigureForVoid();
    });

    return services;
  }

  //-----------------------------------------------------------------------------------------------

  public static IServiceCollection AddVoidConfig(this IServiceCollection services, Config config)
  {
    return services
      .AddSingleton<Domain.Config>(config.Domain)
      .AddSingleton<Domain.MailerConfig>(config.Mailer)
      .AddSingleton<Web.Config>(config);
  }

  public static IServiceCollection AddVoidClock(this IServiceCollection services)
  {
    return services.AddSingleton<IClock>(Clock.System);
  }

  public static IServiceCollection AddVoidRandom(this IServiceCollection services)
  {
    return services.AddSingleton<IRandom>(Lib.Random.Default);
  }

  public static IServiceCollection AddVoidDatabase(this IServiceCollection services, Config config)
  {
    return services.AddScoped<IDatabase>(sp =>
    {
      var logger = sp.GetRequiredService<ILogger>();
      return new Database(logger, config.Domain.DatabaseUrl);
    }); // new connection per request
  }

  public static IServiceCollection AddVoidCache(this IServiceCollection services, Config config)
  {
    return services.AddVoidCache(new CacheOptions
    {
      RedisUrl = config.Domain.RedisCacheUrl,
      JsonSerializerOptions = Json.SerializerOptions,
    });
  }

  public static IServiceCollection AddVoidMailer(this IServiceCollection services, Config config)
  {
    return services.AddSingleton<IMailer, PostmarkMailer>();
  }

  public static IServiceCollection AddVoidAws(this IServiceCollection services, Config config)
  {
    return services.AddSingleton<Aws.Client>();
  }

  public static IServiceCollection AddVoidDataProtection(this IServiceCollection services, Config config)
  {
    services.AddDataProtection()
      .PersistKeysToFileSystem(new DirectoryInfo(config.Web.KeysPath));
    var encryptor = new Crypto.Encryptor(config.Web.EncryptKey);
    var passwordHasher = new Crypto.PasswordHasher();
    services.AddSingleton(encryptor);
    services.AddSingleton(passwordHasher);
    services.AddSingleton(sp =>
    {
      var clock = sp.GetRequiredService<IClock>();
      return new Crypto.JwtGenerator(config.Web.SigningKey, clock);
    });
    return services;
  }

  public static IServiceCollection AddVoidDomain(this IServiceCollection services, Config config)
  {
    return services.AddScoped<Application>();
  }

  public static IServiceCollection AddVoidHttpClients(this IServiceCollection services)
  {
    return services.AddHttpClient();
  }

  public static IServiceCollection AddVoidHttpContext(this IServiceCollection services)
  {
    return services.AddHttpContextAccessor();
  }

  public static IServiceCollection AddVoidUrlGenerator(this IServiceCollection services)
  {
    return services
      .AddScoped<IUrlProvider, UrlProvider>()
      .AddScoped<UrlGenerator>();
  }

  public static IServiceCollection AddVoidFormatter(this IServiceCollection services)
  {
    services.AddScoped<Formatter>(sp =>
    {
      var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext!;
      var principal = httpContext.User.Wrap();
      if (principal.IsLoggedIn)
        return new Formatter(
          timeZone: principal.TimeZone,
          locale: principal.Locale
        );
      else
        return new Formatter(
          timeZone: International.DefaultTimeZone,
          locale: International.DefaultLocale
        );
    });
    return services;
  }

  public static IServiceCollection AddVoidControllers(this IServiceCollection services)
  {
    services.AddControllers(options =>
    {
      options.ModelValidatorProviders.Clear(); // we use FluentValidations in our domain layer
    });
    return services;
  }

  public static IServiceCollection AddVoidPages(this IServiceCollection services)
  {
    services.AddScoped<Current>();
    services.AddRazorPages()
      .AddRazorOptions(options =>
      {
        options.PageViewLocationFormats.Clear();
        options.PageViewLocationFormats.Add("/Pages/{1}/{0}.cshtml");
        options.PageViewLocationFormats.Add("/Pages/{0}.cshtml");
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Pages/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Pages/{0}.cshtml");
        options.ViewLocationExpanders.Add(new PageViewExpander());
      });
    return services;
  }

  public static IServiceCollection AddVoidCsrfProtection(this IServiceCollection services)
  {
    services.AddAntiforgery(options =>
    {
      options.Cookie.Name = Config.CsrfCookieName;
      options.HeaderName = Config.CsrfHeaderName;
      options.FormFieldName = Config.CsrfFieldName;
    });
    return services;
  }

  public static IServiceCollection AddVoidSession(this IServiceCollection services, Config config)
  {
    services.AddSession(options =>
    {
      options.Cookie.Name = config.Web.SessionCookie.Name;
      options.Cookie.Path = config.Web.SessionCookie.Path;
      options.Cookie.HttpOnly = config.Web.SessionCookie.HttpOnly;
      options.Cookie.SecurePolicy = config.Web.SessionCookie.Secure;
      options.Cookie.SameSite = config.Web.SessionCookie.SameSite;
      options.Cookie.MaxAge = config.Web.SessionCookie.MaxAge;
      options.Cookie.IsEssential = true;
      options.IdleTimeout = TimeSpan.FromDays(7);
    });
    return services;
  }

  public static IServiceCollection AddVoidFlash(this IServiceCollection services)
  {
    services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
    services.Configure<CookieTempDataProviderOptions>(options =>
    {
      options.Cookie.Name = Config.FlashCookieName;
    });
    return services;
  }

  public static IServiceCollection AddVoidSocialProviders(this IServiceCollection services, Config config)
  {
    var providers = new OAuth.Providers();
    providers.AddGitHub(config.Web.OAuth.GitHub);
    providers.AddDiscord(config.Web.OAuth.Discord);
    services.AddSingleton(providers);
    return services;
  }

  public static IServiceCollection AddVoidHealthChecks(this IServiceCollection services)
  {
    services.AddHealthChecks();
    return services;
  }

  public static IServiceCollection AddVoidSwagger(this IServiceCollection services, IWebHostEnvironment env)
  {
    if (Program.IsDevelopment(env))
    {
      services.AddEndpointsApiExplorer();
      services.AddSwaggerGen(opts =>
        opts.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo()
        {
          Title = Config.SwaggerTitle,
          Description = Config.SwaggerDescription,
          Version = Config.SwaggerVersion,
        })
      );
    }
    return services;
  }

  //-----------------------------------------------------------------------------------------------

  public static IApplicationBuilder UseVoidErrorHandler(this IApplicationBuilder app, IWebHostEnvironment env)
  {
    app.UseWhen(ctx => ctx.Request.IsHtmx() || ctx.Request.Path.StartsWithSegments("/api"), subApp =>
    {
      subApp.UseExceptionHandler(handler =>
        handler.Run(async context =>
          await Results.Problem().ExecuteAsync(context)
        )
      );
    });

    app.UseWhen(ctx => !ctx.Request.IsHtmx() && !ctx.Request.Path.StartsWithSegments("/api"), subApp =>
    {
      subApp.UseStatusCodePagesWithReExecute("/error/{0}");
      if (Program.IsDevelopment(env))
      {
        subApp.UseDeveloperExceptionPage();
      }
      else
      {
        subApp.UseExceptionHandler("/error/500");
      }
    });
    return app;
  }

  public static IApplicationBuilder UseVoidRouting(this IApplicationBuilder app)
  {
    app.UseStaticFiles();
    app.UseRouting();
    return app;
  }

  public static IApplicationBuilder UseVoidAuth(this IApplicationBuilder app)
  {
    app.UseAuthentication();
    app.UseAuthorization();
    return app;
  }

  public static IApplicationBuilder UseVoidSession(this IApplicationBuilder app)
  {
    app.UseSession();
    return app;
  }

  public static IApplicationBuilder UseVoidSwagger(this IApplicationBuilder app, IWebHostEnvironment env)
  {
    if (Program.IsDevelopment(env))
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }
    return app;
  }

  public static IApplicationBuilder UseVoidForwardedHeaders(this IApplicationBuilder app, string cidr)
  {
    var parts = cidr.Split('/');
    RuntimeAssert.True(parts.Length == 2);
    var ip = System.Net.IPAddress.Parse(parts[0]);
    var mask = int.Parse(parts[1]);
    return app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
      ForwardedHeaders = ForwardedHeaders.All,
      KnownNetworks = {
        new IPNetwork(ip, mask)
      }
    });
  }

  //-----------------------------------------------------------------------------------------------

  public static WebApplication MapVoidEndpoints(this WebApplication app)
  {
    app.MapGet("/jake/{label?}", (string? label) => label ?? "Hi Jake");
    app.MapHealthChecks("/ping");
    app.MapControllers();
    app.MapRazorPages();
    app.MapFallback(ctx =>
    {
      var logger = ctx.RequestServices.GetRequiredService<ILogger>();
      if (ctx.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
      {
        logger.Information("[404] - API ROUTE NOT FOUND {route}", ctx.Request.Path);
        ctx.Response.StatusCode = Http.StatusCode.NotFound;
      }
      else
      {
        logger.Information("[404] - UX PAGE ROUTE NOT FOUND {route}", ctx.Request.Path);
        ctx.Response.StatusCode = Http.StatusCode.NotFound;
        ctx.Request.Path = "/Error/404";
      }
      return Task.CompletedTask;
    });
    return app;
  }

  //-----------------------------------------------------------------------------------------------

  public static WebApplication OnStart(this WebApplication app, Config config)
  {
    app.Lifetime.ApplicationStarted.Register(() =>
    {
      var logger = app.Services.GetRequiredService<ILogger>();
      logger.Information("[LIFECYCLE] Application has started");
      config.Log(logger);
    });
    app.Lifetime.ApplicationStopping.Register(() =>
    {
      var logger = app.Services.GetRequiredService<ILogger>();
      logger.Information($"[LIFECYCLE] Shutting Down...");
    });
    app.Lifetime.ApplicationStopped.Register(() =>
    {
      var logger = app.Services.GetRequiredService<ILogger>();
      logger.Information("[LIFECYCLE] Application has shut down");
    });
    return app;
  }

  //-----------------------------------------------------------------------------------------------
}