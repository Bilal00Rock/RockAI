namespace RockAI.Domain.Common.Interfaces;

public abstract class Entity
{
    public Guid Id { get; init; }

    public DateTime CreatedAt { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }

    public Guid? CreatedBy { get; protected set; }

    public Guid? UpdatedBy { get; protected set; }

    protected readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    public List<IDomainEvent> PopDomainEvents()
    {
        var copy = _domainEvents.ToList();
        _domainEvents.Clear();
        return copy;
    }

    protected Entity()
    {
    }

    protected void SetCreatedAudit(Guid? createdBy = null, DateTime? createdAt = null)
    {
        CreatedAt = createdAt ?? DateTime.UtcNow;
        CreatedBy = createdBy;
        UpdatedAt = CreatedAt;
        UpdatedBy = createdBy;
    }

    protected void SetUpdatedAudit(Guid? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
