using System.Security.Claims;

namespace ActivityMonitor.Api.Infrastructure;

// Existing code, not part of PR #212.
public static class ClaimsPrincipalExtensions
{
    public static int GetTenantId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("tenant_id")
                    ?? throw new UnauthorizedAccessException("Missing tenant_id claim.");
        return int.Parse(value);
    }
}
