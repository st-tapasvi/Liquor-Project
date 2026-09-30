using FluentAssertions;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Logging;
using Xunit;

namespace ST.LiquorTNT.Api.Tests.Logging;

/// <summary>Nothing secret may reach the log, in any shape it arrives in.</summary>
public sealed class SafeJsonTests
{
    [Fact]
    public void Object_PasswordIsMasked_OtherFieldsKept()
    {
        var json = SafeJson.From(new LoginRequest { UserName = "ravi", Password = "S3cret!Pass" });

        json.Should().Contain("\"userName\":\"ravi\"").And.Contain("\"password\":\"***\"").And.NotContain("S3cret");
    }

    [Fact]
    public void NestedAndArrays_TokensAndAnswersMasked()
    {
        var json = SafeJson.From(new
        {
            items = new[] { new { accessToken = "eyJ.a.b", answer = "tommy" } },
            inner = new { requestToken = "k3J9", passwordHash = "PBKDF2.x" },
        });

        json.Should().NotContain("eyJ.a.b").And.NotContain("tommy").And.NotContain("k3J9").And.NotContain("PBKDF2");
    }

    [Fact]
    public void FieldsThatOnlyMentionPassword_AreNotMasked()
    {
        var json = SafeJson.From(new { forcePasswordChange = true, passwordExpiresAt = "2026-12-01" });

        json.Should().Contain("\"forcePasswordChange\":true").And.Contain("2026-12-01");
    }

    [Fact]
    public void RawJsonText_IsMasked_CaseInsensitive()
    {
        SafeJson.FromText("{\"UserName\":\"admin\",\"PASSWORD\":\"Admin@123\"}")
            .Should().NotContain("Admin@123").And.Contain("admin");
    }

    [Theory]
    [InlineData("{\"userName\":\"a\",\"password\":\"Secret1\",}")]      // trailing comma: not valid JSON
    [InlineData("userName=a&password=Secret1")]                         // form body
    [InlineData("{\"password\":\"Secret1\",\"password\":\"b\"}")]       // duplicate key
    public void BodyThatCannotBeParsed_IsNotWrittenAtAll(string text)
    {
        var logged = SafeJson.FromText(text);

        logged.Should().StartWith("<body not logged").And.NotContain("Secret1");
    }

    [Fact]
    public void LongJson_IsMaskedThenCut()
    {
        var json = SafeJson.FromText("{\"password\":\"Secret1\",\"note\":\"" + new string('x', 50) + "\"}", maxLength: 20);

        json.Should().EndWith("…(truncated)").And.NotContain("Secret1");
    }

    [Theory]
    [InlineData("confirmPassword")]
    [InlineData("new_password")]
    [InlineData("pin")]
    [InlineData("SessionTokenHash")]
    [InlineData("securityAnswer")]
    public void SecretByNameEnding(string name) => SafeJson.IsSecretName(name).Should().BeTrue();

    [Theory]
    [InlineData("forcePasswordChange")]
    [InlineData("passwordExpiresAt")]
    [InlineData("passwordHistoryCount")]
    [InlineData("userName")]
    public void ReadableField_NotSecret(string name) => SafeJson.IsSecretName(name).Should().BeFalse();

    [Fact]
    public void ValueThatCannotBeSerialised_IsDescribed_NeverThrows()
    {
        SafeJson.From(new Throwing()).Should().Be("<not serialisable: Throwing>");
    }

    private sealed class Throwing
    {
        public string Value => throw new ArgumentException("boom");
    }

    [Fact]
    public void Null_And_Empty()
    {
        SafeJson.From(null).Should().Be("null");
        SafeJson.FromText("   ").Should().BeEmpty();
    }
}
