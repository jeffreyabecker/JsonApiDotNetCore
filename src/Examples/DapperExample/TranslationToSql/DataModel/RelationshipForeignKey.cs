using System.Text;
using DapperExample.TranslationToSql.Builders;
using Humanizer;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources.Annotations;

namespace DapperExample.TranslationToSql.DataModel;

/// <summary>
/// Defines foreign key information that is required to produce SQL queries.
/// </summary>
public sealed class RelationshipForeignKey
{
    /// <summary>
    /// The JSON:API relationship mapped to this foreign key.
    /// </summary>
    public RelationshipAttribute Relationship { get; }

    /// <summary>
    /// Indicates whether the foreign key column is defined at the left side of the JSON:API relationship.
    /// </summary>
    public bool IsAtLeftSide { get; }

    /// <summary>
    /// The foreign key column name.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Indicates whether the foreign key column is nullable.
    /// </summary>
    public bool IsNullable { get; }

    public RelationshipForeignKey(RelationshipAttribute relationship, bool isAtLeftSide, string columnName, bool isNullable)
    {
        Relationship = relationship;
        IsAtLeftSide = isAtLeftSide;
        ColumnName = columnName;
        IsNullable = isNullable;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append($"{Relationship.LeftType.ClrType.Name}.{Relationship.Property.Name} => ");

        ResourceType tableType = IsAtLeftSide ? Relationship.LeftType : Relationship.RightType;

        builder.Append(SqlQueryBuilder.FormatIdentifier(tableType.ClrType.Name.Pluralize()));
        builder.Append('.');
        builder.Append(SqlQueryBuilder.FormatIdentifier(ColumnName));

        if (IsNullable)
        {
            builder.Append('?');
        }

        return builder.ToString();
    }
}
