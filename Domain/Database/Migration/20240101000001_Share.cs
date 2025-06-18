namespace Void.Platform.Domain;

[Migration(20240101000001)]
public class ShareMigration : Migration
{
  public override void Up()
  {
    //=============================================================================================
    // BRANCHES
    //=============================================================================================

    Create.Table("branches")
      .WithIdPrimaryKey()
      .WithColumn("organization_id").AsId().NotNullable()
      .WithColumn("game_id").AsId().NotNullable()
      .WithColumn("slug").AsUTF8String().NotNullable()
      .WithColumn("password").AsUTF8String().Nullable()
      .WithColumn("pinned").AsBoolean().NotNullable().WithDefaultValue(false)
      .WithColumn("active_deploy_id").AsId().Nullable().WithDefaultValue(null)
      .WithColumn("latest_deploy_id").AsId().Nullable().WithDefaultValue(null)
      .WithTimestamps();

    Create.ForeignKey("branches_organization_id_fkey")
      .FromTable("branches").ForeignColumn("organization_id")
      .ToTable("organizations").PrimaryColumn("id")
      .OnDelete(Rule.Cascade);

    Create.ForeignKey("branches_game_id_fkey")
      .FromTable("branches").ForeignColumn("game_id")
      .ToTable("games").PrimaryColumn("id")
      .OnDelete(Rule.Cascade);

    Create.UniqueConstraint("branches_game_slug_index")
      .OnTable("branches")
      .Columns("game_id", "slug");

    //=============================================================================================
    // DEPLOYS
    //=============================================================================================

    Create.Table("deploys")
      .WithIdPrimaryKey()
      .WithColumn("organization_id").AsId().NotNullable()
      .WithColumn("game_id").AsId().NotNullable()
      .WithColumn("branch_id").AsId().NotNullable()
      .WithColumn("path").AsUTF8String().NotNullable()
      .WithColumn("state").AsEnum(["deploying", "ready", "failed"]).NotNullable()
      .WithColumn("number").AsInt64().NotNullable().WithDefaultValue(1)
      .WithColumn("error").AsText().Nullable()
      .WithColumn("deploying_on").AsTimestamp().Nullable()
      .WithColumn("deployed_by").AsId().NotNullable()
      .WithColumn("deployed_on").AsTimestamp().NotNullable()
      .WithColumn("failed_on").AsTimestamp().Nullable()
      .WithColumn("deleted_on").AsTimestamp().Nullable()
      .WithColumn("deleted_reason").AsUTF8String().Nullable()
      .WithTimestamps();

    Create.ForeignKey("deploys_organization_id_fkey")
      .FromTable("deploys").ForeignColumn("organization_id")
      .ToTable("organizations").PrimaryColumn("id")
      .OnDelete(Rule.Cascade);

    Create.ForeignKey("deploys_game_id_fkey")
      .FromTable("deploys").ForeignColumn("game_id")
      .ToTable("games").PrimaryColumn("id")
      .OnDelete(Rule.Cascade);

    Create.ForeignKey("deploys_deployed_by_fkey")
      .FromTable("deploys").ForeignColumn("deployed_by")
      .ToTable("users").PrimaryColumn("id")
      .OnDelete(Rule.Cascade);

    //---------------------------------------------------------------------------------------------
    // BRANCHES and DEPLOYS inter-dependencies
    //---------------------------------------------------------------------------------------------

    Create.ForeignKey("deploys_branch_id_fkey")
      .FromTable("deploys").ForeignColumn("branch_id")
      .ToTable("branches").PrimaryColumn("id")
      .OnDelete(Rule.Cascade);

    Create.ForeignKey("branches_active_deploy_id_fkey")
      .FromTable("branches").ForeignColumn("active_deploy_id")
      .ToTable("deploys").PrimaryColumn("id")
      .OnDelete(Rule.None); // important, a deploy cannot be deleted if it is still referenced as the active deploy for a branch

    Create.ForeignKey("branches_latest_deploy_id_fkey")
      .FromTable("branches").ForeignColumn("latest_deploy_id")
      .ToTable("deploys").PrimaryColumn("id")
      .OnDelete(Rule.None); // important, a deploy cannot be deleted if it is still referenced as the latest deploy for a branch
  }
}