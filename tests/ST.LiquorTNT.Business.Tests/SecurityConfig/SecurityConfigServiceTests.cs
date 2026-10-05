using FluentAssertions;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.SecurityConfig;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Contracts.SecurityConfig;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.SecurityConfig;

public sealed class SecurityConfigServiceTests
{
    private readonly FakeSecurityConfigRepository _repo = new();
    private readonly FakeUserLogWriter _log = new();
    private readonly SecurityConfigService _service;

    public SecurityConfigServiceTests()
    {
        _repo.Rows.Add(SeedRows.Config(SecuritySettings.Keys.MaxFailedLoginAttempts, "3", "INT", id: 1));
        _repo.Rows.Add(SeedRows.Config(SecuritySettings.Keys.FailedLoginLockEnabled, "1", "BOOL", id: 2));
        _repo.Rows.Add(SeedRows.Config(SecuritySettings.Keys.SessionFullBehaviour, "REJECT", "STRING", id: 3));

        _service = new SecurityConfigService(_repo, new FixedClock(TestData.Now), new FakeCurrentUser(userId: 1), _log);
    }

    private Task<SecurityConfigResponse> Update(string key, string value) =>
        _service.UpdateAsync(key, new UpdateSecurityConfigRequest { Value = value }, CancellationToken.None);

    [Fact]
    public async Task GetAll_MapsEveryRow()
    {
        var all = await _service.GetAllAsync(CancellationToken.None);

        all.Should().HaveCount(3);
        all.Single(c => c.Key == SecuritySettings.Keys.MaxFailedLoginAttempts).Should().BeEquivalentTo(
            new { Value = "3", DataType = "INT" });
    }

    [Fact]
    public async Task Update_UnknownKey_404()
    {
        await _service.Invoking(_ => Update("NO_SUCH_KEY", "1")).Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("0")]       // an INT setting must be >= 1
    [InlineData("-2")]
    [InlineData("abc")]
    [InlineData("")]
    public async Task Update_InvalidInt_400OnValueField_NothingSaved(string value)
    {
        var ex = await _service.Invoking(_ => Update(SecuritySettings.Keys.MaxFailedLoginAttempts, value)).Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().ContainKey("value");
        _repo.Rows[0].ConfigValue.Should().Be("3");
        _repo.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_ValueLongerThanColumn_400()
    {
        var ex = await _service.Invoking(_ => Update(SecuritySettings.Keys.SessionFullBehaviour, new string('R', 201))).Should().ThrowAsync<ValidationException>();
        ex.Which.Errors["value"].Should().ContainSingle(m => m.Contains("200"));
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("2")]
    public async Task Update_InvalidBool_400(string value)
    {
        await _service.Invoking(_ => Update(SecuritySettings.Keys.FailedLoginLockEnabled, value)).Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Update_SessionFullBehaviour_OnlyRejectAllowed()
    {
        await _service.Invoking(_ => Update(SecuritySettings.Keys.SessionFullBehaviour, "TERMINATE_OLDEST")).Should().ThrowAsync<ValidationException>();

        (await Update(SecuritySettings.Keys.SessionFullBehaviour, "reject")).Value.Should().Be("REJECT");   // stored normalised
    }

    [Theory]
    [InlineData("detail", "DETAIL")]
    [InlineData(" Normal ", "NORMAL")]
    public async Task Update_LogMode_AcceptsNormalOrDetail_StoresUppercase(string value, string stored)
    {
        _repo.Rows.Add(SeedRows.Config(SecuritySettings.Keys.LogMode, "NORMAL", "STRING", id: 4));

        (await Update(SecuritySettings.Keys.LogMode, value)).Value.Should().Be(stored);
    }

    [Theory]
    [InlineData("VERBOSE")]
    [InlineData("DEBUG")]
    public async Task Update_LogMode_OtherValue_400ListsTheChoices(string value)
    {
        _repo.Rows.Add(SeedRows.Config(SecuritySettings.Keys.LogMode, "NORMAL", "STRING", id: 4));

        var ex = await _service.Invoking(_ => Update(SecuritySettings.Keys.LogMode, value)).Should().ThrowAsync<ValidationException>();

        ex.Which.Errors["value"].Should().ContainSingle("Must be one of: NORMAL, DETAIL.");
    }

    [Fact]
    public async Task Update_Valid_ChangesValue_KeyIsCaseInsensitive_AuditsOldAndNew()
    {
        var response = await Update("max_failed_login_attempts", " 5 ");

        response.Value.Should().Be("5");
        _repo.Rows[0].ConfigValue.Should().Be("5");
        _repo.Rows[0].UpdatedBy.Should().Be(1);
        _repo.SaveCount.Should().Be(1);

        var entry = _log.Entries.Single(e => e.ActionType == UserLogActions.SecurityConfigChanged);
        ((SecurityConfigResponse)entry.OldValue!).Value.Should().Be("3");
        ((SecurityConfigResponse)entry.NewValue!).Value.Should().Be("5");
    }
}
