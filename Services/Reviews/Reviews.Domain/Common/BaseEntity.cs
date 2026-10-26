namespace Reviews.Domain.Common;

using MongoDB.Bson;

/// <summary>
/// Базова сутність для документів MongoDB (Критерій 2).
/// Містить сурогатний ObjectId, позначки часу та версію для оптимістичного блокування.
/// </summary>
public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; } = ObjectId.GenerateNewId();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public int Version { get; protected set; } = 1;

    protected BaseEntity()
    {
    }

    protected BaseEntity(ObjectId id, DateTime? createdAt = null)
    {
        Id = id;
        if (createdAt.HasValue)
        {
            CreatedAt = createdAt.Value;
        }
    }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void BumpVersion()
    {
        Version++;
        Touch();
    }
}
