using DapperExample.TranslationToSql.DataModel;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;

namespace DapperExample.TranslationToSql.Builders;

internal sealed class DeleteOneToOneStatementBuilder : StatementBuilder
{
    public DeleteOneToOneStatementBuilder(IDataModelService dataModelService)
        : base(dataModelService)
    {
    }

    public DeleteNode Build(ResourceType resourceType, string whereColumnName, object? whereValue)
    {
        ArgumentGuard.NotNull(resourceType);
        ArgumentGuard.NotNull(whereColumnName);

        ResetState();

        TableNode table = GetTable(resourceType, null);

        ColumnNode column = table.GetColumn(whereColumnName);
        FilterNode where = GetWhere(column, whereValue);

        return new DeleteNode(table, where);
    }

    private FilterNode GetWhere(ColumnNode column, object? value)
    {
        ParameterNode parameter = ParameterGenerator.Create(value);
        return new ComparisonNode(ComparisonOperator.Equals, column, parameter);
    }
}