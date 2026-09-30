using FluentValidation;
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
/// </summary>
public sealed class UserService : IUserService
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;
    private const string EntityName = "USERS";

    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly IReferenceLookup _lookup;
    private readonly PasswordRules _passwordRules;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IUserLogWriter _log;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserService(
        IUserRepository users,
        ISessionRepository sessions,
        IReferenceLookup lookup,
        PasswordRules passwordRules,
        IPasswordHasher hasher,
        IClock clock,
        ICurrentUser currentUser,
        IUserLogWriter log,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _users = users;
        _sessions = sessions;
        _lookup = lookup;
        _passwordRules = passwordRules;
        _hasher = hasher;
        _clock = clock;
        _currentUser = currentUser;
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

        await EnsureReferencesExistAsync(request.RoleId, request.CompanyId, ct);

        var policy = await _passwordRules.RequirePolicyAsync(request.RoleId, ct);
        _passwordRules.EnsureAcceptable(request.Password, userName, policy, Array.Empty<string>());

        var now = _clock.IndiaNow;
        var user = USERS.Create(
            userName,
            _hasher.Hash(request.Password),
            request.RoleId,
            request.CompanyId,
            request.FullName,
            request.Email,
            request.Phone,
            request.EmployeeCode,
            request.ForcePasswordChange,
            policy.ExpiryFrom(now),
            now,
            _currentUser.UserId);

        // The audit row needs the generated Id, so create is the one operation with two commits.
        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        var response = UserProjections.Map(user);
        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.UserCreated, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' created.", newValue: response), ct);
        await _users.SaveChangesAsync(ct);

        return response;
    }

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken ct) =>
        UserProjections.Map(await RequireUserAsync(id, ct));

    public Task<PagedResponse<UserResponse>> GetPageAsync(UserListRequest request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        return _users.GetPageAsync(search, page, pageSize, ct);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct)
    {
        (await _updateValidator.ValidateAsync(request, ct)).EnsureValid();

        var user = await RequireUserAsync(id, ct);
        await EnsureReferencesExistAsync(request.RoleId, request.CompanyId, ct);

        if (request.RoleId != user.RoleId)
        {
            if (id == _currentUser.UserId)
            {
                throw new BusinessException(ErrorCodes.CannotChangeOwnRole, "You cannot change your own role.");
            }

            // Same guarantee as create: a user must never end up on a role that has no password policy.
            await _passwordRules.RequirePolicyAsync(request.RoleId, ct);
        }

        var before = UserProjections.Map(user);

        user.UpdateProfile(
            request.FullName, request.Email, request.Phone, request.EmployeeCode,
            request.RoleId, request.CompanyId, _clock.IndiaNow, _currentUser.UserId);

        var after = UserProjections.Map(user);
        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.UserUpdated, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' updated.", oldValue: before, newValue: after), ct);
        await _users.SaveChangesAsync(ct);

        return after;
    }

    public async Task<UserResponse> ActivateAsync(int id, CancellationToken ct)
    {
        var user = await RequireUserAsync(id, ct);
        user.Activate(_clock.IndiaNow, _currentUser.UserId);

        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.UserActivated, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' activated."), ct);
        await _users.SaveChangesAsync(ct);

        return UserProjections.Map(user);
    }

    public async Task<UserResponse> DeactivateAsync(int id, CancellationToken ct)
    {
        if (id == _currentUser.UserId)
        {
            throw new BusinessException(ErrorCodes.CannotDeactivateSelf, "You cannot deactivate your own account.");
        }

        var user = await RequireUserAsync(id, ct);
        var now = _clock.IndiaNow;

        user.Deactivate(now, _currentUser.UserId);

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
        var user = await RequireUserAsync(id, ct);
        user.Unlock(_clock.IndiaNow, _currentUser.UserId);

        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.AccountUnlocked, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' unlocked by administrator."), ct);
        await _users.SaveChangesAsync(ct);

        return UserProjections.Map(user);
    }

    private async Task<USERS> RequireUserAsync(int id, CancellationToken ct) =>
        await _users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User");

    private async Task EnsureReferencesExistAsync(int roleId, int? companyId, CancellationToken ct)
    {
        if (!await _lookup.RoleExistsAsync(roleId, ct))
        {
            throw new NotFoundException("Role");
        }

        if (companyId.HasValue && !await _lookup.CompanyExistsAsync(companyId.Value, ct))
        {
            throw new NotFoundException("Company");
        }
    }
}
