
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class IsOfTypeExpression : ExtendedQueryExpression
{
    public IsOfTypeExpression(ResourceFieldChainExpression resourceFieldChain, ResourceType resourceType, bool notOfType)
    {
        ResourceFieldChain = resourceFieldChain;
        ResourceType = resourceType;
        NotOfType = notOfType;
    }

    public ResourceFieldChainExpression ResourceFieldChain { get; }
    public ResourceType ResourceType { get; }
    public bool NotOfType { get; }


    public override string ToFullString() => $"{ResourceFieldChain.ToFullString()} {(NotOfType ? "is not" : "is")} of type {ResourceType.PublicName}";
}
