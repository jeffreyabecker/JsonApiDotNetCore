using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class ColumnNode : SqlValueNode
{
    public string Name { get; }
    public string? TableAlias { get; }

    protected ColumnNode(string name, string? tableAlias)
    {
        ArgumentGuard.NotNullNorEmpty(name);

        Name = name;
        TableAlias = tableAlias;
    }

    public int GetTableAliasIndex()
    {
        if (TableAlias == null)
        {
            return -1;
        }

        string? number = TableAlias[1..];
        return int.Parse(number);
    }
}
