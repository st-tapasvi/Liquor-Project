using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Users;

/// <summary>Reads password policies and the role → policy assignment. Implemented in Infrastructure.</summary>
public interface IPasswordPolicyRepository
{
    /// <summary>
    /// The active policy of each role in <paramref name="roleIds"/> (via ROLE_PASSWORD_POLICY), keyed by role id.
    /// A role without an assigned active policy is simply missing from the result.
    /// </summary>
    Task<IReadOnlyDictionary<int, PASSWORD_POLICY>> GetForRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken ct);

    /// <summary>The distinct role ids a user holds (any supplier code).</summary>
    Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken ct);

    Task<PASSWORD_POLICY?> GetByIdAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<PASSWORD_POLICY>> GetAllAsync(CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
