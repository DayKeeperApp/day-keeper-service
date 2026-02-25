using DayKeeper.Application.Interfaces;

namespace DayKeeper.Api.Services;

/// <summary>
/// Resolves the current tenant ID from the HTTP request context.
/// Reads from an "X-Tenant-Id" header; returns <c>null</c> if the header
/// is absent or not a valid GUID (no tenant filtering applied).
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    private const string _tenantIdHeader = "X-Tenant-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentTenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            if (httpContext.Request.Headers.TryGetValue(_tenantIdHeader, out var headerValue)
                && Guid.TryParse(headerValue.FirstOrDefault(), out var tenantId))
            {
                return tenantId;
            }

            // Future: read from authenticated user's JWT claims
            return null;
        }
    }
}
