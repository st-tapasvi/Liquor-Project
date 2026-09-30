using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>
/// All end-to-end classes share one API host and the same admin account, so they must not run in
/// parallel: one class resetting admin sessions would invalidate another class's token mid-test.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
    public const string Name = "Api";
}
