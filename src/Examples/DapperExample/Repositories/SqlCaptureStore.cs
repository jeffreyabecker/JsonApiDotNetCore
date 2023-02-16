using DapperExample.TranslationToSql;

namespace DapperExample.Repositories;

/// <summary>
/// Captures the emitted SQL statements, which enables integration tests to assert on them.
/// </summary>
public sealed class SqlCaptureStore
{
    private readonly List<SqlCommand> _sqlCommands = new();

    public IReadOnlyList<SqlCommand> SqlCommands => _sqlCommands;

    public void Clear()
    {
        _sqlCommands.Clear();
    }

    internal void Add(string statement, IDictionary<string, object?> parameters)
    {
        var sqlCommand = new SqlCommand(statement, parameters);
        _sqlCommands.Add(sqlCommand);
    }
}
