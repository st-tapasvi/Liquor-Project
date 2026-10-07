using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Infrastructure.Identity;
using Xunit;

namespace ST.LiquorTNT.Infrastructure.Tests.Identity;

public sealed class TokenServicesTests
{
    [Fact]
    public void Sha256TokenHasher_IsDeterministic_64HexChars()
    {
        var hasher = new Sha256TokenHasher();

        var a = hasher.Hash("token-1");
        var b = hasher.Hash("token-1");
        var c = hasher.Hash("token-2");

        a.Should().Be(b).And.HaveLength(64).And.MatchRegex("^[0-9a-f]{64}$");
        a.Should().NotBe(c);
    }

    [Fact]
    public void Sha256TokenHasher_EmptyToken_Throws()
    {
        new Sha256TokenHasher().Invoking(h => h.Hash("")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Jwt_ShortSigningKey_IsRefusedAtStartup()
    {
        var options = Options.Create(new JwtOptions { SigningKey = "too-short" });

        var act = () => new JwtAccessTokenService(options);

        act.Should().Throw<InvalidOperationException>().WithMessage("*32*");
    }

    [Fact]
    public void Jwt_Create_CarriesIdentityClaims_AndExpiry()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "ST.LiquorTNT",
            Audience = "ST.LiquorTNT.Web",
            SigningKey = "unit-test-signing-key-with-at-least-32-chars",
        });
        var user = USERS.Create("alice", "H:x", companyId: 5, null, null, null, null, false, null,
            new DateTime(2026, 9, 28, 10, 0, 0), null);
        var expires = DateTime.UtcNow.AddMinutes(30);

        var token = new JwtAccessTokenService(options).Create(user, expires);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("ST.LiquorTNT");
        jwt.Audiences.Should().Contain("ST.LiquorTNT.Web");
        jwt.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "alice");
        jwt.Claims.Should().NotContain(c => c.Type == "role_id");         // rights are read per request, never from the token
        jwt.Claims.Should().Contain(c => c.Type == "company_id" && c.Value == "5");
        jwt.Claims.Should().Contain(c => c.Type == "jti");
        jwt.ValidTo.Should().BeCloseTo(expires, TimeSpan.FromSeconds(1));
        jwt.Claims.Should().NotContain(c => c.Value.Contains("H:x"));   // no hash in the token
    }
}
