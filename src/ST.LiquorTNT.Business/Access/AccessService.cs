using FluentValidation;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Contracts.Access;
using ST.LiquorTNT.Contracts.SupplierCodes;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Access;

/// <summary>
/// The caller's own access. Picking a supplier code is the important part: the frontend only sends an id,
/// the server checks the user really holds that supplier code and remembers it in USER_SESSION, so every later
/// call is scoped to it without the client ever sending a company or supplier code again.
/// </summary>
public sealed class AccessService : IAccessService
{
    private readonly SupplierCodeDirectory _supplierCodes;
    private readonly IAccessRepository _access;
    private readonly CurrentAccess _current;
    private readonly ITenantContext _tenant;
    private readonly ISessionRepository _sessions;
    private readonly ITokenHasher _tokenHasher;
    private readonly IRequestContext _request;
    private readonly IClock _clock;
    private readonly IUserLogWriter _log;
    private readonly IValidator<SelectSupplierCodeRequest> _validator;

    public AccessService(
        SupplierCodeDirectory supplierCodes,
        IAccessRepository access,
        CurrentAccess current,
        ITenantContext tenant,
        ISessionRepository sessions,
        ITokenHasher tokenHasher,
        IRequestContext request,
        IClock clock,
        IUserLogWriter log,
        IValidator<SelectSupplierCodeRequest> validator)
    {
        _supplierCodes = supplierCodes;
        _access = access;
        _current = current;
        _tenant = tenant;
        _sessions = sessions;
        _tokenHasher = tokenHasher;
        _request = request;
        _clock = clock;
        _log = log;
        _validator = validator;
    }

    public Task<IReadOnlyList<SupplierCodeResponse>> GetMySupplierCodesAsync(CancellationToken ct) =>
        _supplierCodes.ForUserAsync(_current.UserId, ct);

    public async Task<MyPermissionsResponse> SelectSupplierCodeAsync(SelectSupplierCodeRequest request, CancellationToken ct)
    {
        (await _validator.ValidateAsync(request, ct)).EnsureValid();

        var userId = _current.UserId;

        // The pick must be one of the user's own supplier codes - never trust the id the client sent.
        var supplierCode = (await _supplierCodes.ForUserAsync(userId, ct)).FirstOrDefault(s => s.Id == request.SupplierCodeId)
                      ?? throw new ForbiddenException(ErrorCodes.SupplierCodeNotAssigned,
                          "You do not work in this supplier code.",
                          $"Supplier code {request.SupplierCodeId} is not assigned to your account, or it is inactive.");

        var session = await RequireCurrentSessionAsync(ct);
        session.SelectSupplierCode(supplierCode.Id);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.SupplierCodeSelected, UserLogModules.Auth,
            "USER_SESSION", session.Id.ToString(), $"Supplier code '{supplierCode.DisplayName}' selected."), ct);
        await _sessions.SaveChangesAsync(ct);

        // The request's tenant context still holds the OLD supplier code, so the answer is built for the new one directly.
        return await BuildAsync(userId, supplierCode, ct);
    }

    public async Task<MyPermissionsResponse> GetMyPermissionsAsync(CancellationToken ct)
    {
        var supplierCode = _tenant.SupplierCodeId is int id ? await _access.GetSupplierCodeAsync(id, ct) : null;
        return await BuildAsync(_current.UserId, supplierCode, ct);
    }

    private async Task<MyPermissionsResponse> BuildAsync(int userId, SupplierCodeResponse? supplierCode, CancellationToken ct)
    {
        var isSuperAdmin = await _current.IsSuperAdminAsync(ct);

        IReadOnlyList<string> keys = isSuperAdmin
            ? await _access.GetAllPermissionKeysAsync(ct)
            : supplierCode is null
                ? Array.Empty<string>()
                : await _access.GetPermissionKeysAsync(userId, supplierCode.Id, ct);

        return new MyPermissionsResponse
        {
            IsSuperAdmin = isSuperAdmin,
            ActiveSupplierCode = supplierCode,
            Permissions = keys.OrderBy(k => k, StringComparer.Ordinal).ToList(),
        };
    }

    private async Task<USER_SESSION> RequireCurrentSessionAsync(CancellationToken ct)
    {
        var token = _request.AccessToken
                    ?? throw new UnauthorizedException(ErrorCodes.SessionInvalid, "No session token was presented.");

        var session = await _sessions.GetByTokenHashAsync(_tokenHasher.Hash(token), ct);
        if (session is null || !session.IsActiveAt(_clock.IndiaNow))
        {
            throw new UnauthorizedException(ErrorCodes.SessionInvalid, "This session has ended.", "Log in again.");
        }

        return session;
    }
}
