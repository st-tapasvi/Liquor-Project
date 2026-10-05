using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>The caller's own sessions: list them and end one from another device.</summary>
public interface ISessionService
{
    Task<IReadOnlyList<SessionResponse>> GetMySessionsAsync(CancellationToken ct);

    Task<MessageResponse> RevokeMySessionAsync(int sessionId, CancellationToken ct);
}
