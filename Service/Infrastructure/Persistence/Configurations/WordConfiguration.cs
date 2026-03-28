using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Configurations;

public class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.Property(x => x.Value).HasMaxLength(128);
    }
}