using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Infrastructure.Database;
using ST.LiquorTNT.Infrastructure.Database.Repositories;
using Xunit;

namespace ST.LiquorTNT.Infrastructure.Tests.Database;

/// <summary>
/// The rights query against the real MySQL schema (db/mysql/009, 012): rights of a supplier code's role, of a
/// company-level role and custom rights, and that company-wide rows never leak into another company's supplier code.
/// Builds its own throw-away company, supplier codes, role and user, and removes them afterwards.
/// </summary>
public sealed class AccessRepositoryTests
{
    private static readonly string Connection =
        Environment.GetEnvironmentVariable("ST_TNT_TEST_CONNECTION")
        ?? "Server=192.168.1.99;Port=3306;Database=st_tnt_liquor;User ID=root;Password=root;";

    private static readonly DateTime Now = new(2026, 10, 7, 10, 0, 0);

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(Connection, new MySqlServerVersion(new Version(8, 0, 46)))
            .Options);

    [Fact]
    public async Task Permissions_AndSupplierCodes_FollowRolesCustomRightsAndCompany()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        await using var db = NewContext();

        // two companies (COMPANY has no create screen yet, so plain SQL)
        var companyA = await InsertCompanyAsync(db, "ZZ_TEST_A_" + tag);
        var companyB = await InsertCompanyAsync(db, "ZZ_TEST_B_" + tag);
        var exciseRj = await db.EXCISE.Where(e => e.ExciseCode == "RJ").Select(e => e.Id).SingleAsync();
        var category = await db.LIQUOR_CATEGORY.Select(c => c.Id).FirstAsync();

        var l1 = SUPPLIER_CODE.Create(companyA, null, exciseRj, "T1" + tag, category, Now, null);
        var l2 = SUPPLIER_CODE.Create(companyA, null, exciseRj, "T2" + tag, category, Now, null);
        var l3 = SUPPLIER_CODE.Create(companyB, null, exciseRj, "T3" + tag, category, Now, null);
        var user = USERS.Create("zz_access_" + tag, "H:x", companyA, null, null, null, null, false, null, Now, null);
        db.AddRange(l1, l2, l3, user);
        await db.SaveChangesAsync();

        // "ZZ Operator" of supplier code 1 and a company-level "ZZ Agent" (covers every supplier code of company A)
        var role = ROLES.Create(companyA, "ZZ Operator " + tag, null, false, Now, null, l1.Id);
        var companyRole = ROLES.Create(companyA, "ZZ Agent " + tag, null, false, Now, null);
        db.AddRange(role, companyRole);
        await db.SaveChangesAsync();

        try
        {
            var view = await db.PAGE_ACTIONS.SingleAsync(a => a.PermissionKey == "role.view");
            var unlock = await db.PAGE_ACTIONS.SingleAsync(a => a.PermissionKey == "user.unlock");
            var status = await db.PAGE_ACTIONS.SingleAsync(a => a.PermissionKey == "user.status");

            db.AddRange(
                ROLE_RIGHTS.Create(role.Id, view.Id, Now, null),
                ROLE_RIGHTS.Create(companyRole.Id, status.Id, Now, null),
                USER_ROLES.Create(user.Id, role.Id, Now, null),                 // Operator only in supplier code 1 (from the role)
                USER_ROLES.Create(user.Id, companyRole.Id, Now, null),          // company-level: all supplier codes of company A
                USER_RIGHTS.Create(user.Id, unlock.Id, null, Now, null));      // custom right in all supplier codes
            await db.SaveChangesAsync();

            var repo = new AccessRepository(db);

            (await repo.GetPermissionKeysAsync(user.Id, l1.Id, CancellationToken.None))
                .Should().BeEquivalentTo("role.view", "user.status", "user.unlock");
            (await repo.GetPermissionKeysAsync(user.Id, l2.Id, CancellationToken.None))
                .Should().BeEquivalentTo(new[] { "user.status", "user.unlock" }, "the Operator role belongs to supplier code 1 only");
            (await repo.GetPermissionKeysAsync(user.Id, l3.Id, CancellationToken.None))
                .Should().BeEmpty("an all-supplier-codes right never reaches another company's supplier code");

            (await repo.GetSupplierCodesForUserAsync(user.Id, CancellationToken.None)).Select(s => s.Id)
                .Should().BeEquivalentTo(new[] { l1.Id, l2.Id });

            (await repo.IsSuperAdminAsync(user.Id, CancellationToken.None)).Should().BeFalse();
            (await repo.IsSuperAdminAsync(1, CancellationToken.None)).Should().BeTrue();   // seeded admin

            var supplierCode = await repo.GetSupplierCodeAsync(l1.Id, CancellationToken.None);
            supplierCode!.DisplayName.Should().Be($"RJ {supplierCode.LiquorCategoryCode} T1{tag}");
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM USERS WHERE ID = {user.Id}");          // roles + rights cascade
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ROLES WHERE ID IN ({role.Id}, {companyRole.Id})");   // role rights cascade
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM SUPPLIER_CODE WHERE COMPANY_ID IN ({companyA}, {companyB})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM COMPANY WHERE ID IN ({companyA}, {companyB})");
        }
    }

    private static async Task<int> InsertCompanyAsync(AppDbContext db, string name)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO COMPANY (COMPANY_NAME, IS_ACTIVE, CREATED_AT) VALUES ({name}, 1, NOW())");
        return await db.COMPANY.Where(c => c.CompanyName == name).Select(c => c.Id).SingleAsync();
    }
}
