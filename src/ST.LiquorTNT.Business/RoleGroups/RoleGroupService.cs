using FluentValidation;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Users;
using ValidationException = ST.LiquorTNT.Business.Common.Exceptions.ValidationException;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.RoleGroups;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.RoleGroups;

/// <summary>
/// Role groups: named bundles of master roles of one company ("All Operators" = Operator RJ CL 772 + Operator RJ IMFL
/// 1028 …). A user given a group holds all its roles; changing the group changes it for all its users at once, e.g. a
/// new supplier code's Operator role is ticked in the group once instead of being given to 50 users one by one.
/// <list type="bullet">
/// <item>A group holds roles of its own company only (no templates, no Admin role, no other groups, no rights).</item>
/// <item>A group with an admin role (Plant Admin), or held by an admin user, is changed only with user.manageadmin.</item>
/// <item>Nobody but Admin changes a group they hold themselves (that would raise their own rights).</item>
/// <item>A group held by any user cannot be deleted (409 ROLE_GROUP_IN_USE).</item>
/// <item>A new supplier code's roles are not added to groups automatically; the administrator ticks them.</item>
/// </list>
/// Every change is audited in USER_LOG in the same commit.
/// </summary>
public sealed class RoleGroupService : IRoleGroupService
{
    private const string EntityName = "ROLE_GROUP";

    private readonly IRoleGroupRepository _groups;
    private readonly CurrentAccess _access;
    private readonly PasswordRules _passwordRules;
    private readonly IClock _clock;
    private readonly IUserLogWriter _log;
    private readonly IValidator<SaveRoleGroupRequest> _saveValidator;
    private readonly IValidator<UpdateRoleGroupRolesRequest> _rolesValidator;

    public RoleGroupService(
        IRoleGroupRepository groups,
        CurrentAccess access,
        PasswordRules passwordRules,
        IClock clock,
        IUserLogWriter log,
        IValidator<SaveRoleGroupRequest> saveValidator,
        IValidator<UpdateRoleGroupRolesRequest> rolesValidator)
    {
        _groups = groups;
        _access = access;
        _passwordRules = passwordRules;
        _clock = clock;
        _log = log;
        _saveValidator = saveValidator;
        _rolesValidator = rolesValidator;
    }

    public async Task<IReadOnlyList<RoleGroupResponse>> GetListAsync(CancellationToken ct)
    {
        // Admin without a picked supplier code works in no company, and groups always belong to one.
        var companyId = await _access.CompanyScopeAsync(ct);
        return companyId is int id ? await _groups.GetListAsync(id, ct) : Array.Empty<RoleGroupResponse>();
    }

