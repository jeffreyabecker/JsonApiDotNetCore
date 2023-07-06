using JetBrains.Annotations;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.Queries;

/// <summary>
/// Represents an expression coming from query string. The scope determines at which depth in the <see cref="IResourceGraph" /> to apply its expression.
/// </summary>
[PublicAPI]
public class ExpressionInScope<TScope,TExpression>
    where TExpression: class
{
    public virtual TScope? Scope { get; }
    public virtual TExpression Expression { get; }

    public ExpressionInScope(TScope? scope, TExpression expression)
    {
        ArgumentGuard.NotNull(expression);

        Scope = scope;
        Expression = expression;
    }

    public override string ToString()
    {
        return $"{Scope} => {Expression}";
    }
}



/// <summary>
/// Represents an expression coming from query string. The scope determines at which depth in the <see cref="IResourceGraph" /> to apply its expression.
/// </summary>
[PublicAPI]
public class ExpressionInScope : ExpressionInScope<ResourceFieldChainExpression, QueryExpression>
{
    public ExpressionInScope(ResourceFieldChainExpression? scope, QueryExpression expression) : base(scope, expression)
    {
    }
}
