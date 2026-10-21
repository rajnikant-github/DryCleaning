using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DryCleaning.Api.Data.Configurations;

public sealed class DocumentReviewEventConfiguration : IEntityTypeConfiguration<DocumentReviewEvent>
{
    public void Configure(EntityTypeBuilder<DocumentReviewEvent> b)
    {
        b.ToTable("DocumentReviewEvents");
        b.Property(x => x.Message).HasMaxLength(500);
        b.HasIndex(x => x.VerificationDocumentId);
    }
}
