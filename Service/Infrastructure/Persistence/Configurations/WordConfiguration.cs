using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfMasterIndex.Service.Domain.Index;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Configurations;

public class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public const int MaxWordLength = 128;
    public const string Collation = "SQL_Latin1_General_CP1_CI_AS";

    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");
        
        builder.Property(x => x.Value)
            .HasMaxLength(MaxWordLength)
            .IsRequired();

        builder.HasIndex(x => x.Value)
            .IsUnique();
    }
}