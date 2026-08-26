using RockAI.Domain.Messages;
using RockAI.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RockAI.Infrastructure.Messages.Persistence;

public class MessageConfigurations : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Content)
            .IsRequired();
        
        builder.Property(t => t.CreatedAt)
            .IsRequired();
        
        builder.Property(s => s.ConversationId)
            .IsRequired();

        // Configure MessageRole as value object (if it's a SmartEnum)
        builder.Property(t => t.MessageRole)
            .HasConversion(
                messageRole => messageRole.Value,  // Store the int value
                value => MessageRole.FromValue(value)  // Convert back to enum
            )
            .IsRequired();

        // Configure MessageStatus as value object (if it's a SmartEnum)
        builder.Property(t => t.Status)
            .HasConversion(
                messageStatus => messageStatus.Value,  // Store the int value
                value => MessageStatus.FromValue(value)  // Convert back to enum
            )
            .IsRequired();

    }
}
