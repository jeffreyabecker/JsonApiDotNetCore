using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.FieldChains;

public record IdentifierChainMatchResult(bool IsSuccess, IReadOnlyList<ResourceFieldAttribute> FieldChain, IdentifierChainPatternMatchResult MatchResults);
