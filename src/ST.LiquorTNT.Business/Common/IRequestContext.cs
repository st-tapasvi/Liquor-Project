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

    string? AccessToken { get; }
}
