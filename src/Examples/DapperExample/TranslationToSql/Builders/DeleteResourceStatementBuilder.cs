using DapperExample.TranslationToSql.DataModel;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;

namespace DapperExample.TranslationToSql.Builders;

internal sealed class DeleteResourceStatementBuilder : StatementBuilder
{
    public DeleteResourceStatementBuilder(IDataModelService dataModelService)
        : base(dataModelService)
    {
    }

    public DeleteNode Build(ResourceType resourceType, params object[] idValues)
    {
        ArgumentGuard.NotNull(resourceType);
        ArgumentGuard.NotNullNorEmpty(idValues);

        ResetState();

        TableNode table = GetTable(resourceType, null);

        TableColumnNode idColumn = table.GetIdColumn();
        FilterNode where = GetWhere(idColumn, idValues);

        return new DeleteNode(table, where);
    }

    private FilterNode GetWhere(TableColumnNode idColumn, IEnumerable<object> idValues)
    {
        List<ParameterNode> parameters = idValues.Select(idValue => ParameterGenerator.Create(idValue)).ToList();
        return parameters.Count == 1 ? new ComparisonNode(ComparisonOperator.Equals, idColumn, parameters[0]) : new InNode(idColumn, parameters);
    }
}
