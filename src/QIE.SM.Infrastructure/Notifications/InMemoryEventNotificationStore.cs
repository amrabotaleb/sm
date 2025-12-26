using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QIE.SM.Application.Abstractions;
using QIE.SM.Contracts;

namespace QIE.SM.Infrastructure.Notifications;

/// <summary>
/// Stores event notifications in memory.
/// </summary>
public sealed class InMemoryEventNotificationStore : IEventNotificationStore
{
    private readonly ConcurrentQueue<EventEnvelope<JsonElement>> _events = new();
    private readonly ILogger<InMemoryEventNotificationStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryEventNotificationStore"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public InMemoryEventNotificationStore(ILogger<InMemoryEventNotificationStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task AppendAsync(EventEnvelope<JsonElement> envelope, CancellationToken cancellationToken)
    {
        _events.Enqueue(envelope);
        _logger.LogInformation("Stored event {EventType} from {Source}", envelope.EventType, envelope.Source);
        return Task.CompletedTask;
    }
}
