using System.Security.Cryptography;
using System.Text;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>
/// Security answers are compared after normalisation so casing, whitespace (spaces, tabs, non-breaking spaces — all
/// of it, also between words) and Unicode composition do not lock a user out of their own recovery:
/// "New Delhi" == "newdelhi" == " NEW  DELHI "; Devanagari typed two ways compares equal. Punctuation still counts
/// ("St. Mary's" != "St Marys"), and so does spelling. The hash is made of the normalised text, so changing this rule
/// makes answers saved before it unmatchable (owner decision 2026-10-08: ignore all spaces).
/// </summary>
public static class SecurityAnswers
{
    public static string Normalize(string answer)
    {
        var text = answer.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        return string.Concat(text.Where(c => !char.IsWhiteSpace(c)));
    }
}

/// <summary>Unguessable handle for a reset request. Only its hash is stored (see ITokenHasher).</summary>
public static class ResetTokens
{
    public static string New() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');       // URL-safe
}
