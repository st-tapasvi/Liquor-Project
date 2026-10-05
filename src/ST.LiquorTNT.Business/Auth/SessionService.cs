using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;

namespace ST.LiquorTNT.Business.Auth;

public sealed class SessionService : ISessionService
{
    private readonly ISessionRepository _sessions;
    private readonly ITokenHasher _tokenHasher;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _request;
    private readonly IUserLogWriter _log;

    public SessionService(
        ISessionRepository sessions,
        ITokenHasher tokenHasher,
        IClock clock,
        ICurrentUser currentUser,
        IRequestContext request,
        IUserLogWriter log)
    {
        _sessions = sessions;
        _tokenHasher = tokenHasher;
        _clock = clock;
        _currentUser = currentUser;
        _request = request;
        _log = log;
    }

    public async Task<IReadOnlyList<SessionResponse>> GetMySessionsAsync(CancellationToken ct)
    {
        var userId = RequireUserId();
        var currentHash = _request.AccessToken is null ? null : _tokenHasher.Hash(_request.AccessToken);

        var sessions = await _sessions.GetActiveForUserAsync(userId, _clock.IndiaNow, ct);

        return sessions.Select(s => new SessionResponse
        {
            Id = s.Id,
            LoginAt = s.LoginAt,
            LastActivityAt = s.LastActivityAt,
            ExpiresAt = s.ExpiresAt,
            IpAddress = s.IpAddress,
            UserAgent = s.UserAgent,
            IsCurrent = s.SessionTokenHash == currentHash,
        }).ToList();
    }

    public async Task<MessageResponse> RevokeMySessionAsync(int sessionId, CancellationToken ct)
    {
        var userId = RequireUserId();
        var now = _clock.IndiaNow;

        // Scoped to the caller: a user can only end their own sessions.
        var session = await _sessions.GetByIdForUserAsync(sessionId, userId, ct) ?? throw new NotFoundException("Session");

        if (!session.IsActiveAt(now))
        {
            return MessageResponse.Of($"Session {sessionId} had already ended.");
        }

        session.Revoke(now);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.SessionRevoked, UserLogModules.Auth,
            "USER_SESSION", session.Id.ToString(), "Session revoked by the user."), ct);
        await _sessions.SaveChangesAsync(ct);

        return MessageResponse.Of($"Session {sessionId} has been logged out.");
    }

    private int RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedException(ErrorCodes.SessionInvalid, "Not authenticated.");
}
