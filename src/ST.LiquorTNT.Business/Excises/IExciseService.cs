using ST.LiquorTNT.Contracts.Excises;

namespace ST.LiquorTNT.Business.Excises;

public interface IExciseService
{
    Task<IReadOnlyList<ExciseResponse>> GetAllAsync(CancellationToken ct);
}
