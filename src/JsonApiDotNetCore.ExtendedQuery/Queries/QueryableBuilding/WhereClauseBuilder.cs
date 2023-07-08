using System.Collections.Immutable;
using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Resources;

using QueryClauseBuilderContext = JsonApiDotNetCore.Queries.QueryableBuilding.QueryClauseBuilderContext;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding;
public partial class WhereClauseBuilder : JsonApiDotNetCore.Queries.Expressions.QueryExpressionVisitor<QueryClauseBuilderContext, Expression>, JsonApiDotNetCore.Queries.QueryableBuilding.IWhereClauseBuilder, IVisitExtendedQueryExpressions
{

    private IReadOnlyDictionary<Type, IWhereClauseBuilderStrategy> _strategies;
    public WhereClauseBuilder(ICollection<IWhereClauseBuilderStrategy> clauseStrategies)
    {
        _strategies = clauseStrategies.ToImmutableDictionary(x => x.ForType);
    }
    public Expression ApplyWhere(JsonApiDotNetCore.Queries.Expressions.FilterExpression filter, QueryClauseBuilderContext context)
    {
        if(filter is WrapperExpression wrapper)
        {
            return VisitExtendedQueryExpression(wrapper.InnerExpression, context);
        }
        throw new JsonApiDotNetCore.Queries.Parsing.QueryParseException("Invalid fitler expression type -- is your DI container configured correctly", 0);
    }
    public override Expression DefaultVisit(JsonApiDotNetCore.Queries.Expressions.QueryExpression expression, QueryClauseBuilderContext context)
    {
        if(expression is EmptyExpression)
        {
            return context.Source;
        }
        if(expression is ExtendedQueryExpression ext) {
            return VisitExtendedQueryExpression(ext, context);
        }
        if (expression is WrapperExpression wrapper)
        {
            return VisitExtendedQueryExpression(wrapper.InnerExpression, context);
        }
        throw new NotImplementedException($"I dont know how to interpret a {expression.GetType().Name}");
    }

    public Expression VisitExtendedQueryExpression(ExtendedQueryExpression expression, QueryClauseBuilderContext context)
    {
        Type concreteExpressionType = expression.GetType();
        if (!_strategies.ContainsKey(concreteExpressionType))
        {
            throw new NotImplementedException($"No Where clause builder for {concreteExpressionType.FullName}");
        }
        return _strategies[concreteExpressionType].Visit(this, expression, context);
    }

    public Expression VisitIsOfTypeExpression(IsOfTypeExpression isaExpr, QueryClauseBuilderContext context)
    {
        throw new NotImplementedException();
    }

    public Expression VisitHasRelatedExpression(HasRelatedExpression hasExpr, QueryClauseBuilderContext context)
    {
        throw new NotImplementedException();
    }




    public Type ResolveCommonType(ExtendedQueryExpression left, ExtendedQueryExpression right, QueryClauseBuilderContext context)
    {
        Type leftType = ResolveFixedType(left, context);

        if (RuntimeTypeConverter.CanContainNull(leftType))
        {
            return leftType;
        }

        if (right is LiteralQueryExpression.NullLiteralExpression)
        {
            return typeof(Nullable<>).MakeGenericType(leftType);
        }

        Type? rightType = TryResolveFixedType(right, context);

        if (rightType != null && RuntimeTypeConverter.CanContainNull(rightType))
        {
            return rightType;
        }

        return leftType;
    }
    public Type ResolveFixedType(ExtendedQueryExpression expression, QueryClauseBuilderContext context)
    {
        Expression result = VisitExtendedQueryExpression(expression, context);
        return result.Type;
    }
    public Type? TryResolveFixedType(ExtendedQueryExpression expression, QueryClauseBuilderContext context)
    {
        if (expression is IdentifierExpression chain)
        {
            Expression child = VisitExtendedQueryExpression(chain, context);
            return child.Type;
        }

        return null;
    }


}
