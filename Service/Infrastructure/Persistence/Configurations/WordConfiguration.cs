using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfMasterIndex.Service.Domain.Index;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Configurations;

public class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public const int MaxWordLength = 128;

    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.Property(x => x.Value).HasMaxLength(MaxWordLength);
    }
}