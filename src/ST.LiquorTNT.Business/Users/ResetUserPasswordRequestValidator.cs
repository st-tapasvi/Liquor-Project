using FluentValidation;
using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Business.Users;

/// <summary>Shape checks only. Password strength is the password policy's job, not this validator's.</summary>
public sealed class ResetUserPasswordRequestValidator : AbstractValidator<ResetUserPasswordRequest>
{
    public ResetUserPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(128);
    }
}
