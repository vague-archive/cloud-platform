namespace Void.Platform.Domain;

using FluentMigrator.Runner.VersionTableInfo;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using System.Reflection;

//-------------------------------------------------------------------------------------------------

public interface IDatabase
{
  public T Transaction<T>(Account.Organization org, Func<T> handler);
  public T Transaction<T>(Account.Game game, Func<T> handler);
  public T Transaction<T>(Share.Branch branch, Func<T> handler);
  public T Transaction<T>(string lockName, Func<T> handler);

  public List<T> Query<T>(string sql);
  public List<T> Query<T>(string sql, object parameters);
  public List<R> SplitQuery<T1, T2, R>(string sql, object parameters, Func<T1, T2, R> handler, string splitOn);
  public T QuerySingle<T>(string sql, object? parameters = null);
  public T? QuerySingleOrDefault<T>(string sql, object parameters);
  public long Insert(string sql, object parameters);
  public int Execute(string sql, object parameters);
  public T? ExecuteScalar<T>(string sql, object parameters);

  // JAKE TODO: start to provide async variations (below). deprecate ^^^^ above

  Task<IEnumerable<T>> QueryAsync<T>(string sql);
  Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters);
  Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, R>(string sql, object parameters, Func<T1, T2, T3, R> handler, string splitOn);
  Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, R>(string sql, Func<T1, T2, T3, R> handler, string splitOn);
  Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, R>(string sql, object parameters, Func<T1, T2, T3, T4, R> handler, string splitOn);
  Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, R>(string sql, Func<T1, T2, T3, T4, R> handler, string splitOn);
  Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, T5, R>(string sql, object parameters, Func<T1, T2, T3, T4, T5, R> handler, string splitOn);
  Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, T5, R>(string sql, Func<T1, T2, T3, T4, T5, R> handler, string splitOn);
  Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, T5, T6, R>(string sql, object parameters, Func<T1, T2, T3, T4, T5, T6, R> handler, string splitOn);
  Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, T5, T6, R>(string sql, Func<T1, T2, T3, T4, T5, T6, R> handler, string splitOn);
}

//-------------------------------------------------------------------------------------------------

[AttributeUsage(AttributeTargets.Property)]
public class DatabaseIgnore : Attribute
{
}

//-------------------------------------------------------------------------------------------------

public class Database : IDatabase, IDisposable
{
  private ILogger Logger { get; init; }
  private IDbConnection Conn { get; init; }
  private IDbTransaction? Tx { get; set; }

  static Database()
  {
    RegisterTypeHandlers();
  }

  private Database(ILogger logger, IDbConnection conn, IDbTransaction? tx = null)
  {
    Logger = logger;
    Conn = conn;
    Tx = tx;
  }

  public Database(ILogger logger, string url)
  {
    Logger = logger;
    Conn = Connection(url);
    Tx = null;
  }

  public static Database Transactional(ILogger logger, string url, IsolationLevel isolationLevel)
  {
    var conn = Connection(url);
    var tx = conn.BeginTransaction(isolationLevel);
    return new Database(logger, conn, tx);
  }

  //-----------------------------------------------------------------------------------------------

  private bool disposed = false;
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (disposed)
      return;

    if (disposing)
    {
      if (Tx is not null)
      {
        Tx.Rollback();
        Tx.Dispose();
      }
      Conn.Close();
      Conn.Dispose();
    }

