namespace ST.LiquorTNT.Contracts.SecurityConfig;

public sealed class SecurityConfigResponse
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    /// <summary>INT | BOOL | STRING — how the admin UI should render and validate the value.</summary>
    public string DataType { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>IST.</summary>
    public DateTime UpdatedAt { get; set; }
}

public sealed class UpdateSecurityConfigRequest
{
    public string Value { get; set; } = string.Empty;
}
