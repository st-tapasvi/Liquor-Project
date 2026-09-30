namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for the <c>SECURITY_CONFIG</c> table: one global security setting stored as key/value.
/// Class name mirrors the table name (project rule: entity name = DB table name).
/// Only an administrator edits it; the typed view the rest of the app uses is <c>SecuritySettings</c>.
/// </summary>
public class SECURITY_CONFIG
{
    private SECURITY_CONFIG()
    {
        ConfigKey = string.Empty;
        ConfigValue = string.Empty;
        DataType = string.Empty;
    }

    public int Id { get; private set; }
    public string ConfigKey { get; private set; }
    public string ConfigValue { get; private set; }
    public string DataType { get; private set; }      // INT | BOOL | STRING (tells the admin UI and the validator how to treat VALUE)
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void UpdateValue(string value, int? updatedBy, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", nameof(value));
        }

        ConfigValue = value.Trim();
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }
}
