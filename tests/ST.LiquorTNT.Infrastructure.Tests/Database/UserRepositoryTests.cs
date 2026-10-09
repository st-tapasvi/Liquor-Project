using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Infrastructure.Database;
using ST.LiquorTNT.Infrastructure.Database.Repositories;
using Xunit;

namespace ST.LiquorTNT.Infrastructure.Tests.Database;

/// <summary>
/// Verifies EF mappings and repositories against a real MySQL. The connection comes from the
/// ST_TNT_TEST_CONNECTION environment variable, falling back to the local dev server.
/// Moves to Testcontainers later (standards §17) so it needs no shared database.
/// </summary>
public sealed class UserRepositoryTests
{
    private static readonly string Connection =
        Environment.GetEnvironmentVariable("ST_TNT_TEST_CONNECTION")
        ?? "Server=192.168.1.99;Port=3306;Database=st_tnt_liquor;User ID=root;Password=root;";

    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(Connection, new MySqlServerVersion(new Version(8, 0, 46)))
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByUserNameAsync_ReturnsSeededAdmin()
    {
        await using var db = NewContext();
        var repo = new UserRepository(db);

        var user = await repo.GetByUserNameAsync("admin", CancellationToken.None);

        user.Should().NotBeNull();
        user!.UserName.Should().Be("admin");
        user.IsActive.Should().BeTrue();
        user.CompanyId.Should().BeNull();  // admin belongs to no company
    }

    [Fact]
    public async Task GetByUserNameAsync_WhenUnknown_ReturnsNull()
    {
        await using var db = NewContext();

        var user = await new UserRepository(db).GetByUserNameAsync("no-such-user", CancellationToken.None);

        user.Should().BeNull();
    }

    [Fact]
    public async Task GetPageAsync_ProjectsFiltersAndPages()
    {
        await using var db = NewContext();
        var repo = new UserRepository(db);

        var all = await repo.GetPageAsync(null, null, page: 1, pageSize: 200, CancellationToken.None);
        var filtered = await repo.GetPageAsync(null, "adm", page: 1, pageSize: 10, CancellationToken.None);
        var tiny = await repo.GetPageAsync(null, null, page: 1, pageSize: 1, CancellationToken.None);
        var beyond = await repo.GetPageAsync(null, null, page: 100000, pageSize: 50, CancellationToken.None);

        all.TotalCount.Should().Be(await db.USERS.CountAsync());
        all.Items.Select(u => u.UserName).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);   // MySQL collation ignores case
        filtered.Items.Should().OnlyContain(u => u.UserName.Contains("adm", StringComparison.OrdinalIgnoreCase) || (u.FullName ?? "").Contains("adm", StringComparison.OrdinalIgnoreCase));
        filtered.TotalCount.Should().Be(filtered.Items.Count).And.BeLessThanOrEqualTo(all.TotalCount);
        tiny.Items.Should().HaveCount(1);
        tiny.TotalCount.Should().Be(all.TotalCount);
        beyond.Items.Should().BeEmpty();
        beyond.TotalCount.Should().Be(all.TotalCount);
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateUserNameDifferingOnlyByCase_Returns409NotDatabaseError()
    {
        var name = "dup_" + Guid.NewGuid().ToString("N")[..8];
        var now = new DateTime(2026, 9, 28, 10, 0, 0);
        await using var db = NewContext();
        var repo = new UserRepository(db);

        var first = USERS.Create(name, "H:x", null, null, null, null, null, false, null, now, null);
        await repo.AddAsync(first, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        try
        {
            await using var db2 = NewContext();
            var repo2 = new UserRepository(db2);
            var second = USERS.Create(name.ToUpperInvariant(), "H:x", null, null, null, null, null, false, null, now, null);
            await repo2.AddAsync(second, CancellationToken.None);

            var ex = await repo2.Invoking(r => r.SaveChangesAsync(CancellationToken.None)).Should().ThrowAsync<BusinessException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.UserNameTaken);

            (await repo.UserNameExistsAsync(name.ToUpperInvariant(), CancellationToken.None)).Should().BeTrue();   // case-insensitive
        }
        finally
        {
            db.USERS.Remove(first);                 // password history cascades
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task PasswordPolicyRepository_ResolvesPolicyThroughRoles()
    {
        await using var db = NewContext();
        var repo = new PasswordPolicyRepository(db);

        var byRole = await repo.GetForRolesAsync(new[] { 1, 9999 }, CancellationToken.None);   // Super Admin -> HARD (seed)
        var adminRoles = await repo.GetRoleIdsForUserAsync(1, CancellationToken.None);         // admin holds Super Admin

        byRole.Should().ContainKey(1).And.NotContainKey(9999);
        byRole[1].PolicyName.Should().Be("HARD");
        byRole[1].MinLength.Should().Be(12);
        adminRoles.Should().Contain(1);
    }

    [Fact]
    public async Task ReferenceLookup_ChecksExistence()
    {
        await using var db = NewContext();
        var lookup = new ReferenceLookup(db);

        (await lookup.RoleExistsAsync(1, CancellationToken.None)).Should().BeTrue();
        (await lookup.RoleExistsAsync(9999, CancellationToken.None)).Should().BeFalse();
        (await lookup.CompanyExistsAsync(9999, CancellationToken.None)).Should().BeFalse();
    }
}
