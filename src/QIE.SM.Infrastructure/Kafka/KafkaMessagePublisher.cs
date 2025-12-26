using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;

namespace QIE.SM.Infrastructure.Kafka;

/// <summary>
/// Publishes Kafka messages.
/// </summary>
public sealed class KafkaMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaMessagePublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaMessagePublisher"/> class.
    /// </summary>
    /// <param name="options">The Kafka options.</param>
    /// <param name="logger">The logger.</param>
    public KafkaMessagePublisher(IOptions<KafkaOptions> options, ILogger<KafkaMessagePublisher> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    /// <inheritdoc />
    public async Task PublishAsync(string topic, string? key, string message, CancellationToken cancellationToken)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = key,
            Value = message
        };

        _logger.LogInformation("Publishing message to topic {Topic} with key {Key}", topic, key);

        await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(2));
        _producer.Dispose();
    }
}
