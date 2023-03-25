using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents a reference to a column in a <see cref="SelectNode"/>. For example, "t2.Id0" in: SELECT t2.Id AS Id0 FROM (SELECT t1.Id FROM Users AS t1) AS t2
/// </summary>
internal sealed class ColumnInSelectNode : ColumnNode
{
    public ColumnSelectorNode Selector { get; }

    public bool IsVirtual => Selector.Column is ColumnInSelectNode columnInSelect ? columnInSelect.IsVirtual : Selector.Alias != null;

    public ColumnInSelectNode(ColumnSelectorNode selector, string? tableAlias)
        : base(GetColumnName(selector), selector.Column.Type, tableAlias)
    {
        Selector = selector;
    }

    private static string GetColumnName(ColumnSelectorNode selector)
    {
        ArgumentGuard.NotNull(selector);

        return selector.Identity;
    }

    public string GetPersistedColumnName()
    {
        return Selector.Column is ColumnInSelectNode columnInSelect ? columnInSelect.GetPersistedColumnName() : Selector.Column.Name;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitColumnInSelect(this, argument);
    }
}
