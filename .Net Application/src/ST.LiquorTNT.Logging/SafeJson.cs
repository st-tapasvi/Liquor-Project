using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ST.LiquorTNT.Logging;

/// <summary>
/// Turns a value (method input/output, request/response body) into JSON that is safe to log: secrets are
/// replaced by "***" and long text is cut. Passwords, answers, hashes and tokens never reach the log.
/// It never throws — a value it cannot make safe is described, not written.
/// </summary>
public static class SafeJson
{
    public const string Masked = "***";
    public const int DefaultMaxLength = 8000;

    /// <summary>
    /// A property is secret when its name, ignoring case and '_', ends with one of these
    /// (password, newPassword, confirm_password, passwordHash, accessToken, answerHash …).
    /// Names that only start with them (forcePasswordChange, passwordExpiresAt) stay readable.
    /// </summary>
    private static readonly string[] SecretSuffixes = { "password", "pwd", "pin", "otp", "token", "hash", "answer", "secret", "signingkey" };

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Serialises <paramref name="value"/> and masks secrets.</summary>
    public static string From(object? value, int maxLength = DefaultMaxLength)
    {
        if (value is null)
        {
            return "null";
        }

        try
        {
            var node = JsonSerializer.SerializeToNode(value, value.GetType(), Options);
            return Cut(Mask(node)?.ToJsonString() ?? "null", maxLength);
        }
        catch (Exception)
        {
            return $"<not serialisable: {value.GetType().Name}>";
        }
    }

    /// <summary>
    /// Masks secrets inside a raw JSON text (an HTTP body). Text that is not valid JSON is NOT written —
    /// a malformed login body would otherwise put the plain password in the log.
    /// </summary>
    public static string FromText(string? text, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        try
        {
            return Cut(Mask(JsonNode.Parse(text))?.ToJsonString() ?? string.Empty, maxLength);
        }
        catch (Exception)
        {
            return $"<body not logged: not valid JSON, {text.Length} chars>";
        }
    }

    public static bool IsSecretName(string name)
    {
        var normalised = name.Replace("_", string.Empty).Replace("-", string.Empty);
        return SecretSuffixes.Any(s => normalised.EndsWith(s, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Masks in place (a node already in the tree cannot be re-assigned) and returns the same node.</summary>
    private static JsonNode? Mask(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (name, child) in obj.ToList())
                {
                    if (child is not null && IsSecretName(name))
                    {
                        obj[name] = Masked;
                    }
                    else
                    {
                        Mask(child);
                    }
                }
                break;
            case JsonArray array:
                foreach (var child in array)
                {
                    Mask(child);
                }
                break;
        }

        return node;
    }

    private static string Cut(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength), "…(truncated)");
}
