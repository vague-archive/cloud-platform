# Void Cloud Platform

This repository contains the web platform for Void game development cloud services - starting with...

  * **Accounts** - basic void account management, login, invites, etc.
  * **Share** - share Fiasco (web) games for playtesting.

## Production

The production version of this cloud application is currently hosted at

  * [https://play.void.dev](https://play.void.dev)

For additional production information...
  * We are hosted in [AWS](Ops/README.md)
    where you can find our [AWS Dashboard](https://us-west-2.console.aws.amazon.com/cloudwatch/home?region=us-west-2#dashboards/dashboard/platform?start=PT12H&end=null)
    as well as an [AWS Cost Report](https://us-east-1.console.aws.amazon.com/costmanagement/home#/cost-explorer?chartStyle=STACK&costAggregate=unBlendedCost&endDate=2025-04-23&excludeForecasting=false&filter=%5B%7B%22dimension%22:%7B%22id%22:%22TagKey%22,%22displayValue%22:%22Tag%22%7D,%22operator%22:%22INCLUDES%22,%22values%22:%5B%7B%22value%22:%22share%22,%22displayValue%22:%22share%22%7D%5D,%22growableValue%22:%7B%22value%22:%22pod%22,%22displayValue%22:%22pod%22%7D%7D%5D&futureRelativeRange=CUSTOM&granularity=Daily&groupBy=%5B%22Service%22%5D&historicalRelativeRange=MONTH_TO_DATE&isDefault=false&region=us-west-2&reportArn=arn:aws:ce::339712894694:ce-saved-report%2F90491612-c438-4fcc-bf2b-e047ac9c5e62&reportId=90491612-c438-4fcc-bf2b-e047ac9c5e62&reportName=Share%20Pod%20Daily%20Costs&showOnlyUncategorized=false&showOnlyUntagged=false&startDate=2025-04-01&usageAggregate=undefined&useNormalizedUnits=false)
  * The database is managed by [PlanetScale](https://app.planetscale.com/voiddotdev/void-cloud)
  * Email is sent by [Postmark](https://account.postmarkapp.com/servers/13931279/streams/outbound/overview)
  * Errors are tracked in [Sentry.io](https://void-industries.sentry.io/projects/platform/?project=4508977545478145)
  * Uptime is also monitored by [Sentry.io](https://void-industries.sentry.io/alerts/rules/uptime/platform/181314/details/?project=4508977545478145&statsPeriod=7d)
  * Secrets can be found in [AWS Secrets Manager](https://us-west-2.console.aws.amazon.com/secretsmanager/listsecrets?region=us-west-2) (with a copy maintained in 1Password)

## Quick Start

It is assumed:

  * You are running a Linux or Mac development environment
  * You have [DotNet Core SDK](https://learn.microsoft.com/en-us/dotnet/core/install/) (8.0.15) installed
  * You have [Bun](https://bun.sh/) (1.2.8) - for our build and task runner
  * You have [Redis](https://redis.io/docs/latest/operate/oss_and_stack/install/install-redis/) (7.x) installed (OPTIONAL)
  * You have [Mysql](https://dev.mysql.com/doc/refman/8.0/en/installing.html) (8.2) installed
    and can login as `platform@localhost` with password `platform` with ALL privileges (see below)
  * You have an `NPM_TOKEN` environment variable to enable `bun install` to use our `bunfig.toml`

You can install mysql and redis directly on your development environment, or if you prefer you can run
them via via [Docker: mysql](https://hub.docker.com/_/mysql) [Docker: redis](https://hub.docker.com/_/redis).

With these system dependencies in place you can get started with the following development tasks:

```bash
> bun install     # install all dependencies (both bun and dotnet)
> bun db:reset    # reset your development (and test) database
> bun dev         # run dev server - available on http://localhost:3000
> bun test:all    # run ALL unit tests
> bun test:watch  # run ALL unit tests (in watch mode)
> bun test:domain # run ONLY DOMAIN tests
> bun test:lib    # run ONLY LIB tests
> bun test:web    # run ONLY WEB tests
> bun cover:cli   # run code coverage and report to console
> bun cover:web   # run code coverage and generate (and open) an HTML report
```

> See [package.json](./package.json) for all available tasks

## MySQL

To create the development mysql `platform` user:

```sql
> mysql
mysql> CREATE USER 'platform'@'localhost' IDENTIFIED BY 'platform';
mysql> GRANT ALL PRIVILEGES ON *.* TO 'platform'@'localhost' WITH GRANT OPTION;
mysql> FLUSH PRIVILEGES;
```

## Repository Structure

There are 6 projects in this solution:

  * **Domain** - domain business logic
  * **Fixture** - test fixture factory and fake data ganerator
  * **Lib** - general purpose library methods
  * **Task** - cli tasks
  * **Test** - unit tests
  * **Web** - web layer (including both SSR Pages and API)

## RELATED READING

Some reading I found useful while getting up to speed with the modern dotnet ecosystem...

### Books
  * [C# 12 in a Nutshell](https://www.oreilly.com/library/view/c-12-in/9781098147433/)
  * [ASP.NET Core In Action](https://www.manning.com/books/asp-net-core-in-action-third-edition)
  * [Pro ASP.NET Core](https://www.manning.com/books/pro-aspdotnet-core-7-tenth-edition)
  * [EF Core in Action](https://www.manning.com/books/entity-framework-core-in-action-second-edition)

### Official Documentation
  * [C#](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/)
  * [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core?view=aspnetcore-8.0)
  * [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

### Dependencies
  * [Dapper](https://www.learndapper.com/) - lightweight database ORM (alternative/complement to EF Core)
  * [FluentValidation](https://docs.fluentvalidation.net/en/latest/) - domain validation library
  * [Noda Time](https://nodatime.org/) - date and time library
  * [Serilog](https://serilog.net/) - logging library
  * [Quartz](https://www.quartz-scheduler.net/) - a background worker job queue
  * [FusionCache](https://github.com/ZiggyCreatures/FusionCache) - an L1 (memory) + L2 (redis) hybrid cache
  * [MySqlConnector](https://mysqlconnector.net/) - mysql client
  * [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) - redis client
  * [Spectre Console Cli](https://spectreconsole.net/) - console cli task library
  * [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) - Swagger API documentation

### Dev/Test Dependencies
  * [xUnit](https://xunit.net/) - testing library
  * [NSubstitute](https://nsubstitute.github.io/) - mocking library
  * [Bogus](https://github.com/bchavez/Bogus) - fake test data generator
  * [DotNetEnv](https://github.com/tonerdo/dotnet-env) - load (.gitignored) .env file into configuration
  * [AngleSharp](https://github.com/AngleSharp/AngleSharp) - parse HTML responses for integration tests (see Assert.Html.Document)
  * [MockHTTP](https://github.com/richardszalay/mockhttp) - mock HTTP client for testing
