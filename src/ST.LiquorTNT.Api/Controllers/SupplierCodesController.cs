using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.SupplierCodes;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Api.Controllers;

/// <summary>
/// Supplier codes. Company users see their own company's; creating and editing is Super Admin only
/// (SYSTEM-scope rights). The first supplier code of a company also gives it the default roles.
/// </summary>
[ApiController]
[Route("api/suppliercodes")]
public sealed class SupplierCodesController : ControllerBase
{
    private readonly ISupplierCodeService _supplierCodes;

    public SupplierCodesController(ISupplierCodeService supplierCodes) => _supplierCodes = supplierCodes;

    [HttpGet]
    [HasPermission(Permissions.SupplierCodeView)]
    public async Task<ActionResult<PagedResponse<SupplierCodeResponse>>> GetPageAsync([FromQuery] SupplierCodeListRequest request, CancellationToken ct)
        => Ok(await _supplierCodes.GetPageAsync(request, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.SupplierCodeView)]
    public async Task<ActionResult<SupplierCodeResponse>> GetByIdAsync(int id, CancellationToken ct)
        => Ok(await _supplierCodes.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.SupplierCodeAdd)]
    public async Task<ActionResult<SupplierCodeResponse>> CreateAsync(CreateSupplierCodeRequest request, CancellationToken ct)
    {
        var created = await _supplierCodes.CreateAsync(request, ct);
        return Created($"/api/suppliercodes/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.SupplierCodeEdit)]
    public async Task<ActionResult<SupplierCodeResponse>> UpdateAsync(int id, UpdateSupplierCodeRequest request, CancellationToken ct)
        => Ok(await _supplierCodes.UpdateAsync(id, request, ct));

    [HttpPost("{id:int}/activate")]
    [HasPermission(Permissions.SupplierCodeEdit)]
    public async Task<ActionResult<SupplierCodeResponse>> ActivateAsync(int id, CancellationToken ct)
        => Ok(await _supplierCodes.SetActiveAsync(id, isActive: true, ct));

    [HttpPost("{id:int}/deactivate")]
    [HasPermission(Permissions.SupplierCodeEdit)]
    public async Task<ActionResult<SupplierCodeResponse>> DeactivateAsync(int id, CancellationToken ct)
        => Ok(await _supplierCodes.SetActiveAsync(id, isActive: false, ct));
}
