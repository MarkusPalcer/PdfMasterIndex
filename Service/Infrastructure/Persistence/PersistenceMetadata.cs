using AutoInterfaceAttributes;
using PdfMasterIndex.Service.Attributes;

namespace PdfMasterIndex.Service.Infrastructure.Persistence;

[AutoInterface]
[Lifetime(ServiceLifetime.Scoped)]
public class PersistenceMetadata(MasterIndexDbContext dbContext) : IPersistenceMetadata
{
    public string[] GetModelNames() => dbContext.Model.GetEntityTypes().Select(x => x.Name).ToArray();


    public ModelConstraints GetModelConstraints(string modelName)
    {
        var maxStringLengths = new Dictionary<string, int>();

        var entityType = dbContext.Model.FindEntityType(modelName);
        if (entityType == null)
        {
            throw new KeyNotFoundException($"Entity type {modelName} not found.");
        }

        foreach (var property in entityType.GetProperties())
        {
            if (property.GetMaxLength() is { } maxLength)
            {
                maxStringLengths[property.Name] = maxLength;
            }
        }

        return new ModelConstraints(maxStringLengths);
    }
}

public record ModelConstraints(Dictionary<string, int> MaxStringLengths);