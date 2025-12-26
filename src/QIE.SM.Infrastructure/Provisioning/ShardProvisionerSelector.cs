using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;
using QIE.SM.Contracts;

namespace QIE.SM.Infrastructure.Provisioning;

/// <summary>
/// Selects a shard provisioner implementation.
/// </summary>
public sealed class ShardProvisionerSelector : IShardProvisioner
{
    private readonly KubernetesShardProvisioner _kubernetes;
    private readonly AgentGrpcShardProvisioner _agentGrpc;
    private readonly ShardProvisionerOptions _options;
    private readonly ILogger<ShardProvisionerSelector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardProvisionerSelector"/> class.
    /// </summary>
    /// <param name="kubernetes">The Kubernetes provisioner.</param>
    /// <param name="agentGrpc">The agent gRPC provisioner.</param>
    /// <param name="options">The options.</param>
    /// <param name="logger">The logger.</param>
    public ShardProvisionerSelector(
        KubernetesShardProvisioner kubernetes,
        AgentGrpcShardProvisioner agentGrpc,
        IOptions<ShardProvisionerOptions> options,
        ILogger<ShardProvisionerSelector> logger)
    {
        _kubernetes = kubernetes;
        _agentGrpc = agentGrpc;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(ShardCommand command, CancellationToken cancellationToken)
    {
        if (string.Equals(_options.Mode, "AgentGrpc", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Using agent gRPC provisioner for shard {ShardId}", command.ShardId);
            return _agentGrpc.ExecuteAsync(command, cancellationToken);
        }

        _logger.LogInformation("Using Kubernetes provisioner for shard {ShardId}", command.ShardId);
        return _kubernetes.ExecuteAsync(command, cancellationToken);
    }
}
