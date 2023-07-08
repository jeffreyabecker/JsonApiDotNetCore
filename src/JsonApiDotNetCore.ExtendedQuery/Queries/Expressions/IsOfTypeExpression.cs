
using System.Text;
using JsonApiDotNetCore.Configuration;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class IsOfTypeExpression : ExtendedQueryExpression
{
    public IsOfTypeExpression(IdentifierExpression resourceFieldChain, ResourceType resourceType, bool notOfType)
    {
        ResourceFieldChain = resourceFieldChain;
        ResourceType = resourceType;
        NotOfType = notOfType;
    }

    public IdentifierExpression ResourceFieldChain { get; }
    public ResourceType ResourceType { get; }
    public bool NotOfType { get; }

    public override void Serialize(StringBuilder sb)
    {
        ResourceFieldChain.Serialize(sb);
        sb.Append((NotOfType ? "is not" : "is"));
        sb.Append(" of type ");
        sb.Append(ResourceType.PublicName);
    }

}
