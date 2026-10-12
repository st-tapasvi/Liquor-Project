using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Infrastructure.Database;
using ST.LiquorTNT.Infrastructure.Database.Repositories;
using Xunit;

namespace ST.LiquorTNT.Infrastructure.Tests.Database;

/// <summary>
/// Pages and actions by excise (db/mysql/017, PAGE_EXCISES / PAGE_ACTION_EXCISES). The foreign keys keep every excise
/// real; the first test guards the one rule the database cannot (an action only where its page is), the second shows
/// that a right counts only in the excise its page / action exists in.
/// </summary>
public sealed class PageExciseTests
{
    private static readonly string Connection =
        Environment.GetEnvironmentVariable("ST_TNT_TEST_CONNECTION")
        ?? "Server=192.168.1.99;Port=3306;Database=st_tnt_liquor;User ID=root;Password=root;";

    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0);

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(Connection, new MySqlServerVersion(new Version(8, 0, 46)))
            .Options);

    [Fact]
    public async Task SeededActionExcises_StayInsideTheirPagesExcises()
    {
        await using var db = NewContext();

        // an action row for an excise its page is limited away from: the action could never be used there
        var outside = await (from ae in db.PAGE_ACTION_EXCISES
                             join pa in db.PAGE_ACTIONS on ae.PageActionId equals pa.Id
                             where db.PAGE_EXCISES.Any(pe => pe.PageId == pa.PageId)
                                   && !db.PAGE_EXCISES.Any(pe => pe.PageId == pa.PageId && pe.ExciseId == ae.ExciseId)
                             select pa.PermissionKey)
            .ToListAsync();

        outside.Should().BeEmpty("an action cannot exist in an excise where its page does not");
    }

    [Fact]
    public async Task RightOfAJkOnlyPage_CountsInJk_NotInRj()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        await using var db = NewContext();

        var name = "ZZ_EXCISE_" + tag;
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO COMPANY (COMPANY_NAME, IS_ACTIVE, CREATED_AT) VALUES ({name}, 1, NOW())");
        var company = await db.COMPANY.Where(c => c.CompanyName == name).Select(c => c.Id).SingleAsync();
        var rj = await db.EXCISE.Where(e => e.ExciseCode == "RJ").Select(e => e.Id).SingleAsync();
        var jk = await db.EXCISE.Where(e => e.ExciseCode == "JK").Select(e => e.Id).SingleAsync();
        var category = await db.LIQUOR_CATEGORY.Select(c => c.Id).FirstAsync();

        // a page that exists only in JK, with one action
        var pageKey = "zzjk" + tag;
        var permission = pageKey + ".view";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO PAGES (PAGE_NAME, MODULE_NAME, PAGE_KEY, APPLICATION_TYPE, SORT_ORDER, IS_ACTIVE) VALUES ({"ZZ JK " + tag}, 'Test', {pageKey}, 'WEB', 99, 1)");
        var pageId = await db.PAGES.Where(p => p.PageKey == pageKey).Select(p => p.Id).SingleAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PAGE_EXCISES (PAGE_ID, EXCISE_ID) VALUES ({pageId}, {jk})");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO PAGE_ACTIONS (PAGE_ID, ACTION_KEY, PERMISSION_KEY, ACTION_NAME, GRANT_SCOPE, SORT_ORDER, IS_ACTIVE) VALUES ({pageId}, 'view', {permission}, 'View', 'ANY', 1, 1)");
        var actionId = await db.PAGE_ACTIONS.Where(a => a.PermissionKey == permission).Select(a => a.Id).SingleAsync();

        var rjCode = SUPPLIER_CODE.Create(company, null, rj, "X1" + tag, category, Now, null);
        var jkCode = SUPPLIER_CODE.Create(company, null, jk, "X2" + tag, category, Now, null);
        var role = ROLES.Create(company, "ZZ Manager", null, false, Now, null);          // company-level: RJ and JK
        var user = USERS.Create("zz_exc_" + tag, "H:x", company, null, null, null, null, false, null, Now, null);
        db.AddRange(rjCode, jkCode, role, user);
        await db.SaveChangesAsync();

        try
        {
            db.AddRange(ROLE_RIGHTS.Create(role.Id, actionId, Now, null), USER_ROLES.Create(user.Id, role.Id, Now, null));
            await db.SaveChangesAsync();

            var access = new AccessRepository(db);
            (await access.GetPermissionKeysAsync(user.Id, jkCode.Id, CancellationToken.None)).Should().Contain(permission);
            (await access.GetPermissionKeysAsync(user.Id, rjCode.Id, CancellationToken.None)).Should().NotContain(permission);
            (await access.GetAllPermissionKeysAsync("RJ", CancellationToken.None)).Should().NotContain(permission).And.Contain("user.view");
            (await access.GetAllPermissionKeysAsync(null, CancellationToken.None)).Should().Contain(permission);

            // the role works in both excises of its company
            (await new RoleRepository(db).GetExciseCodesForRoleAsync(role, CancellationToken.None)).Should().Equal("JK", "RJ");
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM USERS WHERE ID = {user.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ROLES WHERE COMPANY_ID = {company}");      // role rights cascade
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM PAGE_ACTIONS WHERE PAGE_ID = {pageId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM PAGES WHERE ID = {pageId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM SUPPLIER_CODE WHERE COMPANY_ID = {company}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM COMPANY WHERE ID = {company}");
        }
    }
}
