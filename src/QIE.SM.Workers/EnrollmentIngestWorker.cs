using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;
using QIE.SM.Contracts;

namespace QIE.SM.Workers;

/// <summary>
/// Processes enrollment ingest events.
/// </summary>
public sealed class EnrollmentIngestWorker : BackgroundService
{
    private readonly IKafkaConsumerFactory _consumerFactory;
    private readonly IEnrollmentManifestRepository _manifestRepository;
    private readonly IShardRouter _shardRouter;
    private readonly IMessagePublisher _messagePublisher;
    private readonly KafkaOptions _kafkaOptions;
    private readonly WorkerGroupOptions _workerGroups;
    private readonly ILogger<EnrollmentIngestWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnrollmentIngestWorker"/> class.
    /// </summary>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="manifestRepository">The manifest repository.</param>
    /// <param name="shardRouter">The shard router.</param>
    /// <param name="messagePublisher">The message publisher.</param>
    /// <param name="kafkaOptions">The Kafka options.</param>
    /// <param name="workerGroups">The worker group options.</param>
    /// <param name="logger">The logger.</param>
    public EnrollmentIngestWorker(
        IKafkaConsumerFactory consumerFactory,
        IEnrollmentManifestRepository manifestRepository,
        IShardRouter shardRouter,
        IMessagePublisher messagePublisher,
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<WorkerGroupOptions> workerGroups,
        ILogger<EnrollmentIngestWorker> logger)
    {
        _consumerFactory = consumerFactory;
        _manifestRepository = manifestRepository;
        _shardRouter = shardRouter;
        _messagePublisher = messagePublisher;
        _kafkaOptions = kafkaOptions.Value;
        _workerGroups = workerGroups.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var consumer = _consumerFactory.CreateConsumer<EventEnvelope<EnrollmentCommitted>>(
            _workerGroups.EnrollmentIngestConsumerGroupId);
        consumer.Subscribe(_kafkaOptions.Topics.EnrollmentEventsTopic);

        _logger.LogInformation("Enrollment ingest worker subscribed to {Topic}", _kafkaOptions.Topics.EnrollmentEventsTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await consumer.ConsumeAsync(stoppingToken);
            if (result?.Value?.Data == null)
            {
                continue;
            }

            var envelope = result.Value;
            var payload = envelope.Data;

            try
            {
                var manifestJson = await _manifestRepository.GetManifestJsonAsync(payload.ManifestId, stoppingToken);
                if (manifestJson == null)
                {
                    _logger.LogWarning(
                        "Manifest {ManifestId} missing for identifier {Identifier}",
                        payload.ManifestId,
                        payload.Identifier);
                    continue;
                }

                var targetShard = await _shardRouter.ResolveTargetShardAsync(payload.Modality, payload.Identifier, stoppingToken);
                var command = new ShardIngestCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"),
                    TargetShardId = targetShard,
                    Modality = payload.Modality,
                    Identifier = payload.Identifier,
                    ManifestId = payload.ManifestId,
                    CorrelationId = envelope.CorrelationId,
                    Utc = DateTime.UtcNow
                };

                var serialized = JsonSerializer.Serialize(command);
                await _messagePublisher.PublishAsync(
                    _kafkaOptions.Topics.ShardIngestCommandsTopic,
                    payload.Identifier,
                    serialized,
                    stoppingToken);

                consumer.Commit(result);
                _logger.LogInformation(
                    "Published ingest command for identifier {Identifier} to shard {ShardId}",
                    payload.Identifier,
                    targetShard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process enrollment event {EventId}", envelope.EventId);
            }
        }
    }
}
