using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;

namespace PdfMasterIndex.Service.Infrastructure.Persistence;

public class MasterIndexDbContext(DbContextOptions<MasterIndexDbContext> options) : DbContext(options)
{
    public DbSet<ScanPath> ScanPaths { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Word> Words { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterIndexDbContext).Assembly);
    }
}
