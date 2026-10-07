using FluentValidation;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ValidationException = ST.LiquorTNT.Business.Common.Exceptions.ValidationException;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Roles;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Domain.Rules;

namespace ST.LiquorTNT.Business.Roles;

/// <summary>
/// Master roles of a company and the rights inside each role.
/// <list type="bullet">
/// <item>A company user only ever sees and changes the roles of their own company.</item>
/// <item>Super Admin without a picked supplier code sees the roles of no company: Super Admin and the default templates.</item>
/// <item>Super Admin cannot be changed. Admin roles (Plant Admin) and ADMIN-scope rights need user.manageadmin.</item>
/// <item>SYSTEM-scope rights (CRM masters) are never put in a role: only Super Admin has them.</item>
/// </list>
/// Every change is audited in USER_LOG in the same commit.
/// </summary>
public sealed class RoleService : IRoleService
{
    private const string EntityName = "ROLES";

    private readonly IRoleRepository _roles;
    private readonly CurrentAccess _access;
    private readonly IClock _clock;
    private readonly IUserLogWriter _log;
    private readonly IValidator<SaveRoleRequest> _saveValidator;
    private readonly IValidator<UpdateRoleRightsRequest> _rightsValidator;

    public RoleService(
        IRoleRepository roles,
        CurrentAccess access,
        IClock clock,
        IUserLogWriter log,
        IValidator<SaveRoleRequest> saveValidator,
        IValidator<UpdateRoleRightsRequest> rightsValidator)
    {
        _roles = roles;
        _access = access;
        _clock = clock;
        _log = log;
        _saveValidator = saveValidator;
        _rightsValidator = rightsValidator;
    }

    public async Task<IReadOnlyList<RoleResponse>> GetListAsync(CancellationToken ct) =>
        await _roles.GetListAsync(await _access.CompanyScopeAsync(ct), ct);

