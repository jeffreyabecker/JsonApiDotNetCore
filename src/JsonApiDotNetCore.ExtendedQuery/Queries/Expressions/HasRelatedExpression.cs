using System.Text;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class HasRelatedExpression : ExtendedQueryExpression
{
    public HasRelatedExpression(ResourceFieldChainExpression lhs, ResourceFieldChainExpression Rhs)
    {
        Left = lhs;
        Right = Rhs;
        
    }

    public ResourceFieldChainExpression Left { get; }
    public ResourceFieldChainExpression Right { get; }

    public override void Serialize(StringBuilder sb)
    {
        throw new NotImplementedException("The syntax for this expression isnt nailed down yet");
    }
}
