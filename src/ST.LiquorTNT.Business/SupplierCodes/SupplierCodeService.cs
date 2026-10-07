using FluentValidation;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.SupplierCodes;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.SupplierCodes;

/// <summary>
/// Supplier codes of the companies. Company users may view their own company's supplier codes; creating
/// and editing is Super Admin's job (SYSTEM-scope rights), because supplier codes come from the CRM.
/// <para>
/// When a company gets its FIRST supplier code, its default roles are copied from the templates
/// (<see cref="RoleTemplates"/>), so the company can start creating users straight away.
/// </para>
/// </summary>
public sealed class SupplierCodeService : ISupplierCodeService
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;
    private const string EntityName = "SUPPLIER_CODE";

    private readonly ISupplierCodeRepository _supplierCodes;
    private readonly IReferenceLookup _lookup;
    private readonly RoleTemplates _templates;
    private readonly CurrentAccess _access;
    private readonly IClock _clock;
    private readonly IUserLogWriter _log;
    private readonly IValidator<CreateSupplierCodeRequest> _createValidator;
    private readonly IValidator<UpdateSupplierCodeRequest> _updateValidator;

    public SupplierCodeService(
        ISupplierCodeRepository supplierCodes,
        IReferenceLookup lookup,
        RoleTemplates templates,
        CurrentAccess access,
        IClock clock,
        IUserLogWriter log,
        IValidator<CreateSupplierCodeRequest> createValidator,
        IValidator<UpdateSupplierCodeRequest> updateValidator)
    {
        _supplierCodes = supplierCodes;
        _lookup = lookup;
        _templates = templates;
        _access = access;
        _clock = clock;
        _log = log;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResponse<SupplierCodeResponse>> GetPageAsync(SupplierCodeListRequest request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        return await _supplierCodes.GetPageAsync(await _access.CompanyScopeAsync(ct), search, page, pageSize, ct);
    }

    public async Task<SupplierCodeResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var response = await _supplierCodes.GetResponseAsync(id, ct) ?? throw new NotFoundException("Supplier code");

        if (!await _access.IsSuperAdminAsync(ct) && response.CompanyId != await _access.CompanyScopeAsync(ct))
        {
            throw new NotFoundException("Supplier code");      // another company's supplierCode
        }

        return response;
    }

    public async Task<SupplierCodeResponse> CreateAsync(CreateSupplierCodeRequest request, CancellationToken ct)
    {
        (await _createValidator.ValidateAsync(request, ct)).EnsureValid();

        if (!await _lookup.CompanyExistsAsync(request.CompanyId, ct))
        {
            throw new NotFoundException("Company");
        }

        await EnsureReferencesAsync(request.ExciseId, request.LiquorCategoryId, ct);
        await EnsureCodeFreeAsync(request.ExciseId, request.SupplierCode, excludeId: null, ct);

        var supplierCode = SUPPLIER_CODE.Create(request.CompanyId, request.FranchiseName, request.ExciseId, request.SupplierCode,
            request.LiquorCategoryId, _clock.IndiaNow, _access.UserId);

        await _supplierCodes.AddAsync(supplierCode, ct);
        await _supplierCodes.SaveChangesAsync(ct);

        // First supplier code of a new company: give it the default roles (no-op when it already has roles).
        var copiedRoles = await _templates.CopyIntoCompanyAsync(supplierCode.CompanyId, ct);

        var response = (await _supplierCodes.GetResponseAsync(supplierCode.Id, ct))!;
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.MasterCreated, UserLogModules.Masters, EntityName,
            supplierCode.Id.ToString(),
            $"Supplier code '{response.DisplayName}' created" + (copiedRoles > 0 ? $"; {copiedRoles} default roles copied into the company." : "."),
            newValue: response), ct);
        await _supplierCodes.SaveChangesAsync(ct);

        return response;
    }

    public async Task<SupplierCodeResponse> UpdateAsync(int id, UpdateSupplierCodeRequest request, CancellationToken ct)
    {
        (await _updateValidator.ValidateAsync(request, ct)).EnsureValid();

        var supplierCode = await _supplierCodes.GetByIdAsync(id, ct) ?? throw new NotFoundException("Supplier code");
        await EnsureReferencesAsync(request.ExciseId, request.LiquorCategoryId, ct);
        await EnsureCodeFreeAsync(request.ExciseId, request.SupplierCode, supplierCode.Id, ct);

        var before = await _supplierCodes.GetResponseAsync(supplierCode.Id, ct);
        supplierCode.Update(request.FranchiseName, request.ExciseId, request.SupplierCode, request.LiquorCategoryId, _clock.IndiaNow, _access.UserId);
        await _supplierCodes.SaveChangesAsync(ct);

        var after = (await _supplierCodes.GetResponseAsync(supplierCode.Id, ct))!;
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.MasterUpdated, UserLogModules.Masters, EntityName,
            supplierCode.Id.ToString(), $"Supplier code '{after.DisplayName}' updated.", oldValue: before, newValue: after), ct);
        await _supplierCodes.SaveChangesAsync(ct);

        return after;
    }

    public async Task<SupplierCodeResponse> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var supplierCode = await _supplierCodes.GetByIdAsync(id, ct) ?? throw new NotFoundException("Supplier code");
        supplierCode.SetActive(isActive, _clock.IndiaNow, _access.UserId);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.MasterUpdated, UserLogModules.Masters, EntityName,
            supplierCode.Id.ToString(), $"Supplier code {supplierCode.SupplierCode} {(isActive ? "activated" : "deactivated")}."), ct);
        await _supplierCodes.SaveChangesAsync(ct);

        return (await _supplierCodes.GetResponseAsync(supplierCode.Id, ct))!;
    }

    private async Task EnsureReferencesAsync(int exciseId, int liquorCategoryId, CancellationToken ct)
    {
        if (!await _supplierCodes.ExciseExistsAsync(exciseId, ct))
        {
            throw new NotFoundException("Excise");
        }

        if (!await _supplierCodes.LiquorCategoryIsActiveAsync(liquorCategoryId, ct))
        {
            throw new NotFoundException("Liquor category");
        }
    }

    private async Task EnsureCodeFreeAsync(int exciseId, string supplierCode, int? excludeId, CancellationToken ct)
    {
        if (await _supplierCodes.CodeExistsAsync(exciseId, supplierCode.Trim(), excludeId, ct))
        {
            throw new BusinessException(ErrorCodes.SupplierCodeTaken, "This supplier code already exists in this excise.",
                $"Supplier code '{supplierCode.Trim()}' is already used by another supplierCode of the same excise.");
        }
    }
}
