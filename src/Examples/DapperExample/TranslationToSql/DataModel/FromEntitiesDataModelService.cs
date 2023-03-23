using System.Data.Common;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace DapperExample.TranslationToSql.DataModel;

/// <summary>
/// Derives foreign keys from an Entity Framework Core model.
/// </summary>
public sealed class FromEntitiesDataModelService : BaseDataModelService
{
    private readonly Dictionary<RelationshipAttribute, RelationshipForeignKey> _foreignKeysByRelationship = new();
    private string? _connectionString;

    public FromEntitiesDataModelService(IResourceGraph resourceGraph)
        : base(resourceGraph)
    {
    }

    public void Initialize(DbContext dbContext)
    {
        ScanForeignKeys(dbContext.Model);
        Initialize();

        _connectionString = dbContext.Database.GetConnectionString();
    }

    private void ScanForeignKeys(IModel entityModel)
    {
        foreach (RelationshipAttribute relationship in ResourceGraph.GetResourceTypes().SelectMany(resourceType => resourceType.Relationships))
        {
            IEntityType? leftEntityType = entityModel.FindEntityType(relationship.LeftType.ClrType);
            INavigation? navigation = leftEntityType?.FindNavigation(relationship.Property.Name);

            if (navigation != null)
            {
                bool isAtLeftSide = navigation.ForeignKey.DeclaringEntityType.ClrType == relationship.LeftType.ClrType;
                string columnName = navigation.ForeignKey.Properties.Single().Name;
                bool isNullable = !navigation.ForeignKey.IsRequired;

                var foreignKey = new RelationshipForeignKey(relationship, isAtLeftSide, columnName, isNullable);
                _foreignKeysByRelationship[relationship] = foreignKey;
            }
        }
    }

    public override DbConnection CreateConnection()
    {
        if (_connectionString == null)
        {
            throw new InvalidOperationException($"Call {nameof(Initialize)} first.");
        }

        return new NpgsqlConnection(_connectionString);
    }

    public override RelationshipForeignKey GetForeignKey(RelationshipAttribute relationship)
    {
        if (_foreignKeysByRelationship.TryGetValue(relationship, out RelationshipForeignKey? foreignKey))
        {
            return foreignKey;
        }

        throw new InvalidOperationException(
            $"Foreign key mapping for relationship '{relationship.LeftType.ClrType.Name}.{relationship.Property.Name}' is unavailable.");
    }
}
