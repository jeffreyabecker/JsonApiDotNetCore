using JsonApiDotNetCore.Resources;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class TableSourceNode : SqlTreeNode
{
    public const string IdColumnName = nameof(Identifiable<object>.Id);

    public abstract IReadOnlyList<ColumnNode> Columns { get; }
    public string? Alias { get; }

    protected TableSourceNode(string? alias)
    {
        Alias = alias;
    }

    public abstract TableSourceNode Clone(string? alias);

    public ColumnNode GetIdColumn(string? innerTableAlias)
    {
        return GetColumn(IdColumnName, ColumnType.Scalar, innerTableAlias);
    }

    public ColumnNode GetColumn(string persistedColumnName, ColumnType? type, string? innerTableAlias)
    {
        ColumnNode? column = FindColumn(persistedColumnName, type, innerTableAlias);

        if (column == null)
        {
            throw new InvalidOperationException($"Column '{persistedColumnName}' not found.");
        }

        return column;
    }

    public abstract ColumnNode? FindColumn(string persistedColumnName, ColumnType? type, string? innerTableAlias);
}
