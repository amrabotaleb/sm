using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QIE.SM.Api.Models;
using QIE.SM.Application.Services;
using QIE.SM.Contracts;
using QIE.SM.Domain;

namespace QIE.SM.Api.Controllers;

/// <summary>
/// Provides shard administration endpoints.
/// </summary>
[ApiController]
[Route("api/sm/shards")]
public sealed class ShardAdminController : ControllerBase
{
    private readonly ShardAdminService _shardAdminService;
    private readonly ILogger<ShardAdminController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardAdminController"/> class.
    /// </summary>
    /// <param name="shardAdminService">The shard admin service.</param>
    /// <param name="logger">The logger.</param>
    public ShardAdminController(ShardAdminService shardAdminService, ILogger<ShardAdminController> logger)
    {
        _shardAdminService = shardAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the shard fleet overview.
    /// </summary>
    [HttpGet("fleet-overview")]
    [Authorize(Policy = "SmOperator")]
    public async Task<ActionResult> GetFleetOverview(CancellationToken cancellationToken)
    {
        var overview = await _shardAdminService.GetFleetOverviewAsync(cancellationToken);
        return Ok(overview);
    }

    /// <summary>
    /// Gets shards by filter.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "SmOperator")]
    public async Task<ActionResult> GetShards(
        [FromQuery] string? modality,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ShardStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse(status, true, out ShardStatus statusValue))
            {
                return BadRequest("Invalid status value.");
            }

            parsedStatus = statusValue;
        }

        var result = await _shardAdminService.GetPagedAsync(modality, parsedStatus, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets shard details by identifier.
    /// </summary>
    [HttpGet("{shardId}")]
    [Authorize(Policy = "SmOperator")]
    public async Task<ActionResult> GetShard(string shardId, CancellationToken cancellationToken)
    {
        var shard = await _shardAdminService.GetAsync(shardId, cancellationToken);
        if (shard == null)
        {
            return NotFound();
        }

        return Ok(shard);
    }

    /// <summary>
    /// Creates a new shard.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "SmAdmin")]
    public async Task<ActionResult> CreateShard([FromBody] CreateShardRequest request, CancellationToken cancellationToken)
    {
        var existing = await _shardAdminService.GetAsync(request.ShardId, cancellationToken);
        if (existing != null)
        {
            return Conflict("Shard already exists.");
        }

        var actor = User.Identity?.Name;
        var correlationId = GetCorrelationId();
        var shard = await _shardAdminService.CreateAsync(request.ShardId, request.Modality, request.Capacity, actor, correlationId, cancellationToken);

        return CreatedAtAction(nameof(GetShard), new { shardId = shard.ShardId }, shard);
    }

    /// <summary>
    /// Starts a shard.
    /// </summary>
    [HttpPost("{shardId}/start")]
    [Authorize(Policy = "SmAdmin")]
    public Task<ActionResult> StartShard(string shardId, CancellationToken cancellationToken) =>
        IssueCommandAsync(ShardCommandType.Start, shardId, cancellationToken);

    /// <summary>
    /// Stops a shard.
    /// </summary>
    [HttpPost("{shardId}/stop")]
    [Authorize(Policy = "SmAdmin")]
    public Task<ActionResult> StopShard(string shardId, CancellationToken cancellationToken) =>
        IssueCommandAsync(ShardCommandType.Stop, shardId, cancellationToken);

    /// <summary>
    /// Drains a shard.
    /// </summary>
    [HttpPost("{shardId}/drain")]
    [Authorize(Policy = "SmAdmin")]
    public Task<ActionResult> DrainShard(string shardId, CancellationToken cancellationToken) =>
        IssueCommandAsync(ShardCommandType.Drain, shardId, cancellationToken);

    /// <summary>
    /// Resumes a shard.
    /// </summary>
    [HttpPost("{shardId}/resume")]
    [Authorize(Policy = "SmAdmin")]
    public Task<ActionResult> ResumeShard(string shardId, CancellationToken cancellationToken) =>
        IssueCommandAsync(ShardCommandType.Resume, shardId, cancellationToken);

    private async Task<ActionResult> IssueCommandAsync(ShardCommandType commandType, string shardId, CancellationToken cancellationToken)
    {
        var shard = await _shardAdminService.GetAsync(shardId, cancellationToken);
        if (shard == null)
        {
            return NotFound();
        }

        var actor = User.Identity?.Name;
        var correlationId = GetCorrelationId();
        await _shardAdminService.IssueCommandAsync(
            commandType,
            shard.ShardId,
            shard.Modality,
            shard.Capacity,
            actor,
            correlationId,
            graceSeconds: commandType == ShardCommandType.Drain ? 30 : null,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Issued {CommandType} for shard {ShardId}", commandType, shardId);
        return Accepted();
    }

    private string? GetCorrelationId()
    {
        if (HttpContext.Items.TryGetValue("X-Correlation-Id", out var value))
        {
            return value?.ToString();
        }

        return null;
    }
}
