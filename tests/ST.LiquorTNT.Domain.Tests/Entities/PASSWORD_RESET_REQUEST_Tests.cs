using FluentAssertions;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Domain.Tests.Entities;

public sealed class PASSWORD_RESET_REQUEST_Tests
{
    private static readonly DateTime Now = new(2026, 9, 28, 10, 0, 0);

    private static PASSWORD_RESET_REQUEST New(int minutesValid = 15) =>
        PASSWORD_RESET_REQUEST.Create(7, "token-hash", Now, Now.AddMinutes(minutesValid));

    [Fact]
    public void Create_StartsPending_WithZeroAttempts()
    {
        var request = New();

        request.Status.Should().Be(PASSWORD_RESET_REQUEST.StatusPending);
        request.VerifyAttempts.Should().Be(0);
        request.IsOpen.Should().BeTrue();
        request.ExpiresAt.Should().Be(Now.AddMinutes(15));
    }

    [Fact]
    public void Create_BlankHash_Throws()
    {
        var act = () => PASSWORD_RESET_REQUEST.Create(7, "", Now, Now.AddMinutes(1));
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ExpiryNotInFuture_Throws(int minutes)
    {
        var act = () => PASSWORD_RESET_REQUEST.Create(7, "h", Now, Now.AddMinutes(minutes));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsExpiredAt_ExpiryIsInclusive()
    {
        var request = New(15);
        var expiry = Now.AddMinutes(15);

        request.IsExpiredAt(expiry.AddSeconds(-1)).Should().BeFalse();
        request.IsExpiredAt(expiry).Should().BeTrue();
    }

    [Fact]
    public void RegisterFailedVerify_CountsAndFailsOnExactlyMaxAttempts()
    {
        var request = New();

        request.RegisterFailedVerify(Now, maxAttempts: 3).Should().BeFalse();
        request.RegisterFailedVerify(Now, maxAttempts: 3).Should().BeFalse();
        request.Status.Should().Be(PASSWORD_RESET_REQUEST.StatusPending);

        request.RegisterFailedVerify(Now, maxAttempts: 3).Should().BeTrue();

        request.VerifyAttempts.Should().Be(3);
        request.Status.Should().Be(PASSWORD_RESET_REQUEST.StatusFailed);
        request.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void StateTransitions_PendingToVerifiedToUsed()
    {
        var request = New();

        request.MarkVerified(Now.AddMinutes(1));
        request.Status.Should().Be(PASSWORD_RESET_REQUEST.StatusVerified);
        request.IsOpen.Should().BeTrue();

        request.MarkUsed(Now.AddMinutes(2));
        request.Status.Should().Be(PASSWORD_RESET_REQUEST.StatusUsed);
        request.IsOpen.Should().BeFalse();
        request.UpdatedAt.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public void MarkExpired_ClosesRequest()
    {
        var request = New();

        request.MarkExpired(Now.AddMinutes(20));

        request.Status.Should().Be(PASSWORD_RESET_REQUEST.StatusExpired);
        request.IsOpen.Should().BeFalse();
    }
}
