using FluentAssertions;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Users;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Users;

public sealed class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    private static CreateUserRequest Valid() => new()
    {
        UserName = "bob", Password = "x", Roles = new() { new UserRoleAssignment { RoleId = 1 } },
    };

    private IEnumerable<string> FailingFields(CreateUserRequest request) =>
        _validator.Validate(request).Errors.Select(e => e.PropertyName).Distinct();

    [Fact]
    public void ValidRequest_Passes() => FailingFields(Valid()).Should().BeEmpty();

    [Theory]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void UserName_MaxLength50_Inclusive(int length, bool ok)
    {
        var request = Valid();
        request.UserName = new string('a', length);

        (FailingFields(request).Contains(nameof(request.UserName)) == false).Should().Be(ok);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has space")]
    [InlineData(" leading")]
    [InlineData("राम")]            // non-ASCII letters are not accepted as a login name
    [InlineData("bob%")]
    [InlineData("bob;drop")]
    public void UserName_RejectsBlankOrUnsafeCharacters(string userName)
    {
        var request = Valid();
        request.UserName = userName;

        FailingFields(request).Should().Contain(nameof(request.UserName));
    }

    [Theory]
    [InlineData("bob.smith")]
    [InlineData("bob_smith-1")]
    [InlineData("bob@plant")]
    public void UserName_AcceptsDotUnderscoreAtDash(string userName)
    {
        var request = Valid();
        request.UserName = userName;

        FailingFields(request).Should().NotContain(nameof(request.UserName));
    }

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Password_MaxLength128_Inclusive(int length, bool ok)
    {
        var request = Valid();
        request.Password = new string('p', length);

        (FailingFields(request).Contains(nameof(request.Password)) == false).Should().Be(ok);
    }

    [Fact]
    public void Roles_AtLeastOneRequired()
    {
        var request = Valid();
        request.Roles.Clear();

        FailingFields(request).Should().Contain(nameof(request.Roles));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RoleIds_MustBePositive(int roleId)
    {
        var request = Valid();
        request.Roles[0] = new UserRoleAssignment { RoleId = roleId };

        FailingFields(request).Should().Contain(f => f.StartsWith("Roles[0]"));
    }

    [Fact]
    public void CompanyId_ZeroRejected_NullAllowed()
    {
        var zero = Valid();
        zero.CompanyId = 0;
        FailingFields(zero).Should().Contain(nameof(zero.CompanyId));

        var none = Valid();
        none.CompanyId = null;
        FailingFields(none).Should().NotContain(nameof(none.CompanyId));
    }

    [Fact]
    public void Email_InvalidOrTooLong_Rejected_BlankAllowed()
    {
        var bad = Valid();
        bad.Email = "not-an-email";
        FailingFields(bad).Should().Contain(nameof(bad.Email));

        var tooLong = Valid();
        tooLong.Email = new string('a', 95) + "@x.com";     // 101 chars
        FailingFields(tooLong).Should().Contain(nameof(tooLong.Email));

        var blank = Valid();
        blank.Email = "";
        FailingFields(blank).Should().NotContain(nameof(blank.Email));
    }
}
