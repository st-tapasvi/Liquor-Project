namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>USER_LOG</c>: one audit row — WHO did WHAT, WHEN, FROM WHERE, on WHICH record.
/// Append-only: rows are never edited or deleted (retention/archival only).
/// Never contains a password, hash, answer, token or other secret; callers pass plain summaries.
/// </summary>
public class USER_LOG
{
    private USER_LOG()
    {
        ActionType = string.Empty;
        ModuleName = string.Empty;
        ActionStatus = string.Empty;
    }

    public int Id { get; private set; }
    public int? UserId { get; private set; }          // who performed the action (null = unknown caller)
    public string ActionType { get; private set; }    // e.g. LOGIN_SUCCESS, USER_CREATED
    public string ModuleName { get; private set; }
    public string? EntityName { get; private set; }
    public string? EntityId { get; private set; }
    public string ActionStatus { get; private set; }  // SUCCESS | FAILED
    public string? Description { get; private set; }
    public DateTime DateTime { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? OldValue { get; private set; }     // JSON
    public string? NewValue { get; private set; }     // JSON

    public static USER_LOG Create(
        int? userId,
        string actionType,
        string moduleName,
        string? entityName,
        string? entityId,
        string actionStatus,
        string? description,
        DateTime now,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        string? oldValueJson,
        string? newValueJson)
    {
        if (string.IsNullOrWhiteSpace(actionType))
        {
            throw new ArgumentException("Action type is required.", nameof(actionType));
        }

        return new USER_LOG
        {
            UserId = userId,
            ActionType = actionType,
            ModuleName = moduleName,
            EntityName = entityName,
            EntityId = entityId,
            ActionStatus = actionStatus,
            Description = description,
            DateTime = now,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            OldValue = oldValueJson,
            NewValue = newValueJson,
        };
    }
}
