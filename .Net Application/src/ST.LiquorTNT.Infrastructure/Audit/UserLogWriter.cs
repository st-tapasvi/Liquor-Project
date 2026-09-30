using System.Text.Json;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Infrastructure.Database;

namespace ST.LiquorTNT.Infrastructure.Audit;

/// <summary>
/// Stages a USER_LOG row on the scoped DbContext; the caller's SaveChanges commits it together with
/// the business change. Request facts (IP, user agent, correlation id) and the actor come from the
/// current request; the business caller only describes what happened. Values are stored as JSON.
/// </summary>
public sealed class UserLogWriter : IUserLogWriter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _request;

    public UserLogWriter(AppDbContext db, IClock clock, ICurrentUser currentUser, IRequestContext request)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _request = request;
    }

    public Task WriteAsync(UserLogEntry entry, CancellationToken ct)
    {
        var row = USER_LOG.Create(
            entry.ActorUserId ?? _currentUser.UserId,
            entry.ActionType,
            entry.ModuleName,
            entry.EntityName,
            entry.EntityId,
            entry.ActionStatus,
            entry.Description,
            _clock.IndiaNow,
            _request.IpAddress,
            _request.UserAgent,
            _request.CorrelationId,
            Serialize(entry.OldValue),
            Serialize(entry.NewValue));

        _db.USER_LOG.Add(row);
        return Task.CompletedTask;
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, Json);
}
