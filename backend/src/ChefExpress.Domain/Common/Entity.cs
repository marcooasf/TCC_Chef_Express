namespace ChefExpress.Domain.Common;

/// <summary>Base type for aggregates/entities identified by a GUID.</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
