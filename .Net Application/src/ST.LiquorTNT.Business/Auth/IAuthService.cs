using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;

namespace ST.LiquorTNT.Business.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);

    Task<MessageResponse> LogoutAsync(CancellationToken ct);

    Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken ct);

    /// <summary>Password-verified, so it works before login too (forced change / expired password).</summary>
    Task<MessageResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct);
}
