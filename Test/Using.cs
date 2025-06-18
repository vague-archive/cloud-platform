global using NSubstitute;
global using RichardSzalay.MockHttp;
global using System.Data;

// global using Xunit;
global using TestContext = Xunit.TestContext;
global using FactAttribute = Xunit.FactAttribute;
global using ITestOutputHelper = Xunit.ITestOutputHelper;
global using Assert = Void.Platform.Test.CustomAssert;

global using Void.Platform.Domain;
global using Void.Platform.Fixture;
global using Void.Platform.Lib;
global using Void.Platform.Test;
global using Aws = Void.Platform.Lib.Aws;

global using Instant = NodaTime.Instant;
global using Duration = NodaTime.Duration;
global using ZonedDateTime = NodaTime.ZonedDateTime;
global using LocalDateTime = NodaTime.LocalDateTime;
global using DateTimeZoneProviders = NodaTime.DateTimeZoneProviders;

namespace Void.Platform.Test
{
  public static class Global
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
    {
      // need to force some static constructors to run before tests (comment out to see what fails)
      System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Validation).TypeHandle);
    }
  }
}