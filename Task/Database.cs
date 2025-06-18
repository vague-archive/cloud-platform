namespace Void.Platform.Task;

//-------------------------------------------------------------------------------------------------

public class ResetDatabaseCommand : Command<Settings>
{
  public override int Execute(CommandContext context, Settings settings)
  {
    var databaseUrl = settings.DatabaseUrl();
    DropDatabaseCommand.Drop(databaseUrl);
    CreateDatabaseCommand.Create(databaseUrl);
    MigrateDatabaseCommand.Migrate(databaseUrl, false);
    LoadFixturesCommand.Load(settings.Logger, databaseUrl);
    return 0;
  }
}

//-------------------------------------------------------------------------------------------------

public class PrepareDatabaseCommand : Command<Settings>
{
  public override int Execute(CommandContext context, Settings settings)
  {
    var databaseUrl = settings.DatabaseUrl();
    MigrateDatabaseCommand.Migrate(databaseUrl, true);
    LoadFixturesCommand.Load(settings.Logger, databaseUrl);
    return 0;
  }
}

//-------------------------------------------------------------------------------------------------

public class DropDatabaseCommand : Command<Settings>
{
  public override int Execute(CommandContext context, Settings settings)
  {
    Drop(settings.DatabaseUrl());
    return 0;
  }

  internal static void Drop(string databaseUrl)
  {
    if (Domain.Database.Drop(databaseUrl))
    {
      AnsiConsole.MarkupLine($"[red]Dropped[/] database [navy]{Domain.Database.Name(databaseUrl)}[/]");
    }
  }
}

//-------------------------------------------------------------------------------------------------

public class CreateDatabaseCommand : Command<Settings>
{
  public override int Execute(CommandContext context, Settings settings)
  {
    Create(settings.DatabaseUrl());
    return 0;
  }

  internal static void Create(string databaseUrl)
  {
    if (Domain.Database.Create(databaseUrl))
    {
      AnsiConsole.MarkupLine($"[green4]Created[/] database [navy]{Domain.Database.Name(databaseUrl)}[/]");
    }
  }
}

//-------------------------------------------------------------------------------------------------

public class MigrateDatabaseCommand : Command<Settings>
{
  public override int Execute(CommandContext context, Settings settings)
  {
    Migrate(settings.DatabaseUrl(true), true);
    return 0;
  }

  internal static void Migrate(string databaseUrl, bool logging)
  {
    Domain.Database.Migrate(databaseUrl, logging);
    AnsiConsole.MarkupLine($"[green4]Migrated[/] database [navy]{Domain.Database.Name(databaseUrl)}[/]");
  }
}

//-------------------------------------------------------------------------------------------------

public class LoadFixturesCommand : Command<Settings>
{
  public override int Execute(CommandContext context, Settings settings)
  {
    Load(settings.Logger, settings.DatabaseUrl());
    return 0;
  }

  internal static void Load(ILogger logger, string databaseUrl)
  {
    Fixture.Loader.Load(logger, databaseUrl);
    AnsiConsole.MarkupLine($"[green4]Loaded fixtures[/] into database [navy]{Domain.Database.Name(databaseUrl)}[/]");
  }
}

//-------------------------------------------------------------------------------------------------

public class NewMigrationSettings : CommandSettings
{
  [CommandArgument(0, "<name>")]
  [Description("new migration name")]
  public required string Name { get; set; }
}

public class NewMigrationCommand : Command<NewMigrationSettings>
{
  public override int Execute(CommandContext context, NewMigrationSettings settings)
  {
    NewMigration(settings.Name);
    return 0;
  }

  internal static void NewMigration(string name)
  {
    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

    var content = $$"""
    namespace Void.Platform.Domain;

    [Migration({{timestamp}})]
    public class {{name}}Migration : Migration
    {
      public override void Up()
      {
      }
    }
    """;

    var fileName = Path.Combine(
      "Domain",
      "Database",
      "Migration",
      $"{timestamp}_{name}.cs"
    );

    File.WriteAllText(fileName, content);
    Console.WriteLine($"CREATED NEW MIGRATION {fileName}");
  }
}

//-------------------------------------------------------------------------------------------------

public enum Env
{
  Dev,
  Test,
  Prod,
}

public class Settings : CommandSettings
{
  [CommandArgument(0, "[env]")]
  [Description("environment (dev|test|prod)")]
  [DefaultValue(Env.Dev)]
  public Env Env { get; set; } = Env.Dev;

  public ILogger Logger { get; init; } = new Logger();

  public string DatabaseUrl(bool allowedInProduction = false)
  {
    switch (Env)
    {
      case Env.Dev:
        return Web.Config.DatabaseUrl;
      case Env.Test:
        return Web.Config.TestDatabaseUrl;
      case Env.Prod:
        if (allowedInProduction)
        {
          return Web.Config.ProductionDatabaseUrl;
        }
        else
        {
          throw new Exception("this command is not allowed in production");
        }

      default:
        throw new Exception($"unknown env {Env}");
    }
  }
}

//-------------------------------------------------------------------------------------------------