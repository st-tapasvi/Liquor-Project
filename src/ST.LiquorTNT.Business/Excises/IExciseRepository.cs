using ST.LiquorTNT.Contracts.Excises;

namespace ST.LiquorTNT.Business.Excises;

/// <summary>EXCISE data access. Implemented in Infrastructure.</summary>
public interface IExciseRepository
{
    /// <summary>Every excise (active and inactive), by code.</summary>
    Task<IReadOnlyList<ExciseResponse>> GetAllAsync(CancellationToken ct);
}
