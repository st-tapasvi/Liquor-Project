using ST.LiquorTNT.Contracts.PasswordPolicies;

namespace ST.LiquorTNT.Business.PasswordPolicies;

public interface IPasswordPolicyService
{
    Task<IReadOnlyList<PasswordPolicyResponse>> GetAllAsync(CancellationToken ct);

    Task<PasswordPolicyResponse> UpdateAsync(int id, UpdatePasswordPolicyRequest request, CancellationToken ct);
}
