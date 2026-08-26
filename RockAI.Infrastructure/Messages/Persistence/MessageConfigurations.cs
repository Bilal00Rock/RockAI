using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RockAI.Domain.Conversations;
using RockAI.Domain.Messages;

namespace RockAI.Infrastructure.Messages.Persistence;

public class MessageConfigurations : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(m => m.MessageRole)
            .HasConversion(
                messageRole => messageRole.Value,
                value => MessageRole.FromValue(value))
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion(
                messageStatus => messageStatus.Value,
                value => MessageStatus.FromValue(value))
            .IsRequired();
    }
}