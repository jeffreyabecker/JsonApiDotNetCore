namespace DapperExample.TranslationToSql;

internal sealed class TableAliasGenerator : UniqueNameGenerator
{
    public TableAliasGenerator()
        : base("t")
    {
    }
}
