using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Infrastructure.Database;
using ST.LiquorTNT.Infrastructure.Database.Repositories;
using Xunit;

namespace ST.LiquorTNT.Infrastructure.Tests.Database;

/// <summary>
/// Roles against the real MySQL schema: a role of a supplier code is listed and named with it, and a role with
/// rights and a password policy can be deleted in one SaveChanges (the link has no cascade, so EF must delete it first).
/// </summary>
public sealed class RoleRepositoryTests
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
    public async Task RoleOfASupplierCode_IsNamedWithIt_AndDeletesWithRightsAndPolicy()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        await using var db = NewContext();

        var name = "ZZ_ROLES_" + tag;
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO COMPANY (COMPANY_NAME, IS_ACTIVE, CREATED_AT) VALUES ({name}, 1, NOW())");
        var company = await db.COMPANY.Where(c => c.CompanyName == name).Select(c => c.Id).SingleAsync();
        var exciseRj = await db.EXCISE.Where(e => e.ExciseCode == "RJ").Select(e => e.Id).SingleAsync();
        var category = await db.LIQUOR_CATEGORY.FirstAsync();
        var supplierCode = SUPPLIER_CODE.Create(company, null, exciseRj, "R" + tag, category.Id, Now, null);
        db.Add(supplierCode);
        await db.SaveChangesAsync();

        try
        {
            var role = ROLES.Create(company, "Operator", null, false, Now, null, supplierCode.Id);
            db.Add(role);
            await db.SaveChangesAsync();

            var view = await db.PAGE_ACTIONS.SingleAsync(a => a.PermissionKey == "suppliercode.view");
            var policy = await db.PASSWORD_POLICY.Select(p => p.Id).FirstAsync();
            db.AddRange(ROLE_RIGHTS.Create(role.Id, view.Id, Now, null), ROLE_PASSWORD_POLICY.Create(role.Id, policy, Now, null));
            await db.SaveChangesAsync();

            var repo = new RoleRepository(db);

            var listed = (await repo.GetListAsync(company, supplierCode.Id, CancellationToken.None)).Single(r => r.Id == role.Id);
            listed.DisplayName.Should().Be($"Operator RJ {category.CategoryCode} R{tag}");
            listed.PasswordPolicyId.Should().Be(policy);

            await repo.RemoveAsync(role, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);

            (await db.ROLES.AnyAsync(r => r.Id == role.Id)).Should().BeFalse();
            (await db.ROLE_PASSWORD_POLICY.AnyAsync(l => l.RoleId == role.Id)).Should().BeFalse();
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM ROLE_PASSWORD_POLICY WHERE ROLE_ID IN (SELECT ID FROM ROLES WHERE COMPANY_ID = {company})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ROLES WHERE COMPANY_ID = {company}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM SUPPLIER_CODE WHERE COMPANY_ID = {company}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM COMPANY WHERE ID = {company}");
        }
    }
}
