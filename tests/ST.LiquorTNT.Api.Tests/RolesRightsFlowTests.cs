using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.LiquorTNT.Contracts.Access;
using ST.LiquorTNT.Contracts.Companies;
using ST.LiquorTNT.Contracts.Excises;
using ST.LiquorTNT.Contracts.Roles;
using ST.LiquorTNT.Contracts.SupplierCodes;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Infrastructure.Database;
using Xunit;
using static ST.LiquorTNT.Api.Tests.ApiTestHelpers;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>
/// Roles, rights and supplier codes end to end over HTTP, against the dev MySQL: Super Admin creates a supplier code (the
/// company gets its default roles), an Agent Manager runs the company's users, rights are checked per supplier code and a
/// change applies on the next call. Builds its own throw-away company and removes everything afterwards.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class RolesRightsFlowTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public RolesRightsFlowTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task SupplierCode_DefaultRoles_AgentManager_RightsApplyOnNextCall()
    {
        await ResetAdminAsync(_factory);
        using var client = _factory.CreateClient();
        var tag = Guid.NewGuid().ToString("N")[..6];
        var companyId = await CreateCompanyAsync(tag);

        try
        {
            var admin = (await LoginOk(client, AdminUser, AdminPassword)).AccessToken;

            // 1. Super Admin creates the company's first supplier code -> company-level roles + this supplier code's roles
            var supplierCode = await Created<SupplierCodeResponse>(await PostJson(client, "/api/suppliercodes", new CreateSupplierCodeRequest
            {
                CompanyId = companyId, ExciseId = 2, SupplierCode = "E" + tag, LiquorCategoryId = 1,
            }, admin));
            supplierCode.DisplayName.Should().EndWith("E" + tag);

            await PostJson(client, "/api/auth/selectsuppliercode", new SelectSupplierCodeRequest { SupplierCodeId = supplierCode.Id }, admin);
            var roles = await GetWithToken<List<RoleResponse>>(client, "/api/roles", admin);
            roles.Where(r => r.SupplierCodeId == null).Select(r => r.RoleName).Should().BeEquivalentTo("Plant Admin", "Agent Manager");
            roles.Where(r => r.SupplierCodeId == supplierCode.Id).Select(r => r.DisplayName).Should().BeEquivalentTo(
                new[] { "Plant Manager", "Supervisor", "Operator", "Viewer" }.Select(n => $"{n} {supplierCode.DisplayName}"));
            int RoleId(string name) => roles.Single(r => r.RoleName == name).Id;

            // 2. an Agent Manager (company-level) and the Operator of this supplier code
            var agentName = "e2e_ag_" + tag;
            var operatorName = "e2e_op_" + tag;
            await CreateCompanyUserAsync(client, admin, agentName, companyId, RoleId("Agent Manager"));
            var operatorUser = await CreateCompanyUserAsync(client, admin, operatorName, companyId, RoleId("Operator"));

            // the company has one supplier code -> picked automatically at login
            var agentLogin = await LoginOk(client, agentName, StrongPassword);
            agentLogin.ActiveSupplierCode!.Id.Should().Be(supplierCode.Id);
            var agent = agentLogin.AccessToken;
            var op = (await LoginOk(client, operatorName, StrongPassword)).AccessToken;

            // 3. the Agent Manager manages users but is no administrator
            (await client.SendAsync(WithToken(HttpMethod.Get, "/api/users", agent))).StatusCode.Should().Be(HttpStatusCode.OK);

            var settings = await client.SendAsync(WithToken(HttpMethod.Get, "/api/securityconfig", agent));
            settings.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await ErrorCode(settings)).Should().Be("PERMISSION_DENIED");

            var own = await PutJson(client, $"/api/users/{agentLogin.User.UserId}/roles",
                new UpdateUserRolesRequest { Roles = new() { new UserRoleAssignment { RoleId = RoleId("Plant Admin") } } }, agent);
            own.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ErrorCode(own)).Should().Be("CANNOT_CHANGE_OWN_ACCESS");

            // 4. the Operator cannot list users ... until the Agent Manager gives a custom right; no new login needed
            (await client.SendAsync(WithToken(HttpMethod.Get, "/api/users", op))).StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var pages = await GetWithToken<List<PageResponse>>(client, "/api/pages", agent);
            var userView = pages.SelectMany(p => p.Actions).Single(a => a.PermissionKey == "user.view").PageActionId;
            (await PutJson(client, $"/api/users/{operatorUser.Id}/rights", new UpdateUserRightsRequest
            {
                Rights = new() { new UserRightAssignment { PageActionId = userView, SupplierCodeId = supplierCode.Id } },
            }, agent)).StatusCode.Should().Be(HttpStatusCode.OK);

            (await client.SendAsync(WithToken(HttpMethod.Get, "/api/users", op))).StatusCode.Should().Be(HttpStatusCode.OK);

            // 5. the menu sees the same
            var mine = await GetWithToken<MyPermissionsResponse>(client, "/api/auth/mypermissions", op);
            mine.Permissions.Should().Contain("user.view").And.NotContain("user.add");

            // 6. lists for dropdowns: excises for everyone with suppliercode.view, companies for Super Admin only
            var excises = await GetWithToken<List<ExciseResponse>>(client, "/api/excises", op);
            excises.Select(e => e.ExciseCode).Should().Contain(new[] { "RJ", "JK" });

            var companies = await GetWithToken<List<CompanyResponse>>(client, "/api/companies", admin);
            companies.Should().Contain(c => c.Id == companyId && c.SupplierCodeCount == 1);

            var notForCompanies = await client.SendAsync(WithToken(HttpMethod.Get, "/api/companies", agent));
            notForCompanies.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await ErrorCode(notForCompanies)).Should().Be("PERMISSION_DENIED");
        }
        finally
        {
            await CleanUpAsync(companyId, tag);
        }
    }

    [Fact]
    public async Task BusinessApi_BeforeAnySupplierCodeIsPicked_409SupplierCodeNotSelected()
    {
        await ResetAdminAsync(_factory);
        using var client = _factory.CreateClient();
        var tag = Guid.NewGuid().ToString("N")[..6];
        var companyId = await CreateCompanyAsync(tag);

        try
        {
            var admin = (await LoginOk(client, AdminUser, AdminPassword)).AccessToken;
            var first = await Created<SupplierCodeResponse>(await PostJson(client, "/api/suppliercodes",
                new CreateSupplierCodeRequest { CompanyId = companyId, ExciseId = 2, SupplierCode = "F" + tag, LiquorCategoryId = 1 }, admin));
            var second = await Created<SupplierCodeResponse>(await PostJson(client, "/api/suppliercodes",
                new CreateSupplierCodeRequest { CompanyId = companyId, ExciseId = 2, SupplierCode = "G" + tag, LiquorCategoryId = 1 }, admin));
            var third = await Created<SupplierCodeResponse>(await PostJson(client, "/api/suppliercodes",
                new CreateSupplierCodeRequest { CompanyId = companyId, ExciseId = 2, SupplierCode = "H" + tag, LiquorCategoryId = 1 }, admin));
            await PostJson(client, "/api/auth/selectsuppliercode", new SelectSupplierCodeRequest { SupplierCodeId = first.Id }, admin);
            var roles = await GetWithToken<List<RoleResponse>>(client, "/api/roles", admin);

            // the Plant Manager of 2 of the company's 3 supplier codes -> the picker offers only those 2, nothing picked yet
            var agentName = "e2e_ag_" + tag;
            int PlantManagerOf(int supplierCodeId) => roles.Single(r => r.RoleName == "Plant Manager" && r.SupplierCodeId == supplierCodeId).Id;
            await Created<UserResponse>(await PostJson(client, "/api/users", new CreateUserRequest
            {
                UserName = agentName, Password = StrongPassword, CompanyId = companyId, ForcePasswordChange = false,
                Roles = new()
                {
                    new UserRoleAssignment { RoleId = PlantManagerOf(first.Id) },
                    new UserRoleAssignment { RoleId = PlantManagerOf(second.Id) },
                },
            }, admin));
            var login = await LoginOk(client, agentName, StrongPassword);
            login.SupplierCodes.Select(s => s.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id });
            login.ActiveSupplierCode.Should().BeNull();

            // the third one is not offered and cannot be picked
            var foreign = await PostJson(client, "/api/auth/selectsuppliercode", new SelectSupplierCodeRequest { SupplierCodeId = third.Id }, login.AccessToken);
            foreign.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await ErrorCode(foreign)).Should().Be("SUPPLIER_CODE_NOT_ASSIGNED");

            var before = await client.SendAsync(WithToken(HttpMethod.Get, "/api/users", login.AccessToken));
            before.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ErrorCode(before)).Should().Be("SUPPLIER_CODE_NOT_SELECTED");

            await PostJson(client, "/api/auth/selectsuppliercode", new SelectSupplierCodeRequest { SupplierCodeId = first.Id }, login.AccessToken);
            (await client.SendAsync(WithToken(HttpMethod.Get, "/api/users", login.AccessToken))).StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await CleanUpAsync(companyId, tag);
        }
    }

    private static async Task<UserResponse> CreateCompanyUserAsync(HttpClient client, string adminToken, string userName, int companyId, int roleId) =>
        await Created<UserResponse>(await PostJson(client, "/api/users", new CreateUserRequest
        {
            UserName = userName,
            Password = StrongPassword,
            CompanyId = companyId,
            ForcePasswordChange = false,
            Roles = new() { new UserRoleAssignment { RoleId = roleId } },
        }, adminToken));

    private static async Task<T> Created<T>(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
    }

    /// <summary>There is no company screen yet, so the test company is written straight to the database.</summary>
    private async Task<int> CreateCompanyAsync(string tag)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var name = "ZZ_E2E_" + tag;

        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO COMPANY (COMPANY_NAME, IS_ACTIVE, CREATED_AT) VALUES ({name}, 1, NOW())");
        return await db.COMPANY.Where(c => c.CompanyName == name).Select(c => c.Id).SingleAsync();
    }

    /// <summary>Everything the test made, children first.</summary>
    private async Task CleanUpAsync(int companyId, string tag)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = $"e2e\\_%\\_{tag}";

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE USER_SESSION SET ACTIVE_SUPPLIER_CODE_ID = NULL WHERE ACTIVE_SUPPLIER_CODE_ID IN (SELECT ID FROM SUPPLIER_CODE WHERE COMPANY_ID = {companyId})");
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM USER_LOG WHERE USER_ID IN (SELECT ID FROM USERS WHERE USERNAME LIKE {users})");
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM USERS WHERE USERNAME LIKE {users}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM ROLE_PASSWORD_POLICY WHERE ROLE_ID IN (SELECT ID FROM ROLES WHERE COMPANY_ID = {companyId})");
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ROLES WHERE COMPANY_ID = {companyId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM SUPPLIER_CODE WHERE COMPANY_ID = {companyId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM COMPANY WHERE ID = {companyId}");
    }
}
