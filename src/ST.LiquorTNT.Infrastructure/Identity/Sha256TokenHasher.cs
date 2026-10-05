using System.Security.Cryptography;
using System.Text;
using ST.LiquorTNT.Business.Auth;

namespace ST.LiquorTNT.Infrastructure.Identity;

/// <summary>SHA-256 of the token, lower-case hex (64 chars). Deterministic so a lookup by hash works.</summary>
public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }
}
