using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RockAI.Domain.Conversations;
using RockAI.Domain.Users;
using RockAI.Infrastructure.Common.Persistence;

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

        builder.Property(c => c.UserId)
    .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure ConversationType as value object (if it's a SmartEnum)
        builder.Property(t => t.ConversationType)
            .HasConversion(
                conversationType => conversationType.Value,  // Store the int value
                value => ConversationType.FromValue(value)  // Convert back to enum
            )
            .IsRequired();
        
    }
}
