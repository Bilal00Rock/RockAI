using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RockAI.Domain.Attachments;
using RockAI.Domain.Messages;

namespace RockAI.Infrastructure.Attachments.Persistence;

public class AttachmentConfigurations : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.MessageId)
            .IsRequired();

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(a => a.OriginalFileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(a => a.Extension)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(a => a.MimeType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(a => a.SizeBytes)
            .IsRequired();

        builder.Property(a => a.RelativePath)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(a => a.Status)
            .HasConversion(
                status => status.Value,
                value => AttachmentStatus.FromValue(value))
            .IsRequired();

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt);

        builder.Property(a => a.CreatedBy);

        builder.Property(a => a.UpdatedBy);

        builder.Property(a => a.ProcessedAt);

        builder.HasIndex(a => a.MessageId);
    }
}
