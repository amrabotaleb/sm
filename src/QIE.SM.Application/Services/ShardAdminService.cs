using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;
using QIE.SM.Contracts;
using QIE.SM.Domain;
using QIE.SM.SharedKernel;

namespace QIE.SM.Application.Services;

/// <summary>
/// Provides shard administration services.
/// </summary>
public sealed class ShardAdminService
{
    private readonly IShardRepository _shardRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<ShardAdminService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardAdminService"/> class.
    /// </summary>
    /// <param name="shardRepository">The shard repository.</param>
    /// <param name="messagePublisher">The message publisher.</param>
    /// <param name="kafkaOptions">The Kafka options.</param>
    /// <param name="logger">The logger.</param>
    public ShardAdminService(
        IShardRepository shardRepository,
        IMessagePublisher messagePublisher,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<ShardAdminService> logger)
    {
        _shardRepository = shardRepository;
        _messagePublisher = messagePublisher;
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the fleet overview.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The fleet overview.</returns>
    public Task<FleetOverview> GetFleetOverviewAsync(CancellationToken cancellationToken) =>
        _shardRepository.GetFleetOverviewAsync(cancellationToken);

    /// <summary>
    /// Gets shards by filter.
    /// </summary>
    /// <param name="modality">The modality filter.</param>
    /// <param name="status">The status filter.</param>
    /// <param name="page">The page.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paged result.</returns>
    public Task<PagedResult<Shard>> GetPagedAsync(
        string? modality,
        ShardStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        _shardRepository.GetPagedAsync(modality, status, page, pageSize, cancellationToken);

    /// <summary>
    /// Gets a shard by identifier.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The shard.</returns>
    public Task<Shard?> GetAsync(string shardId, CancellationToken cancellationToken) =>
        _shardRepository.GetAsync(shardId, cancellationToken);

    /// <summary>
    /// Creates a shard and publishes a create command.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="modality">The modality.</param>
    /// <param name="capacity">The capacity.</param>
    /// <param name="actor">The actor.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created shard.</returns>
    public async Task<Shard> CreateAsync(
        string shardId,
        string modality,
        int capacity,
        string? actor,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var shard = new Shard
        {
            ShardId = shardId,
            Modality = modality,
            Capacity = capacity,
            Status = ShardStatus.Provisioning,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        await _shardRepository.UpsertAsync(shard, cancellationToken);

        var command = new ShardCommand
        {
            CommandId = Guid.NewGuid().ToString("N"),
            Type = ShardCommandType.Create,
            ShardId = shardId,
            Modality = modality,
            Capacity = capacity,
            Actor = actor,
            CorrelationId = correlationId,
            Utc = DateTime.UtcNow
        };

        await PublishCommandAsync(command, cancellationToken);

        _logger.LogInformation(
            "Created shard {ShardId} and dispatched create command with correlation {CorrelationId}",
            shardId,
            correlationId);

        return shard;
    }

    /// <summary>
    /// Issues a shard lifecycle command.
    /// </summary>
    /// <param name="type">The command type.</param>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="modality">The modality.</param>
    /// <param name="capacity">The capacity.</param>
    /// <param name="actor">The actor.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="graceSeconds">The grace seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task IssueCommandAsync(
        ShardCommandType type,
        string shardId,
        string modality,
        int capacity,
        string? actor,
        string? correlationId,
        int? graceSeconds,
        CancellationToken cancellationToken)
    {
        var command = new ShardCommand
        {
            CommandId = Guid.NewGuid().ToString("N"),
            Type = type,
            ShardId = shardId,
            Modality = modality,
            Capacity = capacity,
            GraceSeconds = graceSeconds,
            Actor = actor,
            CorrelationId = correlationId,
            Utc = DateTime.UtcNow
        };

        await PublishCommandAsync(command, cancellationToken);

        _logger.LogInformation(
            "Issued shard command {CommandType} for shard {ShardId} with correlation {CorrelationId}",
            type,
            shardId,
            correlationId);
    }

    private Task PublishCommandAsync(ShardCommand command, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(command);
        return _messagePublisher.PublishAsync(_kafkaOptions.Topics.ShardCommandsTopic, command.ShardId, payload, cancellationToken);
    }
}
