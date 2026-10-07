using FluentValidation;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// User management. Every state change is audited in the same commit as the change itself
/// (the log writer stages the row; SaveChanges writes both).
/// Company scope and admin-user protection come from <see cref="UserAccessRules"/>.
/// </summary>
public sealed class UserService : IUserService
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;
    private const string EntityName = "USERS";

    private readonly IUserRepository _users;
    private readonly IUserAccessRepository _userAccess;
    private readonly ISessionRepository _sessions;
    private readonly IReferenceLookup _lookup;
    private readonly UserAccessRules _rules;
    private readonly CurrentAccess _access;
    private readonly PasswordRules _passwordRules;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly IUserLogWriter _log;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserService(
        IUserRepository users,
        IUserAccessRepository userAccess,
        ISessionRepository sessions,
        IReferenceLookup lookup,
        UserAccessRules rules,
        CurrentAccess access,
        PasswordRules passwordRules,
        IPasswordHasher hasher,
        IClock clock,
        IUserLogWriter log,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _users = users;
        _userAccess = userAccess;
        _sessions = sessions;
        _lookup = lookup;
        _rules = rules;
        _access = access;
        _passwordRules = passwordRules;
        _hasher = hasher;
        _clock = clock;
        _log = log;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        (await _createValidator.ValidateAsync(request, ct)).EnsureValid();

        var userName = request.UserName.Trim();

        if (await _users.UserNameExistsAsync(userName, ct))
        {
            throw new BusinessException(ErrorCodes.UserNameTaken, "User name is already in use.");
        }

        // The new user joins the caller's company. Only Super Admin may name another company (or none).
        var companyId = await _access.IsSuperAdminAsync(ct)
            ? request.CompanyId ?? await _access.CompanyScopeAsync(ct)
            : await _access.CompanyScopeAsync(ct);

        if (companyId.HasValue && !await _lookup.CompanyExistsAsync(companyId.Value, ct))
        {
            throw new NotFoundException("Company");
        }

        var roles = await _rules.ValidateRolesAsync(companyId, request.Roles, ct);

        var policy = await _passwordRules.RequirePolicyAsync(roles.Select(r => r.RoleId).ToList(), ct);
        _passwordRules.EnsureAcceptable(request.Password, userName, policy, Array.Empty<string>());

        var now = _clock.IndiaNow;
        var user = USERS.Create(
            userName,
            _hasher.Hash(request.Password),
            companyId,
            request.FullName,
            request.Email,
            request.Phone,
            request.EmployeeCode,
            request.ForcePasswordChange,
            policy.ExpiryFrom(now),
            now,
            _access.UserId);

        // The roles and the audit row need the generated Id, so create is the one operation with two commits.
        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        await _userAccess.ReplaceRolesAsync(user.Id, roles, now, _access.UserId, ct);

        var response = UserProjections.Map(user);
        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.UserCreated, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' created with {roles.Count} role(s).", newValue: new { user = response, roles }), ct);
        await _users.SaveChangesAsync(ct);

        return response;
    }

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken ct) =>
        UserProjections.Map(await _rules.RequireUserAsync(id, ct));

    public async Task<PagedResponse<UserResponse>> GetPageAsync(UserListRequest request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        // Company users see their own company only; Super Admin without a supplier code sees everyone.
        return await _users.GetPageAsync(await _access.CompanyScopeAsync(ct), search, page, pageSize, ct);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct)
    {
        (await _updateValidator.ValidateAsync(request, ct)).EnsureValid();

        var user = await _rules.RequireUserAsync(id, ct);
        await _rules.EnsureCanManageUserAsync(user, ct);

        var before = UserProjections.Map(user);

        user.UpdateProfile(request.FullName, request.Email, request.Phone, request.EmployeeCode, _clock.IndiaNow, _access.UserId);

        var after = UserProjections.Map(user);
        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.UserUpdated, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' updated.", oldValue: before, newValue: after), ct);
        await _users.SaveChangesAsync(ct);

        return after;
    }

    public async Task<UserResponse> ActivateAsync(int id, CancellationToken ct)
    {
        var user = await _rules.RequireUserAsync(id, ct);
        await _rules.EnsureCanManageUserAsync(user, ct);

        user.Activate(_clock.IndiaNow, _access.UserId);

        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.UserActivated, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' activated."), ct);
        await _users.SaveChangesAsync(ct);

        return UserProjections.Map(user);
    }

    public async Task<UserResponse> DeactivateAsync(int id, CancellationToken ct)
    {
        if (id == _access.UserId)
        {
            throw new BusinessException(ErrorCodes.CannotDeactivateSelf, "You cannot deactivate your own account.");
        }

        var user = await _rules.RequireUserAsync(id, ct);
        await _rules.EnsureCanManageUserAsync(user, ct);

        var now = _clock.IndiaNow;
        user.Deactivate(now, _access.UserId);

        // A deactivated user must lose access immediately, not at their next login.
        foreach (var session in await _sessions.GetActiveForUserAsync(user.Id, now, ct))
        {
            session.Revoke(now);
        }

        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.UserDeactivated, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' deactivated."), ct);
        await _sessions.SaveChangesAsync(ct);
        await _users.SaveChangesAsync(ct);

        return UserProjections.Map(user);
    }

    public async Task<UserResponse> UnlockAsync(int id, CancellationToken ct)
    {
        var user = await _rules.RequireUserAsync(id, ct);
        await _rules.EnsureCanManageUserAsync(user, ct);

        user.Unlock(_clock.IndiaNow, _access.UserId);

        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.AccountUnlocked, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' unlocked by administrator."), ct);
        await _users.SaveChangesAsync(ct);

        return UserProjections.Map(user);
    }
}
