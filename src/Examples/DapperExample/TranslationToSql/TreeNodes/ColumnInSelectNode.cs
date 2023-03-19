using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class ColumnInSelectNode : ColumnNode
{
    public ColumnSelectorNode Selector { get; }

    public bool IsVirtual => Selector.Column is ColumnInSelectNode columnInSelect ? columnInSelect.IsVirtual : Selector.Alias != null;

    public ColumnInSelectNode(ColumnSelectorNode selector, string? tableAlias)
        : base(GetColumnName(selector), tableAlias)
    {
        Selector = selector;
    }

    private static string GetColumnName(ColumnSelectorNode selector)
    {
        ArgumentGuard.NotNull(selector);

        return selector.Identity;
    }

    public string GetUnderlyingTableColumnName()
    {
        return Selector.Column is ColumnInSelectNode columnInSelect ? columnInSelect.GetUnderlyingTableColumnName() : Selector.Column.Name;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitColumnInSelect(this, argument);
    }
}
