using FluentValidation;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// Assigning master roles and custom rights to a user. The request always carries the FULL list; the service
/// works out what was added and removed. A change applies from the user's next API call (rights are read per
/// request), so nobody has to log in again. All the "who may give what" rules are in <see cref="UserAccessRules"/>.
/// </summary>
public sealed class UserAccessService : IUserAccessService
{
    private const string EntityName = "USERS";

    private readonly IUserAccessRepository _userAccess;
    private readonly UserAccessRules _rules;
    private readonly PasswordRules _passwordRules;
    private readonly CurrentAccess _access;
    private readonly IClock _clock;
    private readonly IUserLogWriter _log;
    private readonly IValidator<UpdateUserRolesRequest> _rolesValidator;
    private readonly IValidator<UpdateUserRightsRequest> _rightsValidator;

    public UserAccessService(
        IUserAccessRepository userAccess,
        UserAccessRules rules,
        PasswordRules passwordRules,
        CurrentAccess access,
        IClock clock,
        IUserLogWriter log,
        IValidator<UpdateUserRolesRequest> rolesValidator,
        IValidator<UpdateUserRightsRequest> rightsValidator)
    {
        _userAccess = userAccess;
        _rules = rules;
        _passwordRules = passwordRules;
        _access = access;
        _clock = clock;
        _log = log;
        _rolesValidator = rolesValidator;
        _rightsValidator = rightsValidator;
    }

    public async Task<UserAccessResponse> GetAsync(int userId, CancellationToken ct)
    {
        var user = await _rules.RequireUserAsync(userId, ct);
        return await _userAccess.GetAccessAsync(user, ct);
    }

    public async Task<UserAccessResponse> UpdateRolesAsync(int userId, UpdateUserRolesRequest request, CancellationToken ct)
    {
        (await _rolesValidator.ValidateAsync(request, ct)).EnsureValid();

        var user = await _rules.RequireUserAsync(userId, ct);
        await _rules.EnsureNotSelfAsync(user, ct);
        await _rules.EnsureCanManageUserAsync(user, ct);      // taking roles away from an admin user is protected too

        var roles = await _rules.ValidateRolesAsync(user.CompanyId, request.Roles, ct);

        // The user's password must stay governable: every new role needs a password policy.
        await _passwordRules.RequirePolicyAsync(roles.Select(r => r.RoleId).ToList(), ct);

        var before = await _userAccess.GetRoleAssignmentsAsync(user.Id, ct);
        await _userAccess.ReplaceRolesAsync(user.Id, roles, _clock.IndiaNow, _access.UserId, ct);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.UserRolesChanged, UserLogModules.Users, EntityName,
            user.Id.ToString(), $"Roles of user '{user.UserName}' changed.", oldValue: before, newValue: roles), ct);
        await _userAccess.SaveChangesAsync(ct);

        return await _userAccess.GetAccessAsync(user, ct);
    }

    public async Task<UserAccessResponse> UpdateRightsAsync(int userId, UpdateUserRightsRequest request, CancellationToken ct)
    {
        (await _rightsValidator.ValidateAsync(request, ct)).EnsureValid();

        var user = await _rules.RequireUserAsync(userId, ct);
        await _rules.EnsureNotSelfAsync(user, ct);
        await _rules.EnsureCanManageUserAsync(user, ct);

        var before = await _userAccess.GetRightAssignmentsAsync(user.Id, ct);
        var rights = await _rules.ValidateRightsAsync(user.CompanyId, request.Rights, before, ct);

        await _userAccess.ReplaceRightsAsync(user.Id, rights, _clock.IndiaNow, _access.UserId, ct);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.UserRightsChanged, UserLogModules.Users, EntityName,
            user.Id.ToString(), $"Custom rights of user '{user.UserName}' changed.", oldValue: before, newValue: rights), ct);
        await _userAccess.SaveChangesAsync(ct);

        return await _userAccess.GetAccessAsync(user, ct);
    }
}
