namespace ST.LiquorTNT.Business.Common;

/// <summary>
/// Facts about the current HTTP request that business code needs for auditing and sessions.
/// Filled by the API layer from the HttpContext; empty outside a request (tests, workers).
/// </summary>
public interface IRequestContext
{
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? CorrelationId { get; }

    /// <summary>The bearer token presented on this request, if any (the caller's own session).</summary>
    string? AccessToken { get; }
}
