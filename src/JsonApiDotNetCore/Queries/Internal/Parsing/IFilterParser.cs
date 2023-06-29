using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.Queries.Internal.Parsing;
public interface IFilterParser
{
    FilterExpression Parse(string source, ResourceType resourceTypeInScope);
}