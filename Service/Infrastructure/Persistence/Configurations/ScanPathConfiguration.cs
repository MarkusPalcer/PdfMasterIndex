using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfMasterIndex.Service.Domain.Index;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Configurations;

public class ScanPathConfiguration : IEntityTypeConfiguration<ScanPath>
{
    public void Configure(EntityTypeBuilder<ScanPath> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(64);
        builder.Property(x => x.Path).HasMaxLength(512);
    }
}