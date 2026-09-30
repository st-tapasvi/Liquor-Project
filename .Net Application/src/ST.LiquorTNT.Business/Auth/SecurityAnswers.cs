using System.Security.Cryptography;
using System.Text;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>
/// Security answers are compared after normalisation so casing, stray whitespace (spaces, tabs,
/// non-breaking spaces) and Unicode composition do not lock a user out of their own recovery
/// ("New Delhi" == " new  delhi "; Devanagari typed two ways compares equal).
/// </summary>
public static class SecurityAnswers
{
    public static string Normalize(string answer)
    {
        var words = answer.Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);   // null = every Unicode white-space char

        return string.Join(' ', words);
    }
}

/// <summary>Unguessable handle for a reset request. Only its hash is stored (see ITokenHasher).</summary>
public static class ResetTokens
{
    public static string New() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');       // URL-safe
}
