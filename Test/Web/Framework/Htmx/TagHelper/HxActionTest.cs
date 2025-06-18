namespace Void.Platform.Web.Htmx;

public class HxActionTagHelperTest : TestCase
{
  //
  // Ugh, its impossible to test because it
  // requires a dependency injected IUrlHelperFactory
  // and it's almost impossible (certainly very painful)
  // to construct one of those and it's also almost
  // impossible (certainly very painful) to use NSubstitute
  // to mock it out because its not a pure interface but also
  // has extension methods attached, and it's innapropriate
  // to use Moq (even if it would work) because it now
  // includes spyware...
  //
  // Good grief, the dotnet DI/OO abstractions are horrific
  //
}