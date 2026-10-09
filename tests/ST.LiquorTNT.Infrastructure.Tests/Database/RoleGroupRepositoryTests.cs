using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Infrastructure.Database;
using ST.LiquorTNT.Infrastructure.Database.Repositories;
using Xunit;

namespace ST.LiquorTNT.Infrastructure.Tests.Database;

/// <summary>
/// Role groups against the real MySQL schema (db/mysql/016): a role reached through a group gives its rights, its supplier
/// code appears in the picker and an admin role in a group makes an admin user, exactly like a direct role.
/// </summary>
public sealed class RoleGroupRepositoryTests
{
    private static readonly string Connection =
        Environment.GetEnvironmentVariable("ST_TNT_TEST_CONNECTION")
        ?? "Server=192.168.1.99;Port=3306;Database=st_tnt_liquor;User ID=root;Password=root;";

    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0);

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(Connection, new MySqlServerVersion(new Version(8, 0, 46)))
            .Options);

    [Fact]
    public async Task RolesOfAGroup_CountLikeDirectRoles()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        await using var db = NewContext();

        var name = "ZZ_GROUPS_" + tag;
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO COMPANY (COMPANY_NAME, IS_ACTIVE, CREATED_AT) VALUES ({name}, 1, NOW())");
        var company = await db.COMPANY.Where(c => c.CompanyName == name).Select(c => c.Id).SingleAsync();
        var exciseRj = await db.EXCISE.Where(e => e.ExciseCode == "RJ").Select(e => e.Id).SingleAsync();
        var category = await db.LIQUOR_CATEGORY.Select(c => c.Id).FirstAsync();

        var code1 = SUPPLIER_CODE.Create(company, null, exciseRj, "G1" + tag, category, Now, null);
        var code2 = SUPPLIER_CODE.Create(company, null, exciseRj, "G2" + tag, category, Now, null);
        var user = USERS.Create("zz_grp_" + tag, "H:x", company, null, null, null, null, false, null, Now, null);
        db.AddRange(code1, code2, user);
        await db.SaveChangesAsync();

        try
        {
            var operator2 = ROLES.Create(company, "Operator", null, false, Now, null, code2.Id);
            var plantAdmin = ROLES.Create(company, "Plant Admin", null, isAdminRole: true, Now, null);
            db.AddRange(operator2, plantAdmin);
            await db.SaveChangesAsync();

            var view = await db.PAGE_ACTIONS.SingleAsync(a => a.PermissionKey == "suppliercode.view");
            var group = ROLE_GROUP.Create(company, "All Operators", null, Now, null);
            db.AddRange(ROLE_RIGHTS.Create(operator2.Id, view.Id, Now, null), group);
            await db.SaveChangesAsync();

            var groups = new RoleGroupRepository(db);
            await groups.ReplaceRolesAsync(group.Id, new[] { operator2.Id }, Now, null, CancellationToken.None);
            db.Add(USER_ROLE_GROUPS.Create(user.Id, group.Id, Now, null));
            await db.SaveChangesAsync();

            var access = new AccessRepository(db);

            // the group's role gives its right in its supplier code, and that code is offered in the picker
            (await access.GetPermissionKeysAsync(user.Id, code2.Id, CancellationToken.None)).Should().BeEquivalentTo("suppliercode.view");
            (await access.GetPermissionKeysAsync(user.Id, code1.Id, CancellationToken.None)).Should().BeEmpty();
            (await access.GetSupplierCodesForUserAsync(user.Id, CancellationToken.None)).Select(s => s.Id).Should().Equal(code2.Id);

            var projected = await groups.GetResponseAsync(group.Id, CancellationToken.None);
            projected!.UserCount.Should().Be(1);
            projected.Roles.Should().ContainSingle().Which.DisplayName.Should().Be($"Operator RJ {(await db.LIQUOR_CATEGORY.FindAsync(category))!.CategoryCode} G2{tag}");
            (await groups.IsAssignedAsync(group.Id, CancellationToken.None)).Should().BeTrue();
            (await groups.IsMemberAsync(group.Id, user.Id, CancellationToken.None)).Should().BeTrue();

            // an admin role inside a group makes the user an admin user
            var userAccess = new UserAccessRepository(db);
            (await userAccess.IsAdminUserAsync(user.Id, CancellationToken.None)).Should().BeFalse();
            await groups.ReplaceRolesAsync(group.Id, new[] { operator2.Id, plantAdmin.Id }, Now, null, CancellationToken.None);
            await db.SaveChangesAsync();
            (await userAccess.IsAdminUserAsync(user.Id, CancellationToken.None)).Should().BeTrue();
            (await groups.HasAdminMemberAsync(group.Id, CancellationToken.None)).Should().BeTrue();

            var access2 = await userAccess.GetAccessAsync(user, CancellationToken.None);
            access2.Roles.Should().BeEmpty();
            access2.RoleGroups.Should().ContainSingle().Which.Roles.Should().HaveCount(2);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM USERS WHERE ID = {user.Id}");                // user groups cascade
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ROLE_GROUP WHERE COMPANY_ID = {company}");    // group roles cascade
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ROLES WHERE COMPANY_ID = {company}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM SUPPLIER_CODE WHERE COMPANY_ID = {company}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM COMPANY WHERE ID = {company}");
        }
    }
}
