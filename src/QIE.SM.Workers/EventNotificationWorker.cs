using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;
using QIE.SM.Contracts;

namespace QIE.SM.Workers;

/// <summary>
/// Processes platform events and stores notifications.
/// </summary>
public sealed class EventNotificationWorker : BackgroundService
{
    private readonly IKafkaConsumerFactory _consumerFactory;
    private readonly IEventNotificationStore _notificationStore;
    private readonly KafkaOptions _kafkaOptions;
    private readonly WorkerGroupOptions _workerGroups;
    private readonly EventNotificationFilterOptions _filterOptions;
    private readonly ILogger<EventNotificationWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventNotificationWorker"/> class.
    /// </summary>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="notificationStore">The notification store.</param>
    /// <param name="kafkaOptions">The Kafka options.</param>
    /// <param name="workerGroups">The worker group options.</param>
    /// <param name="filterOptions">The filter options.</param>
    /// <param name="logger">The logger.</param>
    public EventNotificationWorker(
        IKafkaConsumerFactory consumerFactory,
        IEventNotificationStore notificationStore,
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<WorkerGroupOptions> workerGroups,
        IOptions<EventNotificationFilterOptions> filterOptions,
        ILogger<EventNotificationWorker> logger)
    {
        _consumerFactory = consumerFactory;
        _notificationStore = notificationStore;
        _kafkaOptions = kafkaOptions.Value;
        _workerGroups = workerGroups.Value;
        _filterOptions = filterOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var consumer = _consumerFactory.CreateConsumer<EventEnvelope<JsonElement>>(
            _workerGroups.EventNotificationConsumerGroupId);
        consumer.Subscribe(_kafkaOptions.Topics.PlatformEventsTopic);

        _logger.LogInformation("Event notification worker subscribed to {Topic}", _kafkaOptions.Topics.PlatformEventsTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await consumer.ConsumeAsync(stoppingToken);
            if (result?.Value == null)
            {
                continue;
            }

            var envelope = result.Value;

            if (!IsAllowed(envelope))
            {
                _logger.LogInformation("Filtered event {EventType} from {Source}", envelope.EventType, envelope.Source);
                consumer.Commit(result);
                continue;
            }

            await _notificationStore.AppendAsync(envelope, stoppingToken);
            consumer.Commit(result);
        }
    }

    private bool IsAllowed(EventEnvelope<JsonElement> envelope)
    {
        if (_filterOptions.AllowedSources.Count > 0 &&
            !_filterOptions.AllowedSources.Contains(envelope.Source, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_filterOptions.AllowedEventTypes.Count > 0 &&
            !_filterOptions.AllowedEventTypes.Contains(envelope.EventType, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_filterOptions.AllowedSeverities.Count > 0 &&
            !string.IsNullOrWhiteSpace(envelope.Severity) &&
            !_filterOptions.AllowedSeverities.Contains(envelope.Severity, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
