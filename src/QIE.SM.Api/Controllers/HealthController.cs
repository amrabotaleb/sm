using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QIE.SM.Api.Controllers;

/// <summary>
/// Provides health checks.
/// </summary>
[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Returns liveness status.
    /// </summary>
    [HttpGet("live")]
    [AllowAnonymous]
    public ActionResult GetLive() => Ok(new { status = "Live" });
}
