using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;

namespace QIE.SM.Infrastructure.Kafka;

/// <summary>
/// Creates Kafka consumers.
/// </summary>
public sealed class KafkaConsumerFactory : IKafkaConsumerFactory
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaConsumerFactory"/> class.
    /// </summary>
    /// <param name="options">The Kafka options.</param>
    /// <param name="logger">The logger.</param>
    public KafkaConsumerFactory(IOptions<KafkaOptions> options, ILogger<KafkaConsumer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public IAsyncDisposableKafkaConsumer<T> CreateConsumer<T>(string consumerGroupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = consumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var consumer = new ConsumerBuilder<string, string>(config).Build();
        return new KafkaConsumer<T>(consumer, _logger);
    }

    /// <summary>
    /// Kafka consumer implementation.
    /// </summary>
    private sealed class KafkaConsumer<T> : IAsyncDisposableKafkaConsumer<T>
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger _logger;

        public KafkaConsumer(IConsumer<string, string> consumer, ILogger logger)
        {
            _consumer = consumer;
            _logger = logger;
        }

        public void Subscribe(string topic) => _consumer.Subscribe(topic);

        public async Task<KafkaConsumeResult<T>?> ConsumeAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = _consumer.Consume(cancellationToken);
                if (result == null)
                {
                    return null;
                }

                var payload = result.Message?.Value;
                T? value = default;
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    value = JsonSerializer.Deserialize<T>(payload);
                }

                return await Task.FromResult(new KafkaConsumeResult<T>
                {
                    Topic = result.Topic,
                    Key = result.Message?.Key,
                    Value = value,
                    RawValue = payload
                });
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error {Reason}", ex.Error.Reason);
                return null;
            }
        }

        public void Commit(KafkaConsumeResult<T> result)
        {
            _consumer.Commit();
        }

        public ValueTask DisposeAsync()
        {
            _consumer.Close();
            _consumer.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
