using System.Reflection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Common.Interfaces;
using RockAI.Domain.Conversations;
using RockAI.Domain.Messages;
using RockAI.Domain.Users;

namespace RockAI.Infrastructure.Common.Persistence;

public class RockAIDbContext : DbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;

    public RockAIDbContext(DbContextOptions<RockAIDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    public async Task CommitChangesAsync()
    {
        // Persist changes first. Domain events are popped and published only after a successful save
        // so handlers can rely on persisted state.
        await SaveChangesAsync();

        // collect domain events from tracked entities (pop clears the lists)
        var domainEvents = ChangeTracker.Entries<Entity>()
            .Select(entry => entry.Entity.PopDomainEvents())
            .SelectMany(x => x)
            .ToList();

        if (domainEvents.Count > 0)
        {
            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent);
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
