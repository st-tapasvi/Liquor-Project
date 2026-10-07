using System.Reflection;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Tests.Fakes;

/// <summary>Sets the database-generated Id on an entity so fakes can behave like a saved row.</summary>
internal static class EntityIds
{
    public static T WithId<T>(this T entity, int id)
    {
        typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!.SetValue(entity, id);
        return entity;
    }
}

internal static class TestData
{
    public static readonly DateTime Now = new(2026, 9, 28, 10, 0, 0);

    public static PASSWORD_POLICY Policy(
        int id = 1,
        string name = "TEST",
        int minLength = 8,
        int maxLength = 64,
        bool upper = false,
        bool lower = false,
        bool number = false,
        bool special = false,
        int historyCount = 5,
        bool expiryEnabled = false,
        int? expiryDays = null,
        bool allowUserName = false,
        bool allowCommon = false)
        => PASSWORD_POLICY.Create(name, minLength, maxLength, upper, lower, number, special, historyCount,
            expiryEnabled, expiryDays, allowUserName, allowCommon, Now, createdBy: null).WithId(id);

    public static USERS User(
        int id = 10,
        string userName = "alice",
        string passwordHash = "H:Secret@1",
        int? companyId = null,
        bool forceChange = false,
        DateTime? expiresAt = null)
        => USERS.Create(userName, passwordHash, companyId, "Alice", null, null, null,
            forceChange, expiresAt, Now, createdBy: 1).WithId(id);
}

/// <summary>Deterministic: Hash("x") == "H:x", so tests can assert on stored hashes and history.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "H:" + password;

    public bool Verify(string password, string storedHash) => storedHash == "H:" + password;
}

/// <summary>A clock the test moves by hand.</summary>
internal sealed class FixedClock : IClock
{
    private static readonly TimeSpan IstOffset = TimeSpan.FromMinutes(330);

    public FixedClock(DateTime indiaNow) => IndiaNow = indiaNow;

    public DateTime IndiaNow { get; set; }

    public DateTime UtcNow => IndiaNow - IstOffset;

    public void Advance(TimeSpan by) => IndiaNow += by;
}

internal sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(int? userId = 1, string? userName = "admin")
    {
        UserId = userId;
        UserName = userName;
    }

    public bool IsAuthenticated => UserId.HasValue;
    public int? UserId { get; }
    public string? UserName { get; }
    public IReadOnlyCollection<string> Rights => Array.Empty<string>();
}

internal sealed class FakeRequestContext : IRequestContext
{
    public string? IpAddress { get; set; } = "10.0.0.1";
    public string? UserAgent { get; set; } = "test-agent";
    public string? CorrelationId { get; set; } = "corr-1";
    public string? AccessToken { get; set; }
}

internal sealed class FakeUserLogWriter : IUserLogWriter
{
    public List<UserLogEntry> Entries { get; } = new();

    public Task WriteAsync(UserLogEntry entry, CancellationToken ct)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public bool Has(string actionType) => Entries.Any(e => e.ActionType == actionType);
}

internal sealed class FakeSecurityConfigProvider : ISecurityConfigProvider
{
    public FakeSecurityConfigProvider(SecuritySettings? settings = null)
        => Settings = settings ?? SecuritySettings.FromEntries(new Dictionary<string, string>());

    public SecuritySettings Settings { get; set; }

    public Task<SecuritySettings> GetAsync(CancellationToken ct) => Task.FromResult(Settings);
}
