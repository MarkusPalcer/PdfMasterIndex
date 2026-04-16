using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfMasterIndex.Service.Domain.Index;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(128);
        builder.Property(x => x.FilePath).HasMaxLength(512);
        builder.Property(x => x.Hash).HasMaxLength(128);
    }
}