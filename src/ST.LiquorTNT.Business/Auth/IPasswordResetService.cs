using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>Forgot password in three anonymous steps: start (get the question) → verify (answer it) → reset (new password).</summary>
public interface IPasswordResetService
{
    Task<ForgotPasswordStartResponse> StartAsync(ForgotPasswordStartRequest request, CancellationToken ct);

    Task<MessageResponse> VerifyAsync(ForgotPasswordVerifyRequest request, CancellationToken ct);

    Task<MessageResponse> ResetAsync(ForgotPasswordResetRequest request, CancellationToken ct);
}
