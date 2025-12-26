using System.Text.Json;
using QIE.SM.Contracts;
using QIE.SM.Domain;
using QIE.SM.SharedKernel;

namespace QIE.SM.Application.Abstractions;

/// <summary>
/// Publishes messages to a transport.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    /// <param name="topic">The topic.</param>
    /// <param name="key">The message key.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PublishAsync(string topic, string? key, string message, CancellationToken cancellationToken);
}

/// <summary>
/// Creates Kafka consumers for a specific message type.
/// </summary>
public interface IKafkaConsumerFactory
{
    /// <summary>
    /// Creates a consumer instance.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="consumerGroupId">The consumer group identifier.</param>
    /// <returns>The consumer.</returns>
    IAsyncDisposableKafkaConsumer<T> CreateConsumer<T>(string consumerGroupId);
}

/// <summary>
/// Represents an async disposable Kafka consumer.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public interface IAsyncDisposableKafkaConsumer<T> : IAsyncDisposable
{
    /// <summary>
    /// Subscribes to a Kafka topic.
    /// </summary>
    /// <param name="topic">The topic name.</param>
    void Subscribe(string topic);

    /// <summary>
    /// Consumes the next message.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The consumed result.</returns>
    Task<KafkaConsumeResult<T>?> ConsumeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Commits the offset for the last consumed message.
    /// </summary>
    void Commit(KafkaConsumeResult<T> result);
}

/// <summary>
/// Represents a consumed Kafka record.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public sealed class KafkaConsumeResult<T>
{
    /// <summary>
    /// Gets or sets the topic name.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message key.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the message value.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Gets or sets the raw JSON value.
    /// </summary>
    public string? RawValue { get; set; }
}

/// <summary>
/// Provides enrollment manifest data.
/// </summary>
public interface IEnrollmentManifestRepository
{
    /// <summary>
    /// Gets the manifest JSON by manifest identifier.
    /// </summary>
    /// <param name="manifestId">The manifest identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The manifest JSON or null.</returns>
    Task<string?> GetManifestJsonAsync(string manifestId, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves target shards for enrollment ingest.
/// </summary>
public interface IShardRouter
{
    /// <summary>
    /// Resolves the target shard identifier.
    /// </summary>
    /// <param name="modality">The modality.</param>
    /// <param name="identifier">The enrollment identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The shard identifier.</returns>
    Task<string> ResolveTargetShardAsync(string modality, string identifier, CancellationToken cancellationToken);
}

/// <summary>
/// Provides shard registry operations.
/// </summary>
public interface IShardRepository
{
    /// <summary>
    /// Gets a shard by identifier.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The shard or null.</returns>
    Task<Shard?> GetAsync(string shardId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets shards filtered by modality and status.
    /// </summary>
    /// <param name="modality">The modality filter.</param>
    /// <param name="status">The status filter.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paged result.</returns>
    Task<PagedResult<Shard>> GetPagedAsync(string? modality, ShardStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Gets active shards for a modality.
    /// </summary>
    /// <param name="modality">The modality.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of active shards.</returns>
    Task<IReadOnlyCollection<Shard>> GetActiveAsync(string modality, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates a shard.
    /// </summary>
    /// <param name="shard">The shard.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpsertAsync(Shard shard, CancellationToken cancellationToken);

    /// <summary>
    /// Updates shard status.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateStatusAsync(string shardId, ShardStatus status, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a fleet overview.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The fleet overview.</returns>
    Task<FleetOverview> GetFleetOverviewAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Executes shard provisioning actions.
/// </summary>
public interface IShardProvisioner
{
    /// <summary>
    /// Executes the shard command.
    /// </summary>
    /// <param name="command">The shard command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ExecuteAsync(ShardCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Stores notification events.
/// </summary>
public interface IEventNotificationStore
{
    /// <summary>
    /// Appends an event envelope to the store.
    /// </summary>
    /// <param name="envelope">The event envelope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AppendAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken);
}

/// <summary>
/// Represents fleet overview details.
/// </summary>
public sealed class FleetOverview
{
    /// <summary>
    /// Gets or sets the total shard count.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the active shard count.
    /// </summary>
    public int Active { get; set; }

    /// <summary>
    /// Gets or sets the stopped shard count.
    /// </summary>
    public int Stopped { get; set; }

    /// <summary>
    /// Gets or sets the draining shard count.
    /// </summary>
    public int Draining { get; set; }

    /// <summary>
    /// Gets or sets the failed shard count.
    /// </summary>
    public int Failed { get; set; }
}
