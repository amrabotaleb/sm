namespace QIE.SM.Application.Configuration;

/// <summary>
/// Represents Kafka configuration options.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>
    /// Gets or sets the bootstrap servers.
    /// </summary>
    public string BootstrapServers { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consumer group identifier.
    /// </summary>
    public string ConsumerGroupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the topic configuration.
    /// </summary>
    public KafkaTopicOptions Topics { get; set; } = new();
}

/// <summary>
/// Represents Kafka topic names.
/// </summary>
public sealed class KafkaTopicOptions
{
    /// <summary>
    /// Gets or sets the enrollment events topic.
    /// </summary>
    public string EnrollmentEventsTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shard commands topic.
    /// </summary>
    public string ShardCommandsTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shard events topic.
    /// </summary>
    public string ShardEventsTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shard ingest commands topic.
    /// </summary>
    public string ShardIngestCommandsTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the platform events topic.
    /// </summary>
    public string PlatformEventsTopic { get; set; } = string.Empty;
}
