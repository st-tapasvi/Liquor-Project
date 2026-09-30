using FluentAssertions;
using ST.LiquorTNT.Infrastructure.Identity;
using Xunit;

namespace ST.LiquorTNT.Infrastructure.Tests.Identity;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Admin@123");

        _hasher.Verify("Admin@123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_IsCaseSensitive()
    {
        var hash = _hasher.Hash("Admin@123");

        _hasher.Verify("admin@123", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_CalledTwice_ProducesDifferentValues()
    {
        _hasher.Hash("Admin@123").Should().NotBe(_hasher.Hash("Admin@123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("PBKDF2.SHA256.100000.bad-base64.bad-base64")]
    public void Verify_WithMalformedHash_ReturnsFalseAndDoesNotThrow(string storedHash)
    {
        _hasher.Verify("Admin@123", storedHash).Should().BeFalse();
    }
}
