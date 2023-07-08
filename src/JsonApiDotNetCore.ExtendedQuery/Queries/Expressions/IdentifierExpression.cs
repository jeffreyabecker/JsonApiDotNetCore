using System.Collections.Immutable;
using System.Text;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class IdentifierExpression : ExtendedQueryExpression
{
    /// <summary>
    /// A list of one or more JSON:API fields. Use <see cref="FieldChainPattern.Match" /> to convert from text.
    /// </summary>
    public IImmutableList<ResourceFieldAttribute> Fields { get; }


    public IdentifierExpression(IEnumerable<ResourceFieldAttribute> fields)
    {
        ArgumentGuard.NotNullNorEmpty(fields);

        Fields = fields.ToImmutableList();
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (IdentifierExpression)obj;

        return Fields.SequenceEqual(other.Fields);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        foreach (ResourceFieldAttribute field in Fields)
        {
            hashCode.Add(field);
        }

        return hashCode.ToHashCode();
    }

    public override void Serialize(StringBuilder sb)
    {
        for(int i=0; i<Fields.Count; i++)
        {
            if(i> 0)
            {
                sb.Append('.');
            }
            sb.Append(Fields[i].PublicName);
        }
    }
}
