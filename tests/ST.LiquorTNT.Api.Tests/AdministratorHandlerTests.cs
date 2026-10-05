using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using Xunit;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>The interim admin gate: only the configured ADMIN_ROLE_ID passes; everything else is refused.</summary>
public sealed class AdministratorHandlerTests
{
    private sealed class StubConfig : ISecurityConfigProvider
    {
        public Task<SecuritySettings> GetAsync(CancellationToken ct) =>
            Task.FromResult(SecuritySettings.FromEntries(new Dictionary<string, string> { [SecuritySettings.Keys.AdminRoleId] = "7" }));
    }

    private static async Task<bool> Passes(params Claim[] claims)
    {
        var requirement = new AdministratorRequirement();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new AuthorizationHandlerContext(new[] { requirement }, principal, resource: null);

        await new AdministratorHandler(new StubConfig()).HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task ConfiguredAdminRole_Passes() => (await Passes(new Claim("role_id", "7"))).Should().BeTrue();

    [Theory]
    [InlineData("1")]        // a different role (even the seed default) is not the configured admin role
    [InlineData("abc")]
    public async Task OtherRole_IsRefused(string roleId) => (await Passes(new Claim("role_id", roleId))).Should().BeFalse();

    [Fact]
    public async Task NoRoleClaim_IsRefused() => (await Passes(new Claim("sub", "1"))).Should().BeFalse();
}
