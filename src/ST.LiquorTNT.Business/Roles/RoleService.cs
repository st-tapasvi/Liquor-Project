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
/// <item>A role belongs to one supplier code of the company ("Operator RJ CL 772", own rights per supplier code) or is
/// company-level (Plant Admin, Agent Manager). The supplier code is chosen on create and never changes.</item>
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

    public async Task<IReadOnlyList<RoleResponse>> GetListAsync(int? supplierCodeId, CancellationToken ct) =>
        await _roles.GetListAsync(await _access.CompanyScopeAsync(ct), supplierCodeId, ct);

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

        // "Operator" for RJ CL 772: the supplier code must be one of the company's (active) supplier codes.
        var supplierCodeName = await CheckSupplierCodeAsync(companyId, request, ct);

        await EnsureNameFreeAsync(companyId, request.SupplierCodeId, name, excludeRoleId: null, ct);
        await EnsurePolicyExistsAsync(request.PasswordPolicyId, ct);

        var now = _clock.IndiaNow;
        var userId = _access.UserId;
        var role = ROLES.Create(companyId, name, request.Description, request.IsAdminRole, now, userId,
            request.SupplierCodeId, request.PerSupplierCode);

        // The policy link needs the generated role id, so the role is saved first (same pattern as user create).
        await _roles.AddAsync(role, ct);
        await _roles.SaveChangesAsync(ct);
        await _roles.AddPolicyLinkAsync(ROLE_PASSWORD_POLICY.Create(role.Id, request.PasswordPolicyId, now, userId), ct);

        var response = Map(role, request.PasswordPolicyId, supplierCodeName);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleCreated, UserLogModules.Roles, EntityName,
            role.Id.ToString(), $"Role '{response.DisplayName}' created.", newValue: response), ct);
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

        // The supplier code of a role is fixed: its users hold it for that supplier code.
        if (request.SupplierCodeId != role.SupplierCodeId)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["supplierCodeId"] = new[] { "The supplier code of a role cannot be changed. Create a new role for the other supplier code." },
            });
        }

        EnsureAdminRoleIsCompanyLevel(request);

        var name = request.RoleName.Trim();
        await EnsureNameFreeAsync(role.CompanyId, role.SupplierCodeId, name, role.Id, ct);
        await EnsurePolicyExistsAsync(request.PasswordPolicyId, ct);

        var before = await ToResponseAsync(role, ct);
        var now = _clock.IndiaNow;

        role.Update(name, request.Description, request.IsAdminRole, now, _access.UserId, request.PerSupplierCode);

        var link = await _roles.GetPolicyLinkAsync(role.Id, ct);
        if (link is null)
        {
            await _roles.AddPolicyLinkAsync(ROLE_PASSWORD_POLICY.Create(role.Id, request.PasswordPolicyId, now, _access.UserId), ct);
        }
        else
        {
            link.ChangePolicy(request.PasswordPolicyId, now, _access.UserId);
        }

        var after = Map(role, request.PasswordPolicyId, before.SupplierCodeName);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleUpdated, UserLogModules.Roles, EntityName,
            role.Id.ToString(), $"Role '{after.DisplayName}' updated.", oldValue: before, newValue: after), ct);
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

        var name = (await ToResponseAsync(role, ct)).DisplayName;

        // A role still held by users cannot disappear under them: take it away from the users first.
        if (await _roles.IsAssignedAsync(role.Id, ct))
        {
            throw new BusinessException(ErrorCodes.RoleInUse, "This role is still assigned to users.",
                $"Remove '{name}' from every user before deleting it.");
        }

        // The same for a role inside a role group: its users hold it through the group.
        if (await _roles.IsInRoleGroupAsync(role.Id, ct))
        {
            throw new BusinessException(ErrorCodes.RoleInUse, "This role is still inside a role group.",
                $"Remove '{name}' from every role group before deleting it.");
        }

        await _roles.RemoveAsync(role, ct);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleDeleted, UserLogModules.Roles, EntityName,
            role.Id.ToString(), $"Role '{name}' deleted."), ct);
        await _roles.SaveChangesAsync(ct);

        return MessageResponse.Of($"Role '{name}' deleted.");
    }

    public async Task<RoleRightsResponse> GetRightsAsync(int id, string? applicationType, CancellationToken ct)
    {
        var application = ParseApplication(applicationType);
        var role = await RequireRoleAsync(id, ct);
        return await BuildRightsAsync(role, application, ct);
    }

    public async Task<RoleRightsResponse> UpdateRightsAsync(int id, UpdateRoleRightsRequest request, CancellationToken ct)
    {
        (await _rightsValidator.ValidateAsync(request, ct)).EnsureValid();
        var application = ParseApplication(request.ApplicationType);

        var role = await RequireRoleAsync(id, ct);
        await EnsureEditableAsync(role, ct);

        if (role.IsAdminRole)
        {
            await _access.EnsureCanManageAdminsAsync("Changing the rights of an admin role", ct);
        }

        var wanted = request.PageActionIds.Distinct().ToList();
        var current = await _roles.GetRightIdsAsync(role.Id, ct);

        // A grid of one application (WEB or LINE) replaces only that application's rights; the other's stay as they are.
        if (application is not null)
        {
            var inApplication = (await _roles.GetPagesAsync(ct))
                .Where(p => p.ApplicationType == application.ToString())
                .SelectMany(p => p.Actions).Select(a => a.PageActionId).ToHashSet();

            var outside = wanted.Where(a => !inApplication.Contains(a)).ToList();
            if (outside.Count > 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["pageActionIds"] = new[] { $"Page action id(s) {string.Join(", ", outside)} are not part of the {application} application." },
                });
            }

            wanted = current.Where(a => !inApplication.Contains(a)).Concat(wanted).Distinct().ToList();
        }

        var added = wanted.Except(current).ToList();
        var removed = current.Except(wanted).ToList();

        // Only what changes is checked, so saving an unchanged grid never fails on rights the editor cannot grant.
        await EnsureGrantableAsync(added, removed, ct);

        await _roles.ReplaceRightsAsync(role.Id, wanted, _clock.IndiaNow, _access.UserId, ct);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleRightsChanged, UserLogModules.Roles, EntityName,
            role.Id.ToString(), $"Rights of role '{role.RoleName}' changed: {added.Count} added, {removed.Count} removed.",
            oldValue: current, newValue: wanted), ct);
        await _roles.SaveChangesAsync(ct);

        return await BuildRightsAsync(role, application, ct);
    }

    public async Task<IReadOnlyList<PageResponse>> GetPagesAsync(string? applicationType, CancellationToken ct) =>
        OfApplication(await _roles.GetPagesAsync(ct), ParseApplication(applicationType));

    /// <summary>"WEB" / "LINE" (any case) → the application; empty → null (both). Anything else is a 400.</summary>
    private static ApplicationType? ParseApplication(string? applicationType)
    {
        if (string.IsNullOrWhiteSpace(applicationType))
        {
            return null;
        }

        if (Enum.TryParse<ApplicationType>(applicationType.Trim(), ignoreCase: true, out var application)
            && Enum.IsDefined(application))
        {
            return application;
        }

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["applicationType"] = new[] { "Must be WEB or LINE (or empty for both)." },
        });
    }

    private static IReadOnlyList<PageResponse> OfApplication(IReadOnlyList<PageResponse> pages, ApplicationType? application) =>
        application is null ? pages : pages.Where(p => p.ApplicationType == application.ToString()).ToList();

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
                $"{string.Join(", ", system.Select(a => a.PermissionKey))} belong to the Admin role only.");
        }

        var admin = actions.Where(a => a.GrantScope == GrantScope.ADMIN).ToList();
        if (admin.Count > 0)
        {
            await _access.EnsureCanManageAdminsAsync(
                $"Giving or removing {string.Join(", ", admin.Select(a => a.PermissionKey))}", ct);
        }
    }

    private async Task<RoleRightsResponse> BuildRightsAsync(ROLES role, ApplicationType? application, CancellationToken ct)
    {
        var pages = OfApplication(await _roles.GetPagesAsync(ct), application);

        // Super Admin holds every right without rows, so its grid is shown fully ticked.
        var granted = role.IsSystem
            ? pages.SelectMany(p => p.Actions).Select(a => a.PageActionId).ToHashSet()
            : (await _roles.GetRightIdsAsync(role.Id, ct)).ToHashSet();

        foreach (var action in pages.SelectMany(p => p.Actions))
        {
            action.Granted = granted.Contains(action.PageActionId);
        }

        var displayName = (await ToResponseAsync(role, ct)).DisplayName;
        return new RoleRightsResponse { RoleId = role.Id, RoleName = role.RoleName, DisplayName = displayName, Pages = pages };
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
            throw new BusinessException(ErrorCodes.RoleNotEditable, "The Admin role cannot be changed.");
        }

        if (role.IsTemplate && !await _access.IsSuperAdminAsync(ct))
        {
            throw new BusinessException(ErrorCodes.RoleNotEditable, "Default role templates are changed by the Admin role only.");
        }
    }

    /// <summary>"Operator" may exist once per supplier code (and once among the company-level roles).</summary>
    private async Task EnsureNameFreeAsync(int? companyId, int? supplierCodeId, string name, int? excludeRoleId, CancellationToken ct)
    {
        if (await _roles.NameExistsAsync(companyId, supplierCodeId, name, excludeRoleId, ct))
        {
            throw new BusinessException(ErrorCodes.RoleNameTaken, "A role with this name already exists.",
                supplierCodeId is null
                    ? $"Choose another name than '{name}'."
                    : $"This supplier code already has a role '{name}'. Choose another name.");
        }
    }

    /// <summary>
    /// A new role's supplier code: none for a template, otherwise an active supplier code of the caller's company
    /// (another company's is "not found"). Returns its name ("RJ CL 772"), or null for a company-level role.
    /// </summary>
    private async Task<string?> CheckSupplierCodeAsync(int? companyId, SaveRoleRequest request, CancellationToken ct)
    {
        EnsureAdminRoleIsCompanyLevel(request);

        if (request.SupplierCodeId is not int supplierCodeId)
        {
            return null;
        }

        if (companyId is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["supplierCodeId"] = new[] { "A default template has no supplier code. Use perSupplierCode to copy it for every supplier code." },
            });
        }

        var supplierCode = await _roles.GetSupplierCodeAsync(supplierCodeId, ct);
        if (supplierCode is null || supplierCode.CompanyId != companyId)
        {
            throw new NotFoundException("Supplier code");
        }

        return supplierCode.DisplayName;
    }

    /// <summary>An admin role (Plant Admin) runs the whole company, so it is never tied to one supplier code.</summary>
    private static void EnsureAdminRoleIsCompanyLevel(SaveRoleRequest request)
    {
        if (request.IsAdminRole && request.SupplierCodeId is not null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["isAdminRole"] = new[] { "An admin role covers the whole company; leave supplierCodeId empty." },
            });
        }
    }

    private async Task EnsurePolicyExistsAsync(int passwordPolicyId, CancellationToken ct)
    {
        if (!await _roles.PasswordPolicyExistsAsync(passwordPolicyId, ct))
        {
            throw new NotFoundException("Password policy");
        }
    }

    /// <summary>The role as stored now (read from the database, so it is the "before" picture while editing).</summary>
    private async Task<RoleResponse> ToResponseAsync(ROLES role, CancellationToken ct) =>
        await _roles.GetResponseAsync(role.Id, ct) ?? throw new NotFoundException("Role");

    private static RoleResponse Map(ROLES role, int? passwordPolicyId, string? supplierCodeName) => new()
    {
        Id = role.Id,
        CompanyId = role.CompanyId,
        SupplierCodeId = role.SupplierCodeId,
        SupplierCodeName = supplierCodeName,
        RoleName = role.RoleName,
        DisplayName = RoleNames.Display(role.RoleName, supplierCodeName),
        Description = role.Description,
        IsSystem = role.IsSystem,
        IsTemplate = role.IsTemplate,
        PerSupplierCode = role.PerSupplierCode,
        IsAdminRole = role.IsAdminRole,
        IsActive = role.IsActive,
        PasswordPolicyId = passwordPolicyId,
    };
}
