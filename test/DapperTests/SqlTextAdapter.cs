using System.Text.RegularExpressions;
using DapperExample;

namespace DapperTests;

internal sealed class SqlTextAdapter
{
    private static readonly Dictionary<Regex, string> SqlServerReplacements = new()
    {
        [new Regex(@"""([^""]+)""", RegexOptions.Compiled)] = "[$+]",
        [new Regex(@"([ ]*)LIMIT (@p\d+) OFFSET (@p\d+)", RegexOptions.Compiled)] = $"$1OFFSET $3 ROWS{Environment.NewLine}$1FETCH NEXT $2 ROWS ONLY",
        [new Regex(@"([ ]*)LIMIT (@p\d+)", RegexOptions.Compiled)] = $"$1OFFSET 0 ROWS{Environment.NewLine}$1FETCH FIRST $2 ROWS ONLY",
        [new Regex($@"(VALUES \([^)]*\)){Environment.NewLine}RETURNING \[Id\]", RegexOptions.Compiled)] = $"OUTPUT INSERTED.[Id]{Environment.NewLine}$1"
    };

    private readonly DatabaseProvider _databaseProvider;

    public SqlTextAdapter(DatabaseProvider databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    public string Adapt(string text, bool isClientGeneratedId)
    {
        string replaced = text;

        if (_databaseProvider == DatabaseProvider.MySql)
        {
            replaced = replaced.Replace(@"""", "`");

            string selectInsertId = isClientGeneratedId ? $";{Environment.NewLine}SELECT @p1" : $";{Environment.NewLine}SELECT LAST_INSERT_ID()";
            replaced = replaced.Replace($"{Environment.NewLine}RETURNING `Id`", selectInsertId);
        }
        else if (_databaseProvider == DatabaseProvider.SqlServer)
        {
            foreach ((Regex regex, string replacementPattern) in SqlServerReplacements)
            {
                replaced = regex.Replace(replaced, replacementPattern);
            }
        }

        return replaced;
    }
}
