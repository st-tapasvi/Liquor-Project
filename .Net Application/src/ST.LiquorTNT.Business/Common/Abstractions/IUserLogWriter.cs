namespace ST.LiquorTNT.Business.Common.Abstractions;

/// <summary>
/// Adds one USER_LOG row to the current unit of work. It does NOT commit: the caller's next
/// SaveChanges writes the audit row and the change it describes in the same transaction, so a
/// change can never be committed without its audit trail (or the reverse).
/// Used by Users, Auth and Security, so it is cross-cutting. Declared in Business, implemented in Infrastructure.
/// </summary>
public interface IUserLogWriter
{
    Task WriteAsync(UserLogEntry entry, CancellationToken ct);
}
