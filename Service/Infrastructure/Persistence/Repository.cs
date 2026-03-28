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

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}