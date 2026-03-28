using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        var hash = builder.ComplexProperty(d => d.Hash);

        var valueComparer = new ValueComparer<byte[]>(
            (x, y) => ReferenceEquals(x, y),
            x => x.GetHashCode(),
            x => x
        );

        hash.Property(d => d.Value)
            .HasConversion(arr => Convert.ToBase64String(arr),
                           str => Convert.FromBase64String(str),
                           valueComparer)
            .HasMaxLength(128);
        hash.Property(d => d.Algorithm).HasConversion<string>();

        builder.Property(x => x.Name).HasMaxLength(64);
        builder.Property(x => x.FilePath).HasMaxLength(512);
    }
}