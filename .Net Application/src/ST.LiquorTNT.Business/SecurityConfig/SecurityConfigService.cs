using System.Globalization;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Contracts.SecurityConfig;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.SecurityConfig;

/// <summary>
/// Super-admin editing of SECURITY_CONFIG. A value is checked against the row's DATA_TYPE before it is
/// stored, so a typo cannot silently disable locking or lock on the first wrong password.
/// </summary>
public sealed class SecurityConfigService : ISecurityConfigService
{
    private readonly ISecurityConfigRepository _config;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IUserLogWriter _log;

    public SecurityConfigService(ISecurityConfigRepository config, IClock clock, ICurrentUser currentUser, IUserLogWriter log)
    {
        _config = config;
        _clock = clock;
        _currentUser = currentUser;
        _log = log;
    }

    public async Task<IReadOnlyList<SecurityConfigResponse>> GetAllAsync(CancellationToken ct)
    {
        var rows = await _config.GetAllAsync(ct);
        return rows.Select(ToResponse).ToList();
    }

    public async Task<SecurityConfigResponse> UpdateAsync(string key, UpdateSecurityConfigRequest request, CancellationToken ct)
    {
        var row = await _config.GetByKeyAsync(key.Trim().ToUpperInvariant(), ct) ?? throw new NotFoundException("Setting");
        var value = (request.Value ?? string.Empty).Trim();

        var error = Validate(row, value);
        if (error is not null)
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["value"] = new[] { error } });
        }

        if (AllowedValues.ContainsKey(row.ConfigKey))
        {
            value = value.ToUpperInvariant();       // "detail" is stored as DETAIL
        }

        var before = ToResponse(row);
        row.UpdateValue(value, _currentUser.UserId, _clock.IndiaNow);
        var after = ToResponse(row);

        // the old/new JSON carries the full values; the description stays within DESCRIPTION VARCHAR(500)
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.SecurityConfigChanged, UserLogModules.Security,
            "SECURITY_CONFIG", row.ConfigKey, $"'{row.ConfigKey}' changed.", oldValue: before, newValue: after), ct);
        await _config.SaveChangesAsync(ct);

        return after;
    }

    private const int MaxValueLength = 200;       // CONFIG_VALUE VARCHAR(200)

    /// <summary>STRING settings that accept only a fixed set of values.</summary>
    private static readonly Dictionary<string, string[]> AllowedValues = new(StringComparer.OrdinalIgnoreCase)
    {
        [SecuritySettings.Keys.SessionFullBehaviour] = new[] { "REJECT" },
        [SecuritySettings.Keys.LogMode] = new[] { SecuritySettings.LogModes.Normal, SecuritySettings.LogModes.Detail },
    };

    private static string? Validate(SECURITY_CONFIG row, string value)
    {
        if (value.Length == 0)
        {
            return "A value is required.";
        }

        if (value.Length > MaxValueLength)
        {
            return $"Must be at most {MaxValueLength} characters.";
        }

        return row.DataType.ToUpperInvariant() switch
        {
            "INT" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 1
                ? null
                : "Must be a whole number of at least 1.",
            "BOOL" => value.ToLowerInvariant() is "0" or "1" or "true" or "false"
                ? null
                : "Must be 1/0 or true/false.",
            _ => AllowedValues.TryGetValue(row.ConfigKey, out var allowed)
                 && !allowed.Contains(value, StringComparer.OrdinalIgnoreCase)
                ? $"Must be one of: {string.Join(", ", allowed)}."
                : null,
        };
    }

    private static SecurityConfigResponse ToResponse(SECURITY_CONFIG row) => new()
    {
        Key = row.ConfigKey,
        Value = row.ConfigValue,
        DataType = row.DataType,
        Description = row.Description,
        UpdatedAt = row.UpdatedAt,
    };
}
