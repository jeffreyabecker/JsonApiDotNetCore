namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.FieldChains;

[Flags]
public enum IdentifierElementTypes
{
    None = 0,
    ScopeRoot = 1,
    Attribute = 2,
    ToOneRelationship = 4,
    ToManyRelationship = 8,
    Relationship = ToOneRelationship | ToManyRelationship,
    Field = Attribute | Relationship
}
