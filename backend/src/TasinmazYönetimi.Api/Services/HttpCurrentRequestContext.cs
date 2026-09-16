using Microsoft.AspNetCore.Http;
using TasinmazYönetimi.Application.Common;

namespace TasinmazYönetimi.Api.Services;

public sealed class HttpCurrentRequestContext : ICurrentRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentRequestContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string IpAddress =>
        _httpContextAccessor
            .HttpContext?
            .Connection
            .RemoteIpAddress?
            .ToString()
        ?? "unknown";
}