using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class EmptyExpression : ExtendedQueryExpression
{
    public override void Serialize(StringBuilder sb)
    {

    }
}