    public async Task<RoleResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var role = await RequireRoleAsync(id, ct);
        return await ToResponseAsync(role, ct);
    }

    public async Task<RoleResponse> CreateAsync(SaveRoleRequest request, CancellationToken ct)
    {
        (await _saveValidator.ValidateAsync(request, ct)).EnsureValid();

        // A company user creates roles in their own company; Super Admin without a supplier code creates a template.
        var companyId = await _access.CompanyScopeAsync(ct);
        var name = request.RoleName.Trim();

        if (request.IsAdminRole)
        {
            await _access.EnsureCanManageAdminsAsync("Creating an admin role", ct);
        }

        await EnsureNameFreeAsync(companyId, name, excludeRoleId: null, ct);
        await EnsurePolicyExistsAsync(request.PasswordPolicyId, ct);

        var now = _clock.IndiaNow;
        var userId = _access.UserId;
        var role = ROLES.Create(companyId, name, request.Description, request.IsAdminRole, now, userId);

        // The policy link needs the generated role id, so the role is saved first (same pattern as user create).
        await _roles.AddAsync(role, ct);
        await _roles.SaveChangesAsync(ct);
        await _roles.AddPolicyLinkAsync(ROLE_PASSWORD_POLICY.Create(role.Id, request.PasswordPolicyId, now, userId), ct);

        var response = Map(role, request.PasswordPolicyId);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleCreated, UserLogModules.Roles, EntityName,
            role.Id.ToString(), $"Role '{role.RoleName}' created.", newValue: response), ct);
        await _roles.SaveChangesAsync(ct);

        return response;
    }

    public async Task<RoleResponse> UpdateAsync(int id, SaveRoleRequest request, CancellationToken ct)
    {
        (await _saveValidator.ValidateAsync(request, ct)).EnsureValid();

        var role = await RequireRoleAsync(id, ct);
        await EnsureEditableAsync(role, ct);

        // Making a role an admin role, or changing one, is an administrator's job.
        if (role.IsAdminRole || request.IsAdminRole)
        {
            await _access.EnsureCanManageAdminsAsync("Changing an admin role", ct);
        }

        var name = request.RoleName.Trim();
        await EnsureNameFreeAsync(role.CompanyId, name, role.Id, ct);
        await EnsurePolicyExistsAsync(request.PasswordPolicyId, ct);

        var before = await ToResponseAsync(role, ct);
        var now = _clock.IndiaNow;

        role.Update(name, request.Description, request.IsAdminRole, now, _access.UserId);

        var link = await _roles.GetPolicyLinkAsync(role.Id, ct);
        if (link is null)
        {
            await _roles.AddPolicyLinkAsync(ROLE_PASSWORD_POLICY.Create(role.Id, request.PasswordPolicyId, now, _access.UserId), ct);
        }
        else
        {
            link.ChangePolicy(request.PasswordPolicyId, now, _access.UserId);
        }

        var after = Map(role, request.PasswordPolicyId);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleUpdated, UserLogModules.Roles, EntityName,
            role.Id.ToString(), $"Role '{role.RoleName}' updated.", oldValue: before, newValue: after), ct);
        await _roles.SaveChangesAsync(ct);

        return after;
    }

    public async Task<MessageResponse> DeleteAsync(int id, CancellationToken ct)
    {
        var role = await RequireRoleAsync(id, ct);
        await EnsureEditableAsync(role, ct);

        if (role.IsAdminRole)
        {
            await _access.EnsureCanManageAdminsAsync("Deleting an admin role", ct);
        }

        // A role still held by users cannot disappear under them: take it away from the users first.
        if (await _roles.IsAssignedAsync(role.Id, ct))
        {
            throw new BusinessException(ErrorCodes.RoleInUse, "This role is still assigned to users.",
                $"Remove '{role.RoleName}' from every user before deleting it.");
        }

        await _roles.RemoveAsync(role, ct);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleDeleted, UserLogModules.Roles, EntityName,
            role.Id.ToString(), $"Role '{role.RoleName}' deleted."), ct);
        await _roles.SaveChangesAsync(ct);

        return MessageResponse.Of($"Role '{role.RoleName}' deleted.");
    }

    public async Task<RoleRightsResponse> GetRightsAsync(int id, CancellationToken ct)
    {
        var role = await RequireRoleAsync(id, ct);
        return await BuildRightsAsync(role, ct);
    }

    public async Task<RoleRightsResponse> UpdateRightsAsync(int id, UpdateRoleRightsRequest request, CancellationToken ct)
    {
        (await _rightsValidator.ValidateAsync(request, ct)).EnsureValid();

        var role = await RequireRoleAsync(id, ct);
        await EnsureEditableAsync(role, ct);

        if (role.IsAdminRole)
        {
            await _access.EnsureCanManageAdminsAsync("Changing the rights of an admin role", ct);
        }

        var wanted = request.PageActionIds.Distinct().ToList();
        var current = await _roles.GetRightIdsAsync(role.Id, ct);
        var added = wanted.Except(current).ToList();
        var removed = current.Except(wanted).ToList();

        // Only what changes is checked, so saving an unchanged grid never fails on rights the editor cannot grant.
        await EnsureGrantableAsync(added, removed, ct);

        await _roles.ReplaceRightsAsync(role.Id, wanted, _clock.IndiaNow, _access.UserId, ct);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleRightsChanged, UserLogModules.Roles, EntityName,
            role.Id.ToString(), $"Rights of role '{role.RoleName}' changed: {added.Count} added, {removed.Count} removed.",
            oldValue: current, newValue: wanted), ct);
        await _roles.SaveChangesAsync(ct);

        return await BuildRightsAsync(role, ct);
    }

    public Task<IReadOnlyList<PageResponse>> GetPagesAsync(CancellationToken ct) => _roles.GetPagesAsync(ct);

    /// <summary>
    /// SYSTEM rights are never handed out; ADMIN rights only by a holder of user.manageadmin.
    /// Unknown ids are a 400 so a stale screen cannot save garbage.
    /// </summary>
    private async Task EnsureGrantableAsync(IReadOnlyCollection<int> added, IReadOnlyCollection<int> removed, CancellationToken ct)
    {
        var changed = added.Concat(removed).ToList();
        if (changed.Count == 0)
        {
            return;
        }

        var actions = await _roles.GetPageActionsAsync(changed, ct);
        var unknown = changed.Except(actions.Select(a => a.Id)).ToList();
        if (unknown.Count > 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["pageActionIds"] = new[] { $"Unknown page action id(s): {string.Join(", ", unknown)}." },
            });
        }

        var system = actions.Where(a => a.GrantScope == GrantScope.SYSTEM && added.Contains(a.Id)).ToList();
        if (system.Count > 0)
        {
            throw new ForbiddenException(ErrorCodes.RightNotGrantable, "These rights cannot be given to a role.",
                $"{string.Join(", ", system.Select(a => a.PermissionKey))} belong to Super Admin only.");
        }

        var admin = actions.Where(a => a.GrantScope == GrantScope.ADMIN).ToList();
        if (admin.Count > 0)
        {
            await _access.EnsureCanManageAdminsAsync(
                $"Giving or removing {string.Join(", ", admin.Select(a => a.PermissionKey))}", ct);
        }
    }

    private async Task<RoleRightsResponse> BuildRightsAsync(ROLES role, CancellationToken ct)
    {
        var pages = await _roles.GetPagesAsync(ct);

        // Super Admin holds every right without rows, so its grid is shown fully ticked.
        var granted = role.IsSystem
            ? pages.SelectMany(p => p.Actions).Select(a => a.PageActionId).ToHashSet()
            : (await _roles.GetRightIdsAsync(role.Id, ct)).ToHashSet();

        foreach (var action in pages.SelectMany(p => p.Actions))
        {
            action.Granted = granted.Contains(action.PageActionId);
        }

        return new RoleRightsResponse { RoleId = role.Id, RoleName = role.RoleName, Pages = pages };
    }

    /// <summary>The role, if it exists AND the caller may see it (own company; Super Admin sees all). Otherwise 404.</summary>
    private async Task<ROLES> RequireRoleAsync(int id, CancellationToken ct)
    {
        var role = await _roles.GetByIdAsync(id, ct) ?? throw new NotFoundException("Role");

        if (!await _access.IsSuperAdminAsync(ct) && role.CompanyId != await _access.CompanyScopeAsync(ct))
        {
            throw new NotFoundException("Role");     // another company's role: do not even admit it exists
        }

        return role;
    }

    /// <summary>Super Admin's own role never changes; templates are changed by Super Admin only.</summary>
    private async Task EnsureEditableAsync(ROLES role, CancellationToken ct)
    {
        if (role.IsSystem)
        {
            throw new BusinessException(ErrorCodes.RoleNotEditable, "The Super Admin role cannot be changed.");
        }

        if (role.IsTemplate && !await _access.IsSuperAdminAsync(ct))
        {
            throw new BusinessException(ErrorCodes.RoleNotEditable, "Default role templates are changed by Super Admin only.");
        }
    }

    private async Task EnsureNameFreeAsync(int? companyId, string name, int? excludeRoleId, CancellationToken ct)
    {
        if (await _roles.NameExistsAsync(companyId, name, excludeRoleId, ct))
        {
            throw new BusinessException(ErrorCodes.RoleNameTaken, "A role with this name already exists.",
                $"Choose another name than '{name}'.");
        }
    }

    private async Task EnsurePolicyExistsAsync(int passwordPolicyId, CancellationToken ct)
    {
        if (!await _roles.PasswordPolicyExistsAsync(passwordPolicyId, ct))
        {
            throw new NotFoundException("Password policy");
        }
    }

    private async Task<RoleResponse> ToResponseAsync(ROLES role, CancellationToken ct) =>
        Map(role, (await _roles.GetPolicyLinkAsync(role.Id, ct))?.PasswordPolicyId);

    private static RoleResponse Map(ROLES role, int? passwordPolicyId) => new()
    {
        Id = role.Id,
        CompanyId = role.CompanyId,
        RoleName = role.RoleName,
        Description = role.Description,
        IsSystem = role.IsSystem,
        IsTemplate = role.IsTemplate,
        IsAdminRole = role.IsAdminRole,
        IsActive = role.IsActive,
        PasswordPolicyId = passwordPolicyId,
    };
}
