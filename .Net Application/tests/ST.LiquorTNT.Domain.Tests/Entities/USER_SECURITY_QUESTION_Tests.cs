using FluentAssertions;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Domain.Tests.Entities;

public sealed class USER_SECURITY_QUESTION_Tests
{
    private static readonly DateTime Now = new(2026, 9, 28, 10, 0, 0);

    [Fact]
    public void Create_IsActive_StoresHashOnly()
    {
        var row = USER_SECURITY_QUESTION.Create(7, 2, "H:answer", Now);

        row.UserId.Should().Be(7);
        row.QuestionId.Should().Be(2);
        row.AnswerHash.Should().Be("H:answer");
        row.IsActive.Should().BeTrue();
        row.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_BlankHash_Throws()
    {
        var act = () => USER_SECURITY_QUESTION.Create(7, 2, " ", Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Replace_UpdatesHashAndReactivates()
    {
        var row = USER_SECURITY_QUESTION.Create(7, 2, "H:old", Now);
        row.Deactivate(Now);

        row.Replace("H:new", Now.AddDays(2));

        row.AnswerHash.Should().Be("H:new");
        row.IsActive.Should().BeTrue();
        row.UpdatedAt.Should().Be(Now.AddDays(2));
        row.Invoking(r => r.Replace(" ", Now)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_RetiresTheChoice()
    {
        var row = USER_SECURITY_QUESTION.Create(7, 2, "H:answer", Now);

        row.Deactivate(Now.AddDays(1));

        row.IsActive.Should().BeFalse();
        row.UpdatedAt.Should().Be(Now.AddDays(1));
    }
}
