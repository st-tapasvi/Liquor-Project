using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace ST.LiquorTNT.Architecture.Tests;

/// <summary>
/// The dependency rules from the architecture standard, enforced by the build.
/// If one of these fails, the structure was broken - fix the code, not the test.
/// </summary>
public class LayerRuleTests
{
    private static readonly Assembly Domain = typeof(ST.LiquorTNT.Domain.Entities.USERS).Assembly;
    private static readonly Assembly Business = typeof(ST.LiquorTNT.Business.DependencyInjection).Assembly;
    private static readonly Assembly Contracts = typeof(ST.LiquorTNT.Contracts.Common.PagedResponse<>).Assembly;
    private static readonly Assembly Api = typeof(ST.LiquorTNT.Api.Extensions.ApiServiceExtensions).Assembly;

    [Fact]
    public void Domain_DependsOnNoOtherLayer()
    {
        var result = Types.InAssembly(Domain)
            .That().ResideInNamespaceStartingWith("ST.LiquorTNT.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "ST.LiquorTNT.Business",
                "ST.LiquorTNT.Infrastructure",
                "ST.LiquorTNT.Api",
                "ST.LiquorTNT.Contracts")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Fact]
    public void Domain_DoesNotUseEntityFrameworkOrHttp()
    {
        var result = Types.InAssembly(Domain)
            .That().ResideInNamespaceStartingWith("ST.LiquorTNT.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "System.Net.Http",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Fact]
    public void Business_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(Business)
            .That().ResideInNamespaceStartingWith("ST.LiquorTNT.Business")
            .ShouldNot().HaveDependencyOnAny("ST.LiquorTNT.Infrastructure", "ST.LiquorTNT.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Fact]
    public void Business_DoesNotUseEntityFrameworkOrADatabaseDriver()
    {
        var result = Types.InAssembly(Business)
            .That().ResideInNamespaceStartingWith("ST.LiquorTNT.Business")
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "MySqlConnector",
                "System.Data.Common",
                "System.Net.Http")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Fact]
    public void Contracts_DependOnNothing()
    {
        var result = Types.InAssembly(Contracts)
            .That().ResideInNamespaceStartingWith("ST.LiquorTNT.Contracts")
            .ShouldNot().HaveDependencyOnAny(
                "ST.LiquorTNT.Domain",
                "ST.LiquorTNT.Business",
                "ST.LiquorTNT.Infrastructure",
                "ST.LiquorTNT.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Fact]
    public void Api_ReferencesInfrastructureOnlyInExtensions()
    {
        var result = Types.InAssembly(Api)
            .That().ResideInNamespaceStartingWith("ST.LiquorTNT.Api")
            .And().DoNotResideInNamespace("ST.LiquorTNT.Api.Extensions")
            .ShouldNot().HaveDependencyOn("ST.LiquorTNT.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Fact]
    public void Controllers_DoNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(Api)
            .That().ResideInNamespaceStartingWith("ST.LiquorTNT.Api.Controllers")
            .ShouldNot().HaveDependencyOn("ST.LiquorTNT.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    private static string Describe(TestResult result) =>
        result.FailingTypeNames is null
            ? string.Empty
            : "Offending types: " + string.Join(", ", result.FailingTypeNames);
}
