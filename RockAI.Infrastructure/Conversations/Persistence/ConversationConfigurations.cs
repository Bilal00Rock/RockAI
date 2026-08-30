using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RockAI.Domain.Conversations;
using RockAI.Domain.Users;

namespace RockAI.Infrastructure.Conversations.Persistence;

public class ConversationConfigurations : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.IsCompleted)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        builder.Property(t => t.CreatedBy);

        builder.Property(t => t.UpdatedBy);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.ConversationType)
            .HasConversion(
                conversationType => conversationType.Value,
                value => ConversationType.FromValue(value))
            .IsRequired();
    }
}
