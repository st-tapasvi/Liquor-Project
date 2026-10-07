using ST.LiquorTNT.Contracts.Excises;

namespace ST.LiquorTNT.Business.Excises;

/// <summary>
/// The list of state excise departments (seeded by SQL, read-only). It is the same for every company, so there is no
/// company scope; the screen shows only active ones in dropdowns.
/// </summary>
public sealed class ExciseService : IExciseService
{
    private readonly IExciseRepository _excises;

    public ExciseService(IExciseRepository excises) => _excises = excises;

    public Task<IReadOnlyList<ExciseResponse>> GetAllAsync(CancellationToken ct) => _excises.GetAllAsync(ct);
}
