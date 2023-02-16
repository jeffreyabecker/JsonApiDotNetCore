using DapperExample.TranslationToSql.TreeNodes;

namespace DapperExample.TranslationToSql;

internal sealed class ParameterGenerator
{
    private int _lastIndex;

    public ParameterNode Create(object? value)
    {
        string name = GetNextName();
        var parameter = new ParameterNode(name, value);

        return parameter;
    }

    private string GetNextName()
    {
        return $"@p{++_lastIndex}";
    }

    public void Reset()
    {
        _lastIndex = 0;
    }
}
