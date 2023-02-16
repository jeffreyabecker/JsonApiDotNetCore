using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql;

public sealed class SqlCommand
{
    public string Statement { get; }
    public IDictionary<string, object?> Parameters { get; }

    public SqlCommand(string statement, IDictionary<string, object?> parameters)
    {
        ArgumentGuard.NotNull(statement);
        ArgumentGuard.NotNull(parameters);

        Statement = statement;
        Parameters = parameters;
    }
}
