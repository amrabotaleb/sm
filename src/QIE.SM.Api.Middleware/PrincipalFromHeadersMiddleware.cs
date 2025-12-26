using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace QIE.SM.Api.Middleware;

/// <summary>
/// Builds a principal from trusted headers.
/// </summary>
public sealed class PrincipalFromHeadersMiddleware
{
    private const string UserHeader = "X-User";
    private const string RolesHeader = "X-Roles";

    private readonly RequestDelegate _next;
    private readonly ILogger<PrincipalFromHeadersMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrincipalFromHeadersMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate.</param>
    /// <param name="logger">The logger.</param>
    public PrincipalFromHeadersMiddleware(RequestDelegate next, ILogger<PrincipalFromHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(UserHeader, out var userHeader) &&
            !string.IsNullOrWhiteSpace(userHeader))
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, userHeader.ToString()) };

            if (context.Request.Headers.TryGetValue(RolesHeader, out var rolesHeader))
            {
                var roles = rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TrustedHeaders"));
            _logger.LogInformation("Created principal for {User}", userHeader.ToString());
        }
        else
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        await _next(context);
    }
}
