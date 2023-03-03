using DapperExample.TranslationToSql.DataModel;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;

namespace DapperExample.TranslationToSql.Builders;

internal sealed class UpdateClearOneToOneStatementBuilder : StatementBuilder
{
    public UpdateClearOneToOneStatementBuilder(IDataModelService dataModelService)
        : base(dataModelService)
    {
    }

    public UpdateNode Build(ResourceType resourceType, string setColumnName, string whereColumnName, object? whereValue)
    {
        ArgumentGuard.NotNull(resourceType);
        ArgumentGuard.NotNull(setColumnName);
        ArgumentGuard.NotNull(whereColumnName);

        ResetState();

        TableNode table = GetTable(resourceType, null);

        ColumnNode setColumn = table.GetColumn(setColumnName);
        ColumnAssignmentNode columnAssignment = GetColumnAssignment(setColumn);

        ColumnNode whereColumn = table.GetColumn(whereColumnName);
        FilterNode where = GetWhere(whereColumn, whereValue);

        return new UpdateNode(table, columnAssignment.AsList(), where);
    }

    private FilterNode GetWhere(ColumnNode column, object? value)
    {
        ParameterNode whereParameter = ParameterGenerator.Create(value);
        return new ComparisonNode(ComparisonOperator.Equals, column, whereParameter);
    }

    private ColumnAssignmentNode GetColumnAssignment(ColumnNode setColumn)
    {
        ParameterNode parameter = ParameterGenerator.Create(null);
        return new ColumnAssignmentNode(setColumn, parameter);
    }
}
