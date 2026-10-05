using FluentAssertions;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.PasswordPolicies;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Contracts.PasswordPolicies;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.PasswordPolicies;

public sealed class PasswordPolicyServiceTests
{
    private readonly FakePasswordPolicyRepository _repo = new(TestData.Policy(id: 3, name: "HARD", minLength: 12, upper: true));
    private readonly FakeUserLogWriter _log = new();
    private readonly PasswordPolicyService _service;

    public PasswordPolicyServiceTests()
    {
        _service = new PasswordPolicyService(_repo, new FixedClock(TestData.Now), new FakeCurrentUser(userId: 1), _log,
            new UpdatePasswordPolicyRequestValidator());
    }

    private static UpdatePasswordPolicyRequest Valid() => new()
    {
        MinLength = 10, MaxLength = 40, RequireUppercase = false, RequireLowercase = true, RequireNumber = true,
        RequireSpecialCharacter = true, PasswordHistoryCount = 8, PasswordExpiryEnabled = true, PasswordExpiryDays = 60,
        AllowUsernameInPassword = false, AllowCommonPassword = false,
    };

    [Fact]
    public async Task GetAll_MapsRules()
    {
        var all = await _service.GetAllAsync(CancellationToken.None);

        all.Should().ContainSingle(p => p.PolicyName == "HARD" && p.MinLength == 12 && p.RequireUppercase);
    }

    [Fact]
    public async Task Update_Unknown_404()
    {
        await _service.Invoking(s => s.UpdateAsync(99, Valid(), CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_MaxBelowMin_400_NothingSaved()
    {
        var request = Valid();
        request.MaxLength = 5;

        var ex = await _service.Invoking(s => s.UpdateAsync(3, request, CancellationToken.None)).Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().ContainKey("maxLength");
        _repo.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_ExpiryEnabledWithoutDays_400()
    {
        var request = Valid();
        request.PasswordExpiryDays = null;

        var ex = await _service.Invoking(s => s.UpdateAsync(3, request, CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("passwordExpiryDays");
    }

    [Fact]
    public async Task Update_Valid_ChangesRules_AuditsOldAndNew()
    {
        var response = await _service.UpdateAsync(3, Valid(), CancellationToken.None);

        response.MinLength.Should().Be(10);
        response.RequireUppercase.Should().BeFalse();
        response.PasswordExpiryDays.Should().Be(60);
        _repo.Policies.Single().PasswordHistoryCount.Should().Be(8);
        _repo.SaveCount.Should().Be(1);

        var entry = _log.Entries.Single(e => e.ActionType == UserLogActions.PasswordPolicyChanged);
        ((PasswordPolicyResponse)entry.OldValue!).MinLength.Should().Be(12);
        ((PasswordPolicyResponse)entry.NewValue!).MinLength.Should().Be(10);
    }
}
