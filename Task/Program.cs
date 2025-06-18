namespace Void.Platform.Task;

public static class Program
{
  public static int Main(string[] args)
  {
    DotNetEnv.Env.TraversePath().Load();

    var app = new CommandApp();
    app.Configure(config =>
    {
      config.ValidateExamples();
      config.SetApplicationName("Task");
      config.AddBranch("db", db =>
      {
        db.AddCommand<ResetDatabaseCommand>("reset");
        db.AddCommand<DropDatabaseCommand>("drop");
        db.AddCommand<CreateDatabaseCommand>("create");
        db.AddCommand<MigrateDatabaseCommand>("migrate");
        db.AddCommand<LoadFixturesCommand>("fixtures");
        db.AddCommand<PrepareDatabaseCommand>("prepare");
        db.AddCommand<NewMigrationCommand>("new");
      });
    });
    return app.Run(args);
  }
}