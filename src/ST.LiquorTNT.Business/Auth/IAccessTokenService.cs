using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>Issues the bearer token for a login. Implemented in Infrastructure (JWT).</summary>
public interface IAccessTokenService
{
    string Create(USERS user, DateTime expiresAtUtc);
}
