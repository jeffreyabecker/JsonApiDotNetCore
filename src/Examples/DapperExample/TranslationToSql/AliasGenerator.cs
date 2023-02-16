namespace DapperExample.TranslationToSql;

internal sealed class AliasGenerator
{
    private int _lastIndex;

    public string GetNext()
    {
        return $"t{++_lastIndex}";
    }

    public void Reset()
    {
        _lastIndex = 0;
    }
}
