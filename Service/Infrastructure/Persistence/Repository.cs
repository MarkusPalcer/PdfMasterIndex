using AutoInterfaceAttributes;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;

namespace PdfMasterIndex.Service.Infrastructure.Persistence;


[Lifetime(ServiceLifetime.Scoped)]
[AutoInterface]
public class Repository(MasterIndexDbContext context) : IRepository
{
    public IQueryable<ScanPath> ScanPaths => context.ScanPaths;
    public IQueryable<Document> Documents => context.Documents;
    public IQueryable<Word> Words => context.Words;
    
    public IQueryable<Occurrence> Occurrences => context.Occurrences;
    
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
}