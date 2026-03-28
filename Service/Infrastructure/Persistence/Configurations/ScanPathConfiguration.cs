using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Configurations;

public class ScanPathConfiguration : IEntityTypeConfiguration<ScanPath>
{
    public void Configure(EntityTypeBuilder<ScanPath> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(64);
        builder.Property(x => x.ExternalPath).HasMaxLength(512);
        builder.Property(x => x.InternalPath).HasMaxLength(512);
    }
}