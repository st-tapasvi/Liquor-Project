using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Users;

/// <summary>Reads password policies and the role → policy assignment. Implemented in Infrastructure.</summary>
public interface IPasswordPolicyRepository
{
    /// <summary>The active policy assigned to a role via ROLE_PASSWORD_POLICY, or null if none is assigned.</summary>
    Task<PASSWORD_POLICY?> GetForRoleAsync(int roleId, CancellationToken ct);

    Task<PASSWORD_POLICY?> GetByIdAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<PASSWORD_POLICY>> GetAllAsync(CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
