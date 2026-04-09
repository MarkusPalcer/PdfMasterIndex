using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Domain.Index;

namespace PdfMasterIndex.Service.Infrastructure.Persistence;

public class MasterIndexDbContext(DbContextOptions<MasterIndexDbContext> options) : DbContext(options)
{
    public DbSet<ScanPath> ScanPaths { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Word> Words { get; set; }
    public DbSet<Occurrence> Occurrences { get; set; }
    public DbSet<Tag> Tags { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterIndexDbContext).Assembly);
    }
}
