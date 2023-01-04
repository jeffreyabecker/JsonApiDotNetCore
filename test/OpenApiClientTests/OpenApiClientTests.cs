using System.Linq.Expressions;

namespace OpenApiClientTests;

public class OpenApiClientTests
{
    private const string AttributesObjectParameterName = "attributesObject";

    protected static Expression<Func<TAttributesObject, object?>> CreateIncludedAttributeSelector<TAttributesObject>(string propertyName)
        where TAttributesObject : class
    {
        Type attributesObjectType = typeof(TAttributesObject);

        ParameterExpression parameter = Expression.Parameter(attributesObjectType, AttributesObjectParameterName);
        MemberExpression property = Expression.Property(parameter, propertyName);
        UnaryExpression toObjectConversion = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<TAttributesObject, object?>>(toObjectConversion, parameter);
    }
}