using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class HasRelatedExpression : ExtendedQueryExpression
{
    public HasRelatedExpression(ResourceFieldChainExpression lhs, ResourceFieldChainExpression Rhs)
    {
        Lhs = lhs;
        this.Rhs = Rhs;
        
    }

    public ResourceFieldChainExpression Lhs { get; }
    public ResourceFieldChainExpression Rhs { get; }


    public override string ToFullString()
    {
        throw new NotImplementedException();
    }
}
