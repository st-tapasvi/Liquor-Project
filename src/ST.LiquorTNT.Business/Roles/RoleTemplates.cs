using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Roles;

/// <summary>
/// Gives a new company its default roles: every template (Plant Admin, Agent Manager, Plant Manager, Supervisor,
/// Operator, Viewer) is copied with its rights and its password policy. The company then owns the copies and may
/// change or delete them; later edits of a template never reach companies that already have their copies.
/// <para>
/// Called when a company gets its first supplier code (there is no separate "create company" screen yet).
/// A company that already has roles is left alone, so calling this twice is harmless.
/// </para>
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

    /// <summary>Copies the templates into <paramref name="companyId"/>; returns how many roles were created (0 if it had roles).</summary>
    public async Task<int> CopyIntoCompanyAsync(int companyId, CancellationToken ct)
    {
        if (await _roles.CompanyHasRolesAsync(companyId, ct))
        {
            return 0;
        }

        var now = _clock.IndiaNow;
        var templates = await _roles.GetTemplatesAsync(ct);
        var copies = new List<(ROLES Template, ROLES Copy)>();

        foreach (var template in templates)
        {
            var copy = ROLES.CopyOf(template, companyId, now, _user.UserId);
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

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.RoleTemplatesCopied, UserLogModules.Roles, "COMPANY",
            companyId.ToString(), $"{copies.Count} default role(s) copied into the company."), ct);
        await _roles.SaveChangesAsync(ct);

        return copies.Count;
    }
}
