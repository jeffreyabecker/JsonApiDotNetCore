using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class JadncTreeVisitor : ParseTreeVisitorBase<QueryExpression>
{
    public JadncTreeVisitor(ResourceType resourceType)
    {
        Add(new LiteralRuleVisitor());
    }
}
