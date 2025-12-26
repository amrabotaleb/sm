namespace QIE.SM.Domain;

/// <summary>
/// Represents a shard in the registry.
/// </summary>
public sealed class Shard
{
    /// <summary>
    /// Gets or sets the shard identifier.
    /// </summary>
    public string ShardId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the modality.
    /// </summary>
    public string Modality { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shard status.
    /// </summary>
    public ShardStatus Status { get; set; } = ShardStatus.Provisioning;

    /// <summary>
    /// Gets or sets the capacity.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation time.
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC updated time.
    /// </summary>
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents shard status values.
/// </summary>
public enum ShardStatus
{
    /// <summary>
    /// Shard is being provisioned.
    /// </summary>
    Provisioning,

    /// <summary>
    /// Shard is active.
    /// </summary>
    Active,

    /// <summary>
    /// Shard is stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// Shard is draining.
    /// </summary>
    Draining,

    /// <summary>
    /// Shard failed.
    /// </summary>
    Failed
}
