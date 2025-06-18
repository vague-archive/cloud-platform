global using MigrationAttribute = FluentMigrator.MigrationAttribute;

namespace Void.Platform.Domain;

using RawSql = FluentMigrator.RawSql;
using FluentMigrator.Builders.Create.Table;
using FluentMigrator.Builders.Alter.Table;

//-------------------------------------------------------------------------------------------------

[ExcludeFromCodeCoverage]
public abstract class Migration : FluentMigrator.Migration
{
  public override void Down()
  {
    throw new Exception("never look back");
  }
}

//-------------------------------------------------------------------------------------------------

[ExcludeFromCodeCoverage]
public static class MigrationExtensions
{
  private static readonly string Timestamp = "timestamp(3)";
  private static readonly string TimestampDefault = "NOW(3)";

  public static ICreateTableColumnOptionOrWithColumnSyntax WithIdPrimaryKey(this ICreateTableWithColumnOrSchemaOrDescriptionSyntax creator)
  {
    return creator.WithColumn("id").AsId().PrimaryKey().Identity();
  }

  public static ICreateTableColumnOptionOrWithColumnSyntax AsId(this ICreateTableColumnAsTypeSyntax creator)
  {
    return creator.AsInt64();
  }

  public static IAlterTableColumnOptionOrAddColumnOrAlterColumnSyntax AsId(this IAlterTableColumnAsTypeSyntax builder)
  {
    return builder.AsInt64();
  }

  public static ICreateTableColumnOptionOrWithColumnSyntax AsTimestamp(this ICreateTableColumnAsTypeSyntax creator)
  {
    return creator.AsCustom(Timestamp);
  }

  public static IAlterTableColumnOptionOrAddColumnOrAlterColumnSyntax AsTimestamp(this IAlterTableColumnAsTypeSyntax builder)
  {
    return builder.AsCustom(Timestamp);
  }

  public static ICreateTableColumnOptionOrWithColumnSyntax WithTimestamp(this ICreateTableColumnOptionOrWithColumnSyntax creator, string name)
  {
    return creator
      .WithColumn(name).AsTimestamp().NotNullable().WithDefaultValue(RawSql.Insert(TimestampDefault));
  }

  public static ICreateTableColumnOptionOrWithColumnSyntax WithTimestamps(this ICreateTableColumnOptionOrWithColumnSyntax creator)
  {
    return creator
      .WithCreatedOn()
      .WithUpdatedOn();
  }

  public static ICreateTableColumnOptionOrWithColumnSyntax WithCreatedOn(this ICreateTableColumnOptionOrWithColumnSyntax creator) =>
    creator.WithTimestamp("created_on");

  public static ICreateTableColumnOptionOrWithColumnSyntax WithUpdatedOn(this ICreateTableColumnOptionOrWithColumnSyntax creator) =>
    creator.WithTimestamp("updated_on");

  public static ICreateTableColumnOptionOrWithColumnSyntax AsEnum<T>(this ICreateTableColumnAsTypeSyntax creator) where T : Enum
  {
    return creator.AsEnum(Enum.GetNames(typeof(T)).Select(name => name.ToLower()).ToArray());
  }

  public static ICreateTableColumnOptionOrWithColumnSyntax AsEnum(this ICreateTableColumnAsTypeSyntax creator, string[] values)
  {
    var definition = String.Join(", ", values.Select(v => $"'{v}'").ToArray());
    return creator.AsCustom($"ENUM({definition})").NotNullable();
  }

  public static ICreateTableColumnOptionOrWithColumnSyntax AsUTF8String(this ICreateTableColumnAsTypeSyntax creator, int size = 255, bool cs = false)
  {
    if (cs)
      return creator.AsCustom($"varchar ({size}) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs");
    else
      return creator.AsCustom($"varchar ({size})"); // DB default is utf8mb4_0900_ai_ci, so no need to be explicit
  }

  public static IAlterTableColumnOptionOrAddColumnOrAlterColumnSyntax AsUTF8String(this IAlterTableColumnAsTypeSyntax builder, int size = 255, bool cs = false)
  {
    if (cs)
      return builder.AsCustom($"varchar ({size}) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs");
    else
      return builder.AsCustom($"varchar ({size})"); // DB default is utf8mb4_0900_ai_ci, so no need to be explicit
  }

  public static ICreateTableColumnOptionOrWithColumnSyntax AsText(this ICreateTableColumnAsTypeSyntax creator)
  {
    return creator.AsCustom("TEXT");
  }
}

//-------------------------------------------------------------------------------------------------