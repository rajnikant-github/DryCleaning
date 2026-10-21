using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> b)
    {
        b.ToTable("DocumentTypes");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(300);
        b.Property(x => x.NumberLabel).HasMaxLength(60);
        b.Property(x => x.NumberPattern).HasMaxLength(200);
        b.HasIndex(x => new { x.Role, x.Name }).IsUnique();
    }
}
