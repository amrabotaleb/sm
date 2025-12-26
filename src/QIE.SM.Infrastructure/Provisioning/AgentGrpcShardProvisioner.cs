using Microsoft.Extensions.Logging;
using QIE.SM.Application.Abstractions;
using QIE.SM.Contracts;

namespace QIE.SM.Infrastructure.Provisioning;

/// <summary>
/// Executes shard provisioning using an agent gRPC stub.
/// </summary>
public sealed class AgentGrpcShardProvisioner : IShardProvisioner
{
    private readonly ILogger<AgentGrpcShardProvisioner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentGrpcShardProvisioner"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AgentGrpcShardProvisioner(ILogger<AgentGrpcShardProvisioner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(ShardCommand command, CancellationToken cancellationToken)
    {
        // Placeholder for agent gRPC client integration.
        _logger.LogInformation(
            "Agent gRPC provisioner invoked for {CommandType} on shard {ShardId}",
            command.Type,
            command.ShardId);

        return Task.CompletedTask;
    }
}
