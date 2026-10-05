namespace ST.LiquorTNT.Business.Common;

/// <summary>
/// Company / plant / excise of the current session, taken from the token by the API layer.
/// These values are NEVER read from the request body, query string or a client header.
/// </summary>
public interface ITenantContext
{
    int? CompanyId { get; }
    int? PlantId { get; }
    string? ExciseCode { get; }
}
