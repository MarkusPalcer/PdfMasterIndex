using AutoInterfaceAttributes;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Domain.Index;

namespace PdfMasterIndex.Service.Infrastructure.Persistence;


[Lifetime(ServiceLifetime.Scoped)]
[AutoInterface]
public class Repository(MasterIndexDbContext context) : IRepository
{
    public IQueryable<ScanPath> ScanPaths => context.ScanPaths;
    public IQueryable<Document> Documents => context.Documents;
    public IQueryable<Word> Words => context.Words;
    public IQueryable<Occurrence> Occurrences => context.Occurrences;
    public IQueryable<Tag> Tags => context.Tags;
    
    public ValueTask<EntityEntry<T>> AddAsync<T>(T entity) where T : class
    {
        return context.AddAsync(entity);
    }

    public void Update<T>(T entity) where T : class
    {
        context.Update(entity);
    }

    public void Remove<T>(T entity) where T : class
    {
        context.Remove(entity);
    }

    public async Task BulkInsertAsync<T>(IEnumerable<T> entities) where T : class
    {
        await context.BulkInsertAsync(entities);
    }

    public async Task ClearDocumentAsync(Document document)
    {
        document.Hash = string.Empty;
        await context.SaveChangesAsync();
        
        await context.Occurrences
                     .Where(x => x.Document.Id == document.Id)
                     .ExecuteDeleteAsync();
        await context.Words
                     .Where(x => x.Occurrences.Count == 0)
                     .ExecuteDeleteAsync();

    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<List<Tag>> ProcessTags(IEnumerable<string>? usedTags)
    {
        var result = new List<Tag>();

        if (usedTags is null) return [];
        
        var availableTags = Tags.ToDictionary(x => x.Value);
        usedTags = usedTags.Distinct().ToArray();
        
        foreach (var tag in usedTags)
        {
            if (!availableTags.TryGetValue(tag, out var tagEntity))
            {
                tagEntity = new Tag
                {
                    Value = tag,
                };
                await AddAsync(tagEntity);
            }
            
            result.Add(tagEntity);
        }
        
        await SaveChangesAsync();

        return result;
    }
}