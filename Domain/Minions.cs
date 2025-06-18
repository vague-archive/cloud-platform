namespace Void.Platform.Domain;

using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System.Reflection;

//=================================================================================================
// ABSTRACT INTERFACES - we want to hide/encapsulate Quartz as an implementation detail
//=================================================================================================

public interface IMinions
{
  void Enqueue<M, D>(D data, MinionOptions? opts = null) where M : IMinion<D> where D : class;
}

public interface IMinion<D> where D : class
{
  Task Execute(D data, IMinionContext ctx);
}

public interface IMinionContext
{
  public int RetryCount { get; }
  public int RetryLimit { get; }
  public int RetryDelay { get; }
}

public record MinionOptions
{
  public string? Group { get; init; }
  public string? Identity { get; init; }
  public int? RetryLimit { get; init; }
  public int? RetryDelay { get; init; }
}

//=================================================================================================
// Our MinionJob wrapper provides common behavior...
//   * runs the minion in a try/catch block, reporting to Sentry if an exception occurs
//   * logs the start and end (and failure) of every minion
//   * provides (optional) retry logic
//   * ... and more in future
//=================================================================================================

class MinionJob : IJob
{
  //-----------------------------------------------------------------------------------------------

  public const string MinionName = "MinionName";
  public const string MinionData = "MinionData";
  public const string RetryLimit = "MinionRetryLimit";
  public const string RetryCount = "MinionRetryCount";
  public const string RetryDelay = "MinionRetryDelay";

  //-----------------------------------------------------------------------------------------------

  private IClock Clock { get; init; }
  private ILogger Logger { get; init; }
  private IServiceProvider Services { get; init; }

  public MinionJob(IClock clock, ILogger logger, IServiceProvider services)
  {
    Clock = clock;
    Logger = logger;
    Services = services;
  }

  //-----------------------------------------------------------------------------------------------

  public async Task Execute(IJobExecutionContext qtx)
  {
    var start = Clock.Now;
    var ctx = new MinionContext(qtx);
    var key = ctx.Key;
    try
    {
      var minionType = ctx.GetMinionType();
      var dataType = ctx.GetDataType();

      var minion = RuntimeAssert.Present(ActivatorUtilities.CreateInstance(Services, minionType));
      var data = RuntimeAssert.Present(ActivatorUtilities.CreateInstance(Services, dataType));

      foreach (var prop in dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
      {
        if (!prop.CanWrite || !ctx.Contains(prop.Name))
          continue;
        prop.SetValue(data, ctx.Get(prop.Name));
      }

      var minionInterface = minionType.GetInterfaces()
        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMinion<>));
      RuntimeAssert.Present(minionInterface);

      var executeMethod = minionInterface.GetMethod("Execute");
      RuntimeAssert.Present(executeMethod);

      var dType = minionInterface.GetGenericArguments()[0];
      RuntimeAssert.True(dType.IsInstanceOfType(data));

      Logger.Information("[WORKER] starting job {key}", key);
      var result = executeMethod.Invoke(minion, new[] { data, ctx });
      var task = RuntimeAssert.Present(result as Task);
      await task;
      Logger.Information("[WORKER] completed job {key} in {duration}", key, Format.Duration(Clock.Now - start));
    }
    catch (Exception ex)
    {
      var limit = ctx.RetryLimit;
      var count = ctx.RetryCount;
      var delay = ctx.RetryDelay;
      if (count < limit)
      {
        count = count + 1;
        Logger.Warning("[WORKER] soft failed job {key} in {duration} - {message}", key, Format.Duration(Clock.Now - start), ex.Message);
        Logger.Warning("[WORKER] wait to retry ({count}/{limit}) job {key} in {delay} seconds", count, limit, key, delay);

        var retryTrigger = TriggerBuilder.Create()
            .ForJob(key)
            .WithIdentity($"{key.Name}:retry:{count}", key.Group)
            .UsingJobData(RetryCount, count)
            .UsingJobData(RetryDelay, delay * 2)
            .StartAt(DateBuilder.FutureDate(delay, IntervalUnit.Second))
            .Build();

        await qtx.Scheduler.ScheduleJob(retryTrigger);
      }
      else
      {
        Logger.Report(ex, "[WORKER] hard failed job {key} in {duration} - {message}", key, Format.Duration(Clock.Now - start), ex.Message);
      }
    }
  }

  //-----------------------------------------------------------------------------------------------
}

//=================================================================================================
// The MinionContext hides the IJobExecutionContext to keep Quartz as an implementation detail
//=================================================================================================

public class MinionContext : IMinionContext
{
  private IJobExecutionContext qtx;

  public JobKey Key { get; init; }
  public int RetryCount { get; init; }
  public int RetryLimit { get; init; }
  public int RetryDelay { get; init; }

