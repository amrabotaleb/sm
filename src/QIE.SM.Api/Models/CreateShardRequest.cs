namespace QIE.SM.Api.Models;

/// <summary>
/// Represents a request to create a shard.
/// </summary>
public sealed class CreateShardRequest
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
    /// Gets or sets the capacity.
    /// </summary>
    public int Capacity { get; set; }
}