    public async Task<RoleGroupResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var group = await RequireGroupAsync(id, ct);
        return await ToResponseAsync(group, ct);
    }

    public async Task<RoleGroupResponse> CreateAsync(SaveRoleGroupRequest request, CancellationToken ct)
    {
        (await _saveValidator.ValidateAsync(request, ct)).EnsureValid();

        var companyId = await _access.CompanyScopeAsync(ct) ?? throw new ValidationException(new Dictionary<string, string[]>
        {
            ["groupName"] = new[] { "A role group belongs to a company: pick a supplier code of that company first." },
        });

        var name = request.GroupName.Trim();
        await EnsureNameFreeAsync(companyId, name, excludeGroupId: null, ct);

        var group = ROLE_GROUP.Create(companyId, name, request.Description, _clock.IndiaNow, _access.UserId);
        await _groups.AddAsync(group, ct);
        await _groups.SaveChangesAsync(ct);

        var response = await ToResponseAsync(group, ct);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleGroupCreated, UserLogModules.Roles, EntityName,
            group.Id.ToString(), $"Role group '{group.GroupName}' created.", newValue: response), ct);
        await _groups.SaveChangesAsync(ct);

        return response;
    }

    public async Task<RoleGroupResponse> UpdateAsync(int id, SaveRoleGroupRequest request, CancellationToken ct)
    {
        (await _saveValidator.ValidateAsync(request, ct)).EnsureValid();

        var group = await RequireGroupAsync(id, ct);
        var before = await ToResponseAsync(group, ct);
        await EnsureMayChangeAsync(group, before.HasAdminRole, ct);

        var name = request.GroupName.Trim();
        await EnsureNameFreeAsync(group.CompanyId, name, group.Id, ct);

        group.Update(name, request.Description, _clock.IndiaNow, _access.UserId);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleGroupUpdated, UserLogModules.Roles, EntityName,
            group.Id.ToString(), $"Role group '{group.GroupName}' updated.",
            oldValue: new { before.GroupName, before.Description }, newValue: new { group.GroupName, group.Description }), ct);
        await _groups.SaveChangesAsync(ct);

        return await ToResponseAsync(group, ct);
    }

    public async Task<MessageResponse> DeleteAsync(int id, CancellationToken ct)
    {
        var group = await RequireGroupAsync(id, ct);
        var current = await ToResponseAsync(group, ct);
        await EnsureMayChangeAsync(group, current.HasAdminRole, ct);

        // A group still held by users cannot disappear under them: take it away from the users first.
        if (await _groups.IsAssignedAsync(group.Id, ct))
        {
            throw new BusinessException(ErrorCodes.RoleGroupInUse, "This role group is still given to users.",
                $"Remove '{group.GroupName}' from every user ({current.UserCount}) before deleting it.");
        }

        await _groups.RemoveAsync(group, ct);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleGroupDeleted, UserLogModules.Roles, EntityName,
            group.Id.ToString(), $"Role group '{group.GroupName}' deleted.", oldValue: current), ct);
        await _groups.SaveChangesAsync(ct);

        return MessageResponse.Of($"Role group '{group.GroupName}' deleted.");
    }

    public async Task<RoleGroupResponse> UpdateRolesAsync(int id, UpdateRoleGroupRolesRequest request, CancellationToken ct)
    {
        (await _rolesValidator.ValidateAsync(request, ct)).EnsureValid();

        var group = await RequireGroupAsync(id, ct);
        var wanted = request.RoleIds.Distinct().ToList();
        var roles = await RequireRolesOfCompanyAsync(group.CompanyId, wanted, ct);

        // Admin protection looks at the group before AND after: putting in or taking out Plant Admin both count.
        var before = await ToResponseAsync(group, ct);
        await EnsureMayChangeAsync(group, before.HasAdminRole || roles.Any(r => r.IsAdminRole), ct);

        // The group's users must keep a password policy they can follow.
        if (wanted.Count > 0)
        {
            await _passwordRules.RequirePolicyAsync(wanted, ct);
        }

        var current = before.Roles.Select(r => r.RoleId).ToList();
        await _groups.ReplaceRolesAsync(group.Id, wanted, _clock.IndiaNow, _access.UserId, ct);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleGroupRolesChanged, UserLogModules.Roles, EntityName,
            group.Id.ToString(),
            $"Roles of role group '{group.GroupName}' changed: {wanted.Except(current).Count()} added, " +
            $"{current.Except(wanted).Count()} removed; applies to {before.UserCount} user(s).",
            oldValue: current, newValue: wanted), ct);
        await _groups.SaveChangesAsync(ct);

        return await ToResponseAsync(group, ct);
    }

    /// <summary>The group, if it exists AND is in the caller's company (Admin sees all). Otherwise 404.</summary>
    private async Task<ROLE_GROUP> RequireGroupAsync(int id, CancellationToken ct)
    {
        var group = await _groups.GetByIdAsync(id, ct) ?? throw new NotFoundException("Role group");

        if (!await _access.IsSuperAdminAsync(ct) && group.CompanyId != await _access.CompanyScopeAsync(ct))
        {
            throw new NotFoundException("Role group");     // another company's group: do not even admit it exists
        }

        return group;
    }

    /// <summary>
    /// Changing a group changes the access of everyone holding it, so: nobody (but Admin) changes a group they hold,
    /// and a group with an admin role, or held by an admin user, needs user.manageadmin.
    /// </summary>
    private async Task EnsureMayChangeAsync(ROLE_GROUP group, bool hasAdminRole, CancellationToken ct)
    {
        if (!await _access.IsSuperAdminAsync(ct) && await _groups.IsMemberAsync(group.Id, _access.UserId, ct))
        {
            throw new BusinessException(ErrorCodes.CannotChangeOwnAccess, "You cannot change a role group you hold yourself.",
                "Ask another administrator to do it.");
        }

        if (hasAdminRole || await _groups.HasAdminMemberAsync(group.Id, ct))
        {
            await _access.EnsureCanManageAdminsAsync($"Changing the role group '{group.GroupName}'", ct);
        }
    }

    /// <summary>Every role must be an active, non-template role of the group's company; anything else is 404.</summary>
    private async Task<IReadOnlyList<ROLES>> RequireRolesOfCompanyAsync(int companyId, IReadOnlyCollection<int> roleIds, CancellationToken ct)
    {
        if (roleIds.Count == 0)
        {
            return Array.Empty<ROLES>();
        }

        var roles = (await _groups.GetRolesAsync(roleIds, ct)).ToDictionary(r => r.Id);
        var wrong = roleIds.Where(id => !roles.TryGetValue(id, out var role)
                                        || role.CompanyId != companyId || role.IsTemplate || role.IsSystem || !role.IsActive)
                           .ToList();

        if (wrong.Count > 0)
        {
            throw new NotFoundException($"Role {string.Join(", ", wrong)}");
        }

        return roles.Values.ToList();
    }

    private async Task EnsureNameFreeAsync(int companyId, string name, int? excludeGroupId, CancellationToken ct)
    {
        if (await _groups.NameExistsAsync(companyId, name, excludeGroupId, ct))
        {
            throw new BusinessException(ErrorCodes.RoleGroupNameTaken, "A role group with this name already exists.",
                $"Choose another name than '{name}'.");
        }
    }

    private async Task<RoleGroupResponse> ToResponseAsync(ROLE_GROUP group, CancellationToken ct) =>
        await _groups.GetResponseAsync(group.Id, ct) ?? throw new NotFoundException("Role group");
}
