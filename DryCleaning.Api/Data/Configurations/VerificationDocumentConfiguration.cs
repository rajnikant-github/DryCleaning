using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class VerificationDocumentConfiguration : IEntityTypeConfiguration<VerificationDocument>
{
    public void Configure(EntityTypeBuilder<VerificationDocument> b)
    {
        b.ToTable("VerificationDocuments");
        b.Property(x => x.FilePath).HasMaxLength(300).IsRequired();
        b.Property(x => x.FileName).HasMaxLength(200).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Number).HasMaxLength(60);
        b.Property(x => x.RejectionMessage).HasMaxLength(500);
        b.HasIndex(x => new { x.UserId, x.DocumentTypeId }).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Events).WithOne(e => e.VerificationDocument).HasForeignKey(e => e.VerificationDocumentId).OnDelete(DeleteBehavior.Restrict);
    }
}
