using System.Security.Claims;
using TodoApi.Data;

namespace TodoApi;

/// <summary>Resolves the current user from the authenticated principal of the HTTP request.</summary>
public class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? Id => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
