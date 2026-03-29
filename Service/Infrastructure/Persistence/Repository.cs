using AutoInterfaceAttributes;
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

    public async Task ClearDocumentAsync(Document document)
    {
        await context.Entry(document).Collection(x => x.Content).LoadAsync();
        context.RemoveRange(document.Content);
        document.Content.Clear();
        document.Hash.Value = [];
        await context.SaveChangesAsync();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}