using FluentValidation;
using ST.LiquorTNT.Contracts.Auth;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>Shape checks only; strength is the password policy's job.</summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NewPassword)
            .NotEmpty().MaximumLength(128)
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from the current password.");
    }
}
