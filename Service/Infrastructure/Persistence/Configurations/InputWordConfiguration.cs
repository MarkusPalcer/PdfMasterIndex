using PdfMasterIndex.Service.Domain.Index;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class InputWordConfiguration
    : IEntityTypeConfiguration<InputWord>
{
    public void Configure(EntityTypeBuilder<InputWord> builder)
    {
        builder.ToTable("InputWords", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Value)
            .HasMaxLength(WordConfiguration.MaxWordLength)
            .IsRequired()
            .UseCollation(WordConfiguration.Collation);
    }
}