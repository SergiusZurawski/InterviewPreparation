using System.Security.Claims;

namespace UsageSync.Api.Infrastructure;

// Existing code, not part of PR #231.
public static class ClaimsPrincipalExtensions
{
    public static int GetTenantId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("tenant_id")
                    ?? throw new UnauthorizedAccessException("Missing tenant_id claim.");
        return int.Parse(value);
    }
}
