using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding.Strategies;
public class IdentifierStrategy : IWhereClauseBuilderStrategy<IdentifierExpression>
{
    public Expression Visit(IVisitExtendedQueryExpressions visitor, IdentifierExpression identifierExpression, QueryClauseBuilderContext context)
    {
        MemberExpression? property = null;

        foreach (ResourceFieldAttribute field in identifierExpression.Fields)
        {
            Expression parentAccessor = property ?? context.LambdaScope.Accessor;
            Type propertyType = field.Property.DeclaringType!;
            string propertyName = field.Property.Name;

            bool requiresUpCast = parentAccessor.Type != propertyType && parentAccessor.Type.IsAssignableFrom(propertyType);
            Type parentType = requiresUpCast ? propertyType : parentAccessor.Type;

            if (parentType.GetProperty(propertyName) == null)
            {
                throw new InvalidOperationException($"Type '{parentType.Name}' does not contain a property named '{propertyName}'.");
            }

            property = requiresUpCast
                ? Expression.MakeMemberAccess(Expression.Convert(parentAccessor, propertyType), field.Property)
                : Expression.Property(parentAccessor, propertyName);
        }

        return property!;
    }
}
