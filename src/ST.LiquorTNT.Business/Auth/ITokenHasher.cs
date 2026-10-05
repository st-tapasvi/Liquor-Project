namespace ST.LiquorTNT.Business.Auth;

/// <summary>
/// One-way hash for session tokens so the raw token is never stored. Tokens are long random
/// strings, so a fast hash (SHA-256) is correct here — unlike passwords, which use PBKDF2.
/// </summary>
public interface ITokenHasher
{
    string Hash(string token);
}
