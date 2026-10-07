using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Business.SupplierCodes;

public interface ISupplierCodeService
{
    Task<PagedResponse<SupplierCodeResponse>> GetPageAsync(SupplierCodeListRequest request, CancellationToken ct);

    Task<SupplierCodeResponse> GetByIdAsync(int id, CancellationToken ct);

    Task<SupplierCodeResponse> CreateAsync(CreateSupplierCodeRequest request, CancellationToken ct);

    Task<SupplierCodeResponse> UpdateAsync(int id, UpdateSupplierCodeRequest request, CancellationToken ct);

    Task<SupplierCodeResponse> SetActiveAsync(int id, bool isActive, CancellationToken ct);
}
