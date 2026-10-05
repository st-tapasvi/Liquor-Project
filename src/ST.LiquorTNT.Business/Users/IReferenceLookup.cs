namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// Existence checks for the foreign keys a user carries, so an invalid role or company is refused
/// with a clear error instead of a database constraint failure. Implemented in Infrastructure.
/// </summary>
public interface IReferenceLookup
{
    Task<bool> RoleExistsAsync(int roleId, CancellationToken ct);

    Task<bool> CompanyExistsAsync(int companyId, CancellationToken ct);
}
