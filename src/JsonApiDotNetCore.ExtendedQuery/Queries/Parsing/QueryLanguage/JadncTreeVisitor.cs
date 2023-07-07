using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class JadncTreeVisitor : ParseTreeVisitorBase<ExtendedQueryExpression>
{
    public JadncTreeVisitor(ResourceType resourceType) : base(
        new IdentifierRuleVisitor(resourceType),
        new RootRuleVisitor(),
        new OfTypeRuleVisitor(resourceType),
        new HasRuleVisitor(resourceType),
        new InRuleVisitor(),
        new NestedRuleVisitor(),
        new OrRuleVisitor(),
        new GreaterLessRuleVisitor(),
        new FunctionRuleVisitor(),
        new NotRuleVisitor(),
        new AddRuleVisitor(),
        new IsNullRuleVisitor(),
        new LiteralRuleVisitor(),
        new MulRuleVisitor(),
        new LikeRuleVisitor(),
        new IfRuleVisitor(),
        new EqualRuleVisitor(),
        new AndRuleVisitor()
    )
    {

    }
}