  public MinionContext(IJobExecutionContext qtx)
  {
    this.qtx = qtx;
    Key = qtx.JobDetail.Key;
    RetryCount = qtx.MergedJobDataMap.TryGetInt(MinionJob.RetryCount, out var _count) ? _count : 0;
    RetryLimit = qtx.MergedJobDataMap.TryGetInt(MinionJob.RetryLimit, out var _limit) ? _limit : 0;
    RetryDelay = qtx.MergedJobDataMap.TryGetInt(MinionJob.RetryDelay, out var _delay) ? _delay : 0;
  }

  public bool Contains(string key) =>
    qtx.MergedJobDataMap.ContainsKey(key);

  public object Get(string key) =>
    qtx.MergedJobDataMap.Get(key);

  public Type GetMinionType() =>
    GetType(MinionJob.MinionName);

  public Type GetDataType() =>
    GetType(MinionJob.MinionData);

  private Type GetType(string key)
  {
    var typeName = RuntimeAssert.Present(qtx.MergedJobDataMap.GetString(key));
    var type = RuntimeAssert.Present(Type.GetType(typeName));
    return type;
  }
}

//=================================================================================================
// Our Minions class hides the Quartz.IScheduler and provides helper methods for enqueing ad-hoc minions
//=================================================================================================

class Minions : IMinions
{
  private IClock Clock { get; init; }
  private ILogger Logger { get; init; }
  private IRandom Random { get; init; }
  private IScheduler Scheduler { get; init; }

  public Minions(IClock clock, ILogger logger, IRandom random, IScheduler scheduler)
  {
    Clock = clock;
    Logger = logger;
    Random = random;
    Scheduler = scheduler;
  }

  public void Enqueue<M, D>(D data, MinionOptions? opts = null) where M : IMinion<D> where D : class
  {
    var identity = opts?.Identity ?? Random.Identifier();
    var group = opts?.Group ?? "minion";

    var job = JobBuilder.Create<MinionJob>()
      .WithIdentity(identity, group)
      .WithMinionType<M, D>()
      .WithMinionData(data)
      .WithMinionRetries(opts)
      .StoreDurably(false)
      .Build();

    var trigger = TriggerBuilder.Create()
      .WithIdentity(identity)
      .StartNow()
      .Build();

    Scheduler.ScheduleJob(job, trigger);
  }
}

//=================================================================================================
// FINALLY, static extension methods for setting up our DI service(s)
//=================================================================================================

public static class MinionExtensions
{
  public static IServiceCollection AddVoidMinions(this IServiceCollection services)
  {
    services.AddQuartz();
    services.AddQuartzHostedService(options =>
    {
      options.WaitForJobsToComplete = true; // graceful shutdown
    });
    services.AddSingleton<IScheduler>(sp =>
    {
      var factory = sp.GetRequiredService<ISchedulerFactory>();
      return factory.GetScheduler().GetAwaiter().GetResult();
    });
    services.AddSingleton<IMinions, Minions>();
    return services;
  }

  public static IServiceCollection RemoveVoidWorkers(this IServiceCollection services)
  {
    return services
      .RemoveService<IMinions>()
      .RemoveService<IScheduler>()
      .RemoveService<ISchedulerFactory>();
  }

  public static JobBuilder WithMinionType<M, D>(this JobBuilder builder) where M : IMinion<D> where D : class
  {
    builder.UsingJobData(MinionJob.MinionName, typeof(M).AssemblyQualifiedName);
    builder.UsingJobData(MinionJob.MinionData, typeof(D).AssemblyQualifiedName);
    return builder;
  }

  public static JobBuilder WithMinionRetries(this JobBuilder builder, MinionOptions? opts = null)
  {
    var retry = opts?.RetryLimit ?? 0;
    var delay = opts?.RetryDelay ?? 1000;
    if (retry > 0)
    {
      builder.UsingJobData(MinionJob.RetryLimit, retry);
      builder.UsingJobData(MinionJob.RetryDelay, delay);
    }
    return builder;
  }

  public static JobBuilder WithMinionData<D>(this JobBuilder builder, D data) where D : class
  {
    foreach (var prop in typeof(D).GetProperties(BindingFlags.Instance | BindingFlags.Public))
    {
      var value = prop.GetValue(data);
      if (value is not null)
      {
        switch (value)
        {
          case string str:
            builder.UsingJobData(prop.Name, str);
            break;
          case int n:
            builder.UsingJobData(prop.Name, n);
            break;
          case long n:
            builder.UsingJobData(prop.Name, n);
            break;
          case bool b:
            builder.UsingJobData(prop.Name, b);
            break;
          default:
            throw new Exception($"unsupported minion data type {value.GetType()} - only use simple types like string, int, bool, etc");
        }
      }
    }
    return builder;
  }
}

//=================================================================================================