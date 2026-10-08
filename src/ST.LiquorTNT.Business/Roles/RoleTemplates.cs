using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Roles;

/// <summary>
/// Gives a new supplier code its default roles, copied from the templates with their rights and password policy:
/// <list type="bullet">
/// <item>Per-supplier-code templates (Plant Manager, Supervisor, Operator, Viewer) are copied for EVERY new supplier
/// code, e.g. "Operator RJ CL 772", "Operator RJ IMFL 1028".</item>
/// <item>Company-level templates (Plant Admin, Agent Manager) are copied once, with the company's first supplier code.</item>
/// </list>
/// The company then owns the copies and may change or delete them; later edits of a template never reach copies
/// that already exist. A company or supplier code that already has its roles is left alone, so calling this twice
/// is harmless.
/// </summary>
public sealed class RoleTemplates
{
    private readonly IRoleRepository _roles;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IUserLogWriter _log;

    public RoleTemplates(IRoleRepository roles, IClock clock, ICurrentUser user, IUserLogWriter log)
    {
        _roles = roles;
        _clock = clock;
        _user = user;
        _log = log;
    }

    /// <summary>Copies the templates for a new supplier code of <paramref name="companyId"/>; returns how many roles were created.</summary>
    public async Task<int> CopyForSupplierCodeAsync(int companyId, int supplierCodeId, CancellationToken ct)
    {
        var templates = await _roles.GetTemplatesAsync(ct);
        var wanted = new List<(ROLES Template, int? SupplierCodeId)>();

        if (!await _roles.CompanyHasCompanyRolesAsync(companyId, ct))
        {
            wanted.AddRange(templates.Where(t => !t.PerSupplierCode).Select(t => (t, (int?)null)));
        }

        if (!await _roles.SupplierCodeHasRolesAsync(supplierCodeId, ct))
        {
            wanted.AddRange(templates.Where(t => t.PerSupplierCode).Select(t => (t, (int?)supplierCodeId)));
        }

        if (wanted.Count == 0)
        {
            return 0;
        }

        var now = _clock.IndiaNow;
        var copies = new List<(ROLES Template, ROLES Copy)>();

        foreach (var (template, forSupplierCode) in wanted)
        {
            var copy = ROLES.CopyOf(template, companyId, now, _user.UserId, forSupplierCode);
            await _roles.AddAsync(copy, ct);
            copies.Add((template, copy));
        }

        // The rights and policy links need the new role ids.
        await _roles.SaveChangesAsync(ct);

        foreach (var (template, copy) in copies)
        {
            var rights = await _roles.GetRightIdsAsync(template.Id, ct);
            await _roles.ReplaceRightsAsync(copy.Id, rights, now, _user.UserId, ct);

            if (await _roles.GetPolicyLinkAsync(template.Id, ct) is { } link)
            {
                await _roles.AddPolicyLinkAsync(ROLE_PASSWORD_POLICY.Create(copy.Id, link.PasswordPolicyId, now, _user.UserId), ct);
            }
        }

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleTemplatesCopied, UserLogModules.Roles, "SUPPLIER_CODE",
            supplierCodeId.ToString(), $"{copies.Count} default role(s) copied for the new supplier code."), ct);
        await _roles.SaveChangesAsync(ct);

        return copies.Count;
    }
}
