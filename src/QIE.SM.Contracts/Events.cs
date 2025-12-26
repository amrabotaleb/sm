using System.Text.Json.Serialization;

namespace QIE.SM.Contracts;

/// <summary>
/// Represents a generic event envelope.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public sealed class EventEnvelope<T>
{
    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp of the event.
    /// </summary>
    public DateTime Utc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the payload data.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the severity indicator.
    /// </summary>
    public string? Severity { get; set; }
}

/// <summary>
/// Represents an enrollment committed event.
/// </summary>
public sealed class EnrollmentCommitted
{
    /// <summary>
    /// Gets or sets the enrollment identifier.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the modality.
    /// </summary>
    public string Modality { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the manifest identifier.
    /// </summary>
    public string ManifestId { get; set; } = string.Empty;
}

/// <summary>
/// Represents a shard ingest command.
/// </summary>
public sealed class ShardIngestCommand
{
    /// <summary>
    /// Gets or sets the command identifier.
    /// </summary>
    public string CommandId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the target shard identifier.
    /// </summary>
    public string TargetShardId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the modality.
    /// </summary>
    public string Modality { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the enrollment identifier.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the manifest identifier.
    /// </summary>
    public string ManifestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp.
    /// </summary>
    public DateTime Utc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the shard command type.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShardCommandType
{
    /// <summary>
    /// Create a shard.
    /// </summary>
    Create,

    /// <summary>
    /// Start a shard.
    /// </summary>
    Start,

    /// <summary>
    /// Stop a shard.
    /// </summary>
    Stop,

    /// <summary>
    /// Drain a shard.
    /// </summary>
    Drain,

    /// <summary>
    /// Resume a shard.
    /// </summary>
    Resume
}

/// <summary>
/// Represents a shard lifecycle command.
/// </summary>
public sealed class ShardCommand
{
    /// <summary>
    /// Gets or sets the command identifier.
    /// </summary>
    public string CommandId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the command type.
    /// </summary>
    public ShardCommandType Type { get; set; }

    /// <summary>
    /// Gets or sets the shard identifier.
    /// </summary>
    public string ShardId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the modality.
    /// </summary>
    public string Modality { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the capacity.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Gets or sets the grace seconds.
    /// </summary>
    public int? GraceSeconds { get; set; }

    /// <summary>
    /// Gets or sets the actor.
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp.
    /// </summary>
    public DateTime Utc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a shard provisioned event.
/// </summary>
public sealed class ShardProvisioned
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
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a shard failure event.
/// </summary>
public sealed class ShardFailed
{
    /// <summary>
    /// Gets or sets the shard identifier.
    /// </summary>
    public string ShardId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the failure reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents a shard state change event.
/// </summary>
public sealed class ShardStateChanged
{
    /// <summary>
    /// Gets or sets the shard identifier.
    /// </summary>
    public string ShardId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a shard stopped event.
/// </summary>
public sealed class ShardStopped
{
    /// <summary>
    /// Gets or sets the shard identifier.
    /// </summary>
    public string ShardId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a shard drained event.
/// </summary>
public sealed class ShardDrained
{
    /// <summary>
    /// Gets or sets the shard identifier.
    /// </summary>
    public string ShardId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
