# TODO

## NEXT:

  * INCREMENTAL DEPLOYS
    - DEPLOY AND TEST
    - REFACTOR FULL DEPLOY be more integrated with CAS system
    - CD - linux build
    - CD - mac build
    - OPS - custom upload domain (VERIFY ALB LIMITS)
    - DOWNLOADS - add share CLI to downloads page
    - ACTIONS - new version of Share/On/Web action
    - HELP - new instructions - remove bash script instructions
    - MIGRATE INTERNAL TOOLS
    - MIGRATE SCRAMBLE HEART CITY
    - MIGRATE HAT TEAM
    - REMOVE LEGACY FULL DEPLOY OPTIONS
    - MAYBE
      * separate file systems - CONTENT (s3) vs SHARE (efs)
      * store manifest in DB
      * does CLI incrementalUpload need retry logic?


## DEVOPS:

  * WORKERS:
    - PERSTENT (DB) JOB STORE
      - CLUSTERED CONFIG

## QOL:
  * QA: Unit Test MinionContext and MinionJob
  * QA: more robust deploy upload (domain) test for ActivateDeploy
    - branch no longer exists
    - deploy has been superceeded
    - deploy has been replaced (or not for first run)
  * QOL: do I really need all the [DatabaseIgnore] attributes?
  * QOL: extract Fake.Build (from Factory.Build) so they can be built independently of DB (for easier unit tests without full domain test case)
  * QA: need unit test ServeGame.CheckPassword
  * UX: syntax highlighted code blocks (e.g. on help page)
  * QOL: cleanup the insanity I had to add to make (and test) a friendly UrlGenerator
  * QOL: remove WebIntegrationTest.BuildForm and make WebIntegrationTest.Post do that work
  * QOL: break domain modules up and separate model (e.g. Deploy) and action (e.g. CreateDeploy)
  * QOL: combine "new WebIntegrationTest" with "test.Login" and/or "test.Authenticate" (e.g. do authn in WebIntegrationTest constructor)
  * QOL: Generate/Delete user access tokens
    - rewrite Profile/AccessTokens as a component?
    - rewrite Profile Page as a controller instead of a razor page?
  * QOL: tidy up flash message messiness (provide a tag helper)
  * QOL: provide some violatedUniqueKey() helper messages to reduce fragile string parsing in domain catch statements
  * QOL: need to find a way to test tag helpers and provide more help to implement them
  * QOL: cleanup and unit test With*** and Get*** association methods
  * QOL: unify the 3 crypto dependencies Crypto.Encryptor, Crypto.PasswordHasher, and Crypto.JWTGenerator
  * QOL: rewrite the FrameOptions middleware as a filter that can be applied to Serve controller without overhead to all the other endpoints
  * QA: how can I unit test my client side lib typescript helpers
  * QOL: make all db access via the async methods (and remove the sync methods)

## FUTURE
  * DEVOPS - maintenance page
  * UX - need better htmx progress indicators and disabled buttons (e.g. sysadmin refresh button)
  * REDACT mailer params logging in production (but not in dev because you need to see the tokens)
  * Integrate tailwind/esbuild watchers into dotnet watch using <Target> and <Exec>
  * Implement custom IDataProtector using our EncryptKey for the authn, csrf, and flash cookie data protector
  * Logging for SQL queries
  * Load Fixtures From YAML files
  * Move Migrations to Task Project

## WHY MYSQL/PLANETSCALE SUCKS AND WE SHOULD SWITCH TO POSTGRES

  * MIGRATIONS ARE FRAGILE
      * Mysql DDL is not transactional
      * Planetscale doesn't support RENAME (table,column) migrations
      * Planetscale can't mix DDL and Data in a migration (ugh)
  * Planetscale doesn't support CTE
  * Postgres is SO MUCH BETTER
      * arrays
      * hstore
      * ranges
      * uuid
      * postgis
      * timezones
      * jsonb
      * full text search
      * partial, GIN, GIST indexes
      * MVCC
      * deferred constraints - can't model NON-NULLABLE branch.active_deploy_id
      * CLI is sooooo much easier to browse things like constraints and foreign keys

## RESEARCH TODO

  * RESEARCH [Redis Insight](https://redis.io/insight/) - free redis gui might be embeddable on the sysadmin page?
  * RESEARCH [CSharpRepl](https://github.com/waf/CSharpRepl) to see if we can have a REPL for easier dev/debugging
  * RESEARCH [MarkDig](https://github.com/xoofx/markdig) as a markdown parser
  * RESEARCH [MassTransit](https://masstransit.io/)
  * RESEARCH [WebOptimizer](https://github.com/ligershark/WebOptimizer)
