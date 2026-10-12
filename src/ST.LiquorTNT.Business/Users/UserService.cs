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
    private readonly ISecurityQuestionRepository _questions;
    private readonly IReferenceLookup _lookup;
    private readonly UserAccessRules _rules;
    private readonly CurrentAccess _access;
    private readonly PasswordRules _passwordRules;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly IUserLogWriter _log;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<ResetUserPasswordRequest> _resetPasswordValidator;

    public UserService(
        IUserRepository users,
        IUserAccessRepository userAccess,
        ISessionRepository sessions,
        ISecurityQuestionRepository questions,
        IReferenceLookup lookup,
        UserAccessRules rules,
        CurrentAccess access,
        PasswordRules passwordRules,
        IPasswordHasher hasher,
        IClock clock,
        IUserLogWriter log,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<ResetUserPasswordRequest> resetPasswordValidator)
    {
        _users = users;
        _userAccess = userAccess;
        _sessions = sessions;
        _questions = questions;
        _lookup = lookup;
        _rules = rules;
        _access = access;
        _passwordRules = passwordRules;
        _hasher = hasher;
        _clock = clock;
        _log = log;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _resetPasswordValidator = resetPasswordValidator;
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
        var isAdmin = await _access.IsSuperAdminAsync(ct);
        var companyId = isAdmin
            ? request.CompanyId ?? await _access.CompanyScopeAsync(ct)
            : await _access.CompanyScopeAsync(ct);

        // A new Admin belongs to no company, whatever supplier code the creating Admin has picked. Only when the
        // request names no company; a company sent together with the Admin role is still refused (400) below.
        if (isAdmin && request.CompanyId is null && await HasAdminRoleAsync(request.Roles, ct))
        {
            if (request.Roles.Select(r => r.RoleId).Distinct().Count() > 1 || request.RoleGroupIds.Count > 0)
            {
                throw new Common.Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["roles"] = new[] { "The Admin role is given alone: an Admin already holds every right, in every company." },
                });
            }

            companyId = null;
        }

        if (companyId.HasValue && !await _lookup.CompanyExistsAsync(companyId.Value, ct))
        {
            throw new NotFoundException("Company");
        }

        var roles = await _rules.ValidateRolesAsync(companyId, request.Roles, ct);
        var groups = await _rules.ValidateRoleGroupsAsync(companyId, request.RoleGroupIds, ct);
        UserAccessRules.EnsureSomeRoleOrGroup(roles.Count, groups.Count);

        // the strictest policy of every role the user will hold, directly or through a group
        var policy = await _passwordRules.RequirePolicyAsync(
            roles.Select(r => r.RoleId).Concat(groups.SelectMany(g => g.RoleIds)).ToList(), ct);
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
        var groupIds = groups.Select(g => g.Id).ToList();
        await _userAccess.ReplaceRoleGroupsAsync(user.Id, groupIds, now, _access.UserId, ct);

        var response = UserProjections.Map(user);
        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.UserCreated, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"User '{user.UserName}' created with {roles.Count} role(s) and {groupIds.Count} role group(s).",
            newValue: new { user = response, roles, roleGroups = groupIds }), ct);
        await _users.SaveChangesAsync(ct);

        return response;
    }

    private async Task<bool> HasAdminRoleAsync(IReadOnlyCollection<UserRoleAssignment> requested, CancellationToken ct) =>
        requested.Count > 0
        && (await _userAccess.GetRolesAsync(requested.Select(r => r.RoleId).Distinct().ToList(), ct)).Any(r => r.IsSystem);

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

    /// <summary>
    /// The user cannot get in (forgot the password, or the password and the security answer). The administrator sets a
    /// temporary password: the user must change it at the next login and then choose a new security question, because
    /// the old one is deleted. Open sessions and forgot-password requests end; a failed-login lock is cleared (a block is
    /// not, that stays with unlock). Everything is written in one commit.
    /// </summary>
    public async Task<UserResponse> ResetPasswordAsync(int id, ResetUserPasswordRequest request, CancellationToken ct)
    {
        (await _resetPasswordValidator.ValidateAsync(request, ct)).EnsureValid();

        if (id == _access.UserId)
        {
            throw new BusinessException(ErrorCodes.CannotResetOwnPassword,
                "You cannot reset your own password here.", "Use change password (POST /api/auth/changepassword).");
        }

        var user = await _rules.RequireUserAsync(id, ct);
        await _rules.EnsureCanManageUserAsync(user, ct);

        var policy = await _passwordRules.EnsureAcceptableForUserAsync(user, request.NewPassword, ct);

        var now = _clock.IndiaNow;
        user.SetPassword(_hasher.Hash(request.NewPassword), policy.ExpiryFrom(now), forceChange: true, now, _access.UserId);
        user.ClearTemporaryLock(now, _access.UserId);

        await _questions.RemoveAllForUserAsync(user.Id, ct);

        foreach (var reset in await _questions.GetOpenResetRequestsAsync(user.Id, ct))
        {
            reset.MarkExpired(now);
        }

        foreach (var session in await _sessions.GetActiveForUserAsync(user.Id, now, ct))
        {
            session.Revoke(now);
        }

        await _log.WriteAsync(UserLogEntry.Success(
            UserLogActions.PasswordResetByAdmin, UserLogModules.Users, EntityName, user.Id.ToString(),
            $"Temporary password set for '{user.UserName}' by administrator; security question removed."), ct);
        await _users.SaveChangesAsync(ct);      // one DbContext: user, questions, requests, sessions and the log row together

        return UserProjections.Map(user);
    }
}
