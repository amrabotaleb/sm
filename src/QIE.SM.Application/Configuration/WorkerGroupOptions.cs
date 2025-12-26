namespace QIE.SM.Application.Configuration;

/// <summary>
/// Represents worker-specific consumer groups.
/// </summary>
public sealed class WorkerGroupOptions
{
    /// <summary>
    /// Gets or sets the enrollment ingest consumer group identifier.
    /// </summary>
    public string EnrollmentIngestConsumerGroupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shard management consumer group identifier.
    /// </summary>
    public string ShardManagementConsumerGroupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event notification consumer group identifier.
    /// </summary>
    public string EventNotificationConsumerGroupId { get; set; } = string.Empty;
}