    disposed = true;
  }

  ~Database()
  {
    Dispose(false);
  }

  //-----------------------------------------------------------------------------------------------

  public T Transaction<T>(Func<T> handler)
  {
    if (Tx is not null)
      return handler(); // mysql doesn't support nested transactions

    try
    {
      using (var tx = Conn.BeginTransaction())
      {
        Tx = tx;
        var result = handler();
        tx.Commit();
        return result;
      }
    }
    finally
    {
      Tx = null;
    }
  }

  //-----------------------------------------------------------------------------------------------

  private const int TransactionLockTimeout = 10; /* seconds */

  public T Transaction<T>(Account.Organization org, Func<T> handler) =>
    Transaction<T>($"org:{org.Id}", handler);

  public T Transaction<T>(Account.Game game, Func<T> handler) =>
    Transaction<T>($"game:{game.Id}", handler);

  public T Transaction<T>(Share.Branch branch, Func<T> handler) =>
    Transaction<T>($"branch:{branch.Id}", handler);

  public T Transaction<T>(string lockName, Func<T> handler)
  {
    Logger.Debug($"[DATABASE] ACQUIRED XACT ADVISORY LOCK {lockName}");
    var gotLock = Conn.ExecuteScalar<bool>("""
      SELECT GET_LOCK(@lockName, @timeout)
    """, new
    {
      lockName = lockName,
      timeout = TransactionLockTimeout,
    }, transaction: Tx);

    RuntimeAssert.True(gotLock, $"could not acquire advisory lock {lockName} within {TransactionLockTimeout} seconds");

    try
    {
      return Transaction<T>(handler);
    }
    finally
    {
      Logger.Debug($"[DATABASE] RELEASED XACT ADVISORY LOCK {lockName}");
      Conn.Execute("""
        SELECT RELEASE_LOCK(@lockName)
      """, new
      {
        lockName = lockName
      }, transaction: Tx);
    }
  }

  //===============================================================================================
  // DAPPER WRAPPERS that respect transaction
  //===============================================================================================

  public List<T> Query<T>(string sql)
  {
    return Conn.Query<T>(sql, null, transaction: Tx).ToList();
  }

  public List<T> Query<T>(string sql, object parameters)
  {
    return Conn.Query<T>(sql, ForDapper(parameters), transaction: Tx).ToList();
  }

  public List<R> SplitQuery<T1, T2, R>(string sql, object parameters, Func<T1, T2, R> handler, string splitOn)
  {
    return Conn.Query<T1, T2, R>(sql, handler, ForDapper(parameters), splitOn: splitOn, transaction: Tx).ToList();
  }

  public T QuerySingle<T>(string sql, object? parameters = null)
  {
    return Conn.QuerySingle<T>(sql, ForDapper(parameters), transaction: Tx);
  }

  public T? QuerySingleOrDefault<T>(string sql, object parameters)
  {
    return Conn.QuerySingleOrDefault<T>(sql, ForDapper(parameters), transaction: Tx);
  }

  public long Insert(string sql, object parameters)
  {
    return Conn.QuerySingle<long>($"{sql}; SELECT LAST_INSERT_ID()", ForDapper(parameters), transaction: Tx);
  }

  public int Execute(string sql, object parameters)
  {
    return Conn.Execute(sql, ForDapper(parameters), transaction: Tx);
  }

  public T? ExecuteScalar<T>(string sql, object parameters)
  {
    var value = Conn.ExecuteScalar(sql, ForDapper(parameters), transaction: Tx);
    if (value is T)
      return (T) value;
    return default(T);
  }

  //===============================================================================================
  // ASYNC METHODS
  //===============================================================================================

  public Task<IEnumerable<T>> QueryAsync<T>(string sql) =>
    Conn.QueryAsync<T>(sql, transaction: Tx);

  public Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters) =>
    Conn.QueryAsync<T>(sql, ForDapper(parameters), transaction: Tx);

  public Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, R>(string sql, object parameters, Func<T1, T2, T3, R> handler, string splitOn) =>
    Conn.QueryAsync<T1, T2, T3, R>(sql, handler, ForDapper(parameters), splitOn: splitOn, transaction: Tx);

  public Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, R>(string sql, Func<T1, T2, T3, R> handler, string splitOn) =>
    Conn.QueryAsync<T1, T2, T3, R>(sql, handler, splitOn: splitOn, transaction: Tx);

  public Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, R>(string sql, object parameters, Func<T1, T2, T3, T4, R> handler, string splitOn) =>
    Conn.QueryAsync<T1, T2, T3, T4, R>(sql, handler, ForDapper(parameters), splitOn: splitOn, transaction: Tx);

  public Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, R>(string sql, Func<T1, T2, T3, T4, R> handler, string splitOn) =>
    Conn.QueryAsync<T1, T2, T3, T4, R>(sql, handler, splitOn: splitOn, transaction: Tx);

  public Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, T5, R>(string sql, object parameters, Func<T1, T2, T3, T4, T5, R> handler, string splitOn) =>
    Conn.QueryAsync<T1, T2, T3, T4, T5, R>(sql, handler, ForDapper(parameters), splitOn: splitOn, transaction: Tx);

  public Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, T5, R>(string sql, Func<T1, T2, T3, T4, T5, R> handler, string splitOn) =>
    Conn.QueryAsync<T1, T2, T3, T4, T5, R>(sql, handler, splitOn: splitOn, transaction: Tx);

  public Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, T5, T6, R>(string sql, object parameters, Func<T1, T2, T3, T4, T5, T6, R> handler, string splitOn) =>
    Conn.QueryAsync<T1, T2, T3, T4, T5, T6, R>(sql, handler, ForDapper(parameters), splitOn: splitOn, transaction: Tx);

  public Task<IEnumerable<R>> SplitQueryAsync<T1, T2, T3, T4, T5, T6, R>(string sql, Func<T1, T2, T3, T4, T5, T6, R> handler, string splitOn) =>
    Conn.QueryAsync<T1, T2, T3, T4, T5, T6, R>(sql, handler, splitOn: splitOn, transaction: Tx);

  //===============================================================================================
  // STATIC HELPER METHODS
  //===============================================================================================

  public static string Name(string databaseUrl)
  {
    var builder = new MySqlConnectionStringBuilder(databaseUrl);
    return builder.Database;
  }

  //-----------------------------------------------------------------------------------------------

  public static MySqlConnection Connection(string databaseUrl)
  {
    var conn = new MySqlConnection(databaseUrl);
    conn.Open();
    conn.Execute("SET time_zone = '+00:00';");
    return conn;
  }

  public static MySqlConnection SystemConnection(string databaseUrl)
  {
    var builder = new MySqlConnectionStringBuilder(databaseUrl);
    builder.Database = null;
    return Connection(builder.ToString());
  }

  //-----------------------------------------------------------------------------------------------

  public static bool Exists(string databaseUrl)
  {
    var dbname = Name(databaseUrl);
    using (var conn = SystemConnection(databaseUrl))
    {
      var sql = "SELECT count(*) FROM information_schema.schemata WHERE schema_name = @dbname";
      using (var command = new MySqlCommand(sql, conn))
      {
        command.Parameters.AddWithValue("@dbname", dbname);
        var result = command.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
      }
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static bool Drop(string databaseUrl)
  {
    if (Exists(databaseUrl))
    {
      using (var conn = SystemConnection(databaseUrl))
      {
        var sql = $"DROP DATABASE {Name(databaseUrl)}";
        using (var command = new MySqlCommand(sql, conn))
        {
          command.ExecuteNonQuery();
        }
      }
      return true;
    }
    return false;
  }

  //-----------------------------------------------------------------------------------------------

  public static bool Create(string databaseUrl)
  {
    if (!Exists(databaseUrl))
    {
      using (var conn = SystemConnection(databaseUrl))
      {
        var sql = $"CREATE DATABASE {Name(databaseUrl)}";
        using (var command = new MySqlCommand(sql, conn))
        {
          command.ExecuteNonQuery();
        }
      }
      return true;
    }
    return false;
  }

  //-----------------------------------------------------------------------------------------------

  public static void Migrate(string databaseUrl, bool logging)
  {
    if (Exists(databaseUrl))
    {
      var migrations = typeof(Migration).Assembly;
      using var serviceProvider = new ServiceCollection()
        .AddFluentMigratorCore()
        .AddScoped(typeof(IVersionTableMetaData), typeof(VersionTable))
        .ConfigureRunner(rb =>
        {
          rb.AddMySql8()
          .WithGlobalConnectionString(databaseUrl)
          .ScanIn(migrations).For.Migrations();
        })
        .AddLogging(lb =>
        {
          if (logging)
          {
            lb.AddFluentMigratorConsole();
          }
        })
        .BuildServiceProvider(false);

      using (var scope = serviceProvider.CreateScope())
      {
        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
      }
    }
    else
    {
      throw new Exception($"database {Name(databaseUrl)} does not exist");
    }
  }

  private class VersionTable : IVersionTableMetaData
  {
    public string SchemaName { get; set; } = string.Empty;
    public string TableName => "migrations";
    public string ColumnName => "version";
    public string UniqueIndexName => "migrations_version";
    public string AppliedOnColumnName => "applied_on";
    public string DescriptionColumnName => "description";
    public bool CreateWithPrimaryKey => false;
    public bool OwnsSchema => true;
  }

  //-----------------------------------------------------------------------------------------------

  public class NodaTimeInstantHandler : Dapper.SqlMapper.TypeHandler<Instant>
  {
    public override Instant Parse(object value)
    {
      if (value is DateTime dt)
      {
        RuntimeAssert.True(dt.Kind == DateTimeKind.Unspecified, $"expected dt.Kind == DateTimeKind.Unspecified, but was {dt.Kind}");
        dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return Instant.FromDateTimeUtc(dt);
      }
      throw new DataException($"Cannot convert {value.GetType()} {value} to NodaTime.Instant");
    }

    public override void SetValue(IDbDataParameter parameter, Instant value)
    {
      parameter.Value = value.ToDateTimeUtc();
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static void RegisterTypeHandlers()
  {
    SqlMapper.AddTypeHandler(new Database.NodaTimeInstantHandler());
  }

  //-----------------------------------------------------------------------------------------------

  public static Dictionary<string, object?>? ForDapper(object? o)
  {
    if (o is null)
      return null;

    // IMPORTANT: dapper can't automatically serialize enum to string - see (https://github.com/DapperLib/Dapper/issues/813) and (https://github.com/DapperLib/Dapper/issues/259)
    // so we intercept all objects and wrap them with serialized enums
    // ALSO we need to drop any DatabaseIgnore properties so dapper doesn't try to bind them in queries

    var properties = o.GetType()
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanRead && p.GetCustomAttribute<DatabaseIgnore>() is null)
      .ToArray();

    return properties
        .Select(c => new { Key = c.Name, Value = c.GetValue(o), Type = c.PropertyType })
        .ToDictionary(
            c => c.Key,
            c => (c.Type.IsEnum || Nullable.GetUnderlyingType(c.Type)
                ?.IsEnum == true) ? c.Value?.ToString()?.ToLower() : c.Value);
  }

  //-----------------------------------------------------------------------------------------------
}