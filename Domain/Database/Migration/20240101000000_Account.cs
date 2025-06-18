namespace Void.Platform.Domain;

[Migration(20240101000000)]
public class AccountMigration : Migration
{
  public override void Up()
  {
    //=============================================================================================
    // ORGANIZATIONS
    //=============================================================================================

    Create.Table("organizations")
      .WithIdPrimaryKey()
      .WithColumn("name").AsUTF8String().NotNullable()
      .WithColumn("slug").AsUTF8String().NotNullable()
      .WithTimestamps();

    Create.UniqueConstraint("organizations_slug")
      .OnTable("organizations")
      .Columns("slug");

    //=============================================================================================
    // USERS
    //=============================================================================================

    Create.Table("users")
      .WithIdPrimaryKey()
      .WithColumn("name").AsUTF8String().NotNullable()
      .WithColumn("email").AsUTF8String().NotNullable()
      .WithColumn("timezone").AsUTF8String().NotNullable()
      .WithColumn("locale").AsUTF8String().NotNullable()
      .WithColumn("password").AsUTF8String().Nullable()
      .WithColumn("disabled").AsBoolean().NotNullable().WithDefaultValue(false)
      .WithTimestamps();

    Create.UniqueConstraint("email")
      .OnTable("users")
      .Columns("email");

    //=============================================================================================
    // USER ROLES
    //=============================================================================================

    Create.Table("user_roles")
      .WithColumn("user_id").AsId()
      .WithColumn("role").AsEnum<Account.UserRole>().NotNullable()
      .WithTimestamps();

    Create.PrimaryKey("user_roles_pkey")
      .OnTable("user_roles")
      .Columns("user_id", "role");

    Create.ForeignKey("user_roles_user_id_fkey")
      .FromTable("user_roles").ForeignColumn("user_id")
      .ToTable("users").PrimaryColumn("Id")
      .OnDelete(Rule.Cascade);

    //=============================================================================================
    // IDENTITIES
    //=============================================================================================

    Create.Table("identities")
      .WithIdPrimaryKey()
      .WithColumn("user_id").AsId()
      .WithColumn("provider").AsEnum<Account.IdentityProvider>().NotNullable()
      .WithColumn("identifier").AsUTF8String().NotNullable()
      .WithColumn("username").AsUTF8String().NotNullable()
      .WithTimestamps();

    Create.UniqueConstraint("identities_provider_identifier_index")
      .OnTable("identities")
      .Columns("provider", "identifier");

    Create.UniqueConstraint("identities_provider_username_index")
      .OnTable("identities")
      .Columns("provider", "username");

    //=============================================================================================
    // MEMBERS
    //=============================================================================================

    Create.Table("members")
      .WithColumn("organization_id").AsId()
      .WithColumn("user_id").AsId()
      .WithTimestamps();

    Create.PrimaryKey("members_pkey")
      .OnTable("members")
      .Columns("organization_id", "user_id");

    Create.ForeignKey("members_organization_id_fkey")
      .FromTable("members").ForeignColumn("organization_id")
      .ToTable("organizations").PrimaryColumn("Id")
      .OnDelete(Rule.Cascade);

    Create.ForeignKey("members_user_id_fkey")
      .FromTable("members").ForeignColumn("user_id")
      .ToTable("users").PrimaryColumn("Id")
      .OnDelete(Rule.Cascade);

    //=============================================================================================
    // TOKENS
    //=============================================================================================

    Create.Table("tokens")
      .WithIdPrimaryKey()
      .WithColumn("type").AsEnum<Account.TokenType>().NotNullable()
      .WithColumn("digest").AsUTF8String().NotNullable()
      .WithColumn("tail").AsUTF8String().NotNullable()  // we retain the last N digits of the token to help users identify which token is which
      .WithColumn("user_id").AsId().Nullable()          // optional - some tokens are tied to an existing user (:access)
      .WithColumn("organization_id").AsId().Nullable()  // optional - some tokens are tied to an organization (:invite)
      .WithColumn("sent_to").AsUTF8String().Nullable()  // optional - was this token actually sent to an email address
      .WithColumn("is_spent").AsBoolean().NotNullable().WithDefaultValue(false)
      .WithColumn("expires_on").AsTimestamp().Nullable()
      .WithTimestamps();

    Create.ForeignKey("tokens_user_id_fkey")
      .FromTable("tokens").ForeignColumn("user_id")
      .ToTable("users").PrimaryColumn("id")
      .OnDelete(Rule.Cascade);

    Create.ForeignKey("tokens_organization_id_fkey")
      .FromTable("tokens").ForeignColumn("organization_id")
      .ToTable("organizations").PrimaryColumn("id")
      .OnDelete(Rule.Cascade);

    Create.Index("tokens_organization_id_index")
      .OnTable("tokens")
      .OnColumn("organization_id");

    Create.Index("tokens_user_id_index")
      .OnTable("tokens")
      .OnColumn("user_id");

    Create.Index("tokens_expires_on_index")
      .OnTable("tokens")
      .OnColumn("expires_on");

    Create.Index("tokens_type_digest_index")
      .OnTable("tokens")
      .OnColumn("type").Ascending()
      .OnColumn("digest").Ascending();

    Create.UniqueConstraint("tokens_digest_index")
      .OnTable("tokens")
      .Columns("digest");

    //=============================================================================================
    // GAMES
    //=============================================================================================

    Create.Table("games")
      .WithIdPrimaryKey()
      .WithColumn("organization_id").AsId()
      .WithColumn("purpose").AsEnum<Account.GamePurpose>().NotNullable()
      .WithColumn("name").AsUTF8String()
      .WithColumn("slug").AsUTF8String()
      .WithColumn("description").AsText().Nullable()
      .WithColumn("archived").AsBoolean().NotNullable().WithDefaultValue(false)
      .WithColumn("archived_on").AsTimestamp().Nullable()
      .WithTimestamps();

    Create.ForeignKey("games_organization_id_fkey")
      .FromTable("games").ForeignColumn("organization_id")
      .ToTable("organizations").PrimaryColumn("id")
      .OnDelete(Rule.Cascade);

    Create.UniqueConstraint("games_slug_index")
      .OnTable("games")
      .Columns("organization_id", "slug");
  }
}