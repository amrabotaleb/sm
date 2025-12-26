using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;
using QIE.SM.Contracts;
using QIE.SM.Domain;

namespace QIE.SM.Workers;

/// <summary>
/// Processes shard lifecycle commands.
/// </summary>
public sealed class ShardManagementWorker : BackgroundService
{
    private readonly IKafkaConsumerFactory _consumerFactory;
    private readonly IShardProvisioner _shardProvisioner;
    private readonly IShardRepository _shardRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly KafkaOptions _kafkaOptions;
    private readonly WorkerGroupOptions _workerGroups;
    private readonly ILogger<ShardManagementWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardManagementWorker"/> class.
    /// </summary>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="shardProvisioner">The shard provisioner.</param>
    /// <param name="shardRepository">The shard repository.</param>
    /// <param name="messagePublisher">The message publisher.</param>
    /// <param name="kafkaOptions">The Kafka options.</param>
    /// <param name="workerGroups">The worker group options.</param>
    /// <param name="logger">The logger.</param>
    public ShardManagementWorker(
        IKafkaConsumerFactory consumerFactory,
        IShardProvisioner shardProvisioner,
        IShardRepository shardRepository,
        IMessagePublisher messagePublisher,
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<WorkerGroupOptions> workerGroups,
        ILogger<ShardManagementWorker> logger)
    {
        _consumerFactory = consumerFactory;
        _shardProvisioner = shardProvisioner;
        _shardRepository = shardRepository;
        _messagePublisher = messagePublisher;
        _kafkaOptions = kafkaOptions.Value;
        _workerGroups = workerGroups.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var consumer = _consumerFactory.CreateConsumer<ShardCommand>(_workerGroups.ShardManagementConsumerGroupId);
        consumer.Subscribe(_kafkaOptions.Topics.ShardCommandsTopic);

        _logger.LogInformation("Shard management worker subscribed to {Topic}", _kafkaOptions.Topics.ShardCommandsTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await consumer.ConsumeAsync(stoppingToken);
            if (result?.Value == null)
            {
                continue;
            }

            var command = result.Value;

            try
            {
                await HandleCommandAsync(command, stoppingToken);
                consumer.Commit(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process shard command {CommandId}", command.CommandId);
                await PublishFailureAsync(command, ex.Message, stoppingToken);
                await _shardRepository.UpdateStatusAsync(command.ShardId, ShardStatus.Failed, stoppingToken);
            }
        }
    }

    private async Task HandleCommandAsync(ShardCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing shard command {CommandType} for shard {ShardId}", command.Type, command.ShardId);

        if (command.Type == ShardCommandType.Create)
        {
            var shard = new Shard
            {
                ShardId = command.ShardId,
                Modality = command.Modality,
                Capacity = command.Capacity,
                Status = ShardStatus.Provisioning,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };
            await _shardRepository.UpsertAsync(shard, cancellationToken);
        }

        await _shardProvisioner.ExecuteAsync(command, cancellationToken);

        switch (command.Type)
        {
            case ShardCommandType.Create:
                await _shardRepository.UpdateStatusAsync(command.ShardId, ShardStatus.Active, cancellationToken);
                await PublishEventAsync(
                    command,
                    "ShardProvisioned",
                    new ShardProvisioned { ShardId = command.ShardId, Modality = command.Modality, Status = "Active" },
                    cancellationToken);
                break;
            case ShardCommandType.Start:
            case ShardCommandType.Resume:
                await _shardRepository.UpdateStatusAsync(command.ShardId, ShardStatus.Active, cancellationToken);
                await PublishEventAsync(
                    command,
                    "ShardResumed",
                    new ShardStateChanged { ShardId = command.ShardId, Status = "Active" },
                    cancellationToken);
                break;
            case ShardCommandType.Stop:
                await _shardRepository.UpdateStatusAsync(command.ShardId, ShardStatus.Stopped, cancellationToken);
                await PublishEventAsync(
                    command,
                    "ShardStopped",
                    new ShardStopped { ShardId = command.ShardId, Status = "Stopped" },
                    cancellationToken);
                break;
            case ShardCommandType.Drain:
                await _shardRepository.UpdateStatusAsync(command.ShardId, ShardStatus.Draining, cancellationToken);
                await PublishEventAsync(
                    command,
                    "ShardDrained",
                    new ShardDrained { ShardId = command.ShardId, Status = "Draining" },
                    cancellationToken);
                break;
        }
    }

    private async Task PublishEventAsync<T>(ShardCommand command, string eventType, T data, CancellationToken cancellationToken)
    {
        var envelope = new EventEnvelope<T>
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            Source = "QIE.SM.Workers.ShardManagement",
            Utc = DateTime.UtcNow,
            CorrelationId = command.CorrelationId,
            Data = data
        };

        var serialized = JsonSerializer.Serialize(envelope);
        await _messagePublisher.PublishAsync(_kafkaOptions.Topics.ShardEventsTopic, command.ShardId, serialized, cancellationToken);
    }

    private Task PublishFailureAsync(ShardCommand command, string reason, CancellationToken cancellationToken)
    {
        var failure = new ShardFailed
        {
            ShardId = command.ShardId,
            Reason = reason
        };

        return PublishEventAsync(command, "ShardFailed", failure, cancellationToken);
    }
}
