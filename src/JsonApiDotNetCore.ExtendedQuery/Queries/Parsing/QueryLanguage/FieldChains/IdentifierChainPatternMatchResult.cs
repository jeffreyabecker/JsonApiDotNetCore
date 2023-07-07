namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.FieldChains;

public record IdentifierChainPatternMatchResult(IdentifierChainPattern Pattern, bool IsSuccess, int FailurePosition, string FailureMessage);
