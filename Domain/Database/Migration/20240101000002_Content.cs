namespace Void.Platform.Domain;

[Migration(20240101000002)]
public class ContentMigration : Migration
{
  public override void Up()
  {
    //=============================================================================================
    // CONTENT OBJECTS
    //=============================================================================================

    Create.Table("content_objects")
      .WithIdPrimaryKey()
      .WithColumn("path").AsUTF8String().NotNullable()
      .WithColumn("blake3").AsUTF8String().NotNullable()
      .WithColumn("md5").AsUTF8String().NotNullable()
      .WithColumn("sha256").AsUTF8String().NotNullable()
      .WithColumn("content_length").AsInt64().NotNullable()
      .WithColumn("content_type").AsUTF8String().NotNullable()
      .WithTimestamps();

    Create.UniqueConstraint("blake3_index")
      .OnTable("content_objects")
      .Column("blake3");

    Create.UniqueConstraint("md5_index")
      .OnTable("content_objects")
      .Column("md5");

    Create.UniqueConstraint("sha256_index")
      .OnTable("content_objects")
      .Column("sha256");
  }
}