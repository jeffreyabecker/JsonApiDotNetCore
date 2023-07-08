using JsonApiDotNetCore.Queries.Parsing;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class ExtendedQueryParseException : QueryParseException
{
    public ExtendedQueryParseException(string message, int position, Exception innerException): base(message, position, innerException) { }
    public ExtendedQueryParseException(string message, int position) : base(message, position)
    {
    }
}
